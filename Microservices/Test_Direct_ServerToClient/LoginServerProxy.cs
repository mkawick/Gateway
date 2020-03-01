using System.Collections.Generic;
using System;
using CommonLibrary;
using Packets;

namespace Test_Direct_ServerToClient
{
    public class LoginServerProxy : ThreadWrapper
    {
        List<PlayerConnectionState> newConnections;
        List<PlayerConnectionState> limboConnections;
        List<PlayerConnectionState> loggedInPlayers;
        List<PlayerConnectionState> invalidPlayers;
        private object responsesLock = new object();
        List<BasePacket> unprocessedLoginServerResponses;
        IPacketSend loginServerSocket;
        
        bool isConnectedToRealLogin;
        int tempLoginId = 2048;

        // Ensures serialized access to the two above lists
        private object connectionLock = new object();
        public event Action<PlayerConnectionState, bool, PlayerSaveState> OnNewPlayerLoggedIn;

        //------------------------------------------------------------------

        public LoginServerProxy(SocketWrapperSettings loginServerToConnectTo)
        {
            newConnections = new List<PlayerConnectionState>();
            limboConnections = new List<PlayerConnectionState>();
            loggedInPlayers = new List<PlayerConnectionState>();
            invalidPlayers = new List<PlayerConnectionState>();

            unprocessedLoginServerResponses = new List<BasePacket>();

            isConnectedToRealLogin = loginServerToConnectTo != null;
            if(isConnectedToRealLogin == true)
            {
                loginServerSocket = new SocketWrapper(loginServerToConnectTo);
                loginServerSocket.OnPacketsReceived += LoginServerSocket_OnPacketsReceived;
                loginServerSocket.Connect();
            }
            configuredSleep = NetworkConstants.LoginProxyFPS;
        }

        public override void EndService()
        {
            base.EndService();
            if(loginServerSocket != null)
            {
                loginServerSocket.Disconnect();
            }
        }

        public void HandleNewConnection(PlayerConnectionState playerConnection)
        {
            playerConnection.tempId = tempLoginId++;
            lock (connectionLock)
            {
                newConnections.Add(playerConnection);
            }
        }

        //Loops through the waiting connections and collects full data for them
        private void LoginUnhandledPlayers()
        {
            List<PlayerConnectionState> tempList;
            // Need to lock as other threads will be calling HandleNewConnection
            lock (connectionLock)
            {
                if (newConnections.Count == 0)
                    return;
                
                tempList = newConnections;
                newConnections = new List<PlayerConnectionState>();
            }

            if(isConnectedToRealLogin == false)
            {
                MoveToLoggedInPlayers(tempList);// auto login
            }
            else
            {
                MoveToLimboConnections(tempList);
            }
        }

        void MoveToLoggedInPlayers(List<PlayerConnectionState> tempList)
        {
            for (int i = tempList.Count - 1; i >= 0; --i)
            {
                var player = tempList[i];// normally goes to db
                loggedInPlayers.Add(player);
                player.finishedLoginSuccessfully = true;
                tempList.RemoveAt(i);

                PlayerSaveState save = new PlayerSaveState();
                // Fudge the accountId to be the temporary id
                save.accountId = player.tempId;

                OnNewPlayerLoggedIn?.Invoke(player, true, save);
            }
        }

        void MoveToLimboConnections(List<PlayerConnectionState> tempList)
        {
            foreach (var player in tempList)
            {
                limboConnections.Add(player);
            }
        }


        public void ServiceLimboConnections()
        {
            foreach (PlayerConnectionState player in limboConnections)
            {
                if (player.HasNewData())
                {
                    List<BasePacket> packetList = player.RetrieveData();
                    foreach(BasePacket packet in packetList)
                    {
                        LoginCredentials lc = packet as LoginCredentials;
                        if(lc != null && loginServerSocket != null)
                        {
                            UserAccountRequest uar = IntrepidSerialize.TakeFromPool(PacketType.UserAccountRequest) as UserAccountRequest;
                            uar.connectionId = player.tempId;
                            uar.password.Copy(lc.password);
                            uar.product_name.Copy("hungry hippos"); // TODO, configure product name 
                            uar.username.Copy(lc.playerName);

                            loginServerSocket.Send(uar);
                            break;
                        }
                    }
                    IntrepidSerialize.ReturnToPool(packetList);
                }
            }
        }

        void ProcessUnprocessedLoginServerResponses()
        {
            List<BasePacket> tempPacketList;
            lock (responsesLock)
            {
                tempPacketList = unprocessedLoginServerResponses;
                unprocessedLoginServerResponses = new List<BasePacket>();
            }

            foreach (BasePacket packet in tempPacketList)
            {
                UserAccountResponse uar = packet as UserAccountResponse;
                if (uar == null)
                {
                    IntrepidSerialize.ReturnToPool(packet);
                    continue;
                }

                //List<PlayerConnectionState> validConnections;
                // the pending users should be a tiny list
                PlayerConnectionState foundPlayer = null;
                int indexOfFoundPlayer = -1;
                foreach (PlayerConnectionState player in limboConnections)
                {
                    indexOfFoundPlayer++;
                    if (player.tempId == uar.connectionId)
                    {
                        foundPlayer = player;
                        break;
                    }
                }
                if(foundPlayer != null)
                {
                    limboConnections.RemoveAt(indexOfFoundPlayer);
                    foundPlayer.finishedLoginSuccessfully = uar.isValidAccount;
                    if (uar.isValidAccount == false)
                    {
                        invalidPlayers.Add(foundPlayer);
                    }
                    else
                    {
                        loggedInPlayers.Add(foundPlayer);
                    }

                    OnNewPlayerLoggedIn?.Invoke(foundPlayer, uar.isValidAccount, uar.state);
                }
                IntrepidSerialize.ReturnToPool(uar);
            }
        }

        protected override void ThreadTick()
        {
            LoginUnhandledPlayers();
            ServiceLimboConnections();
            ProcessUnprocessedLoginServerResponses();
        }

        //----------------------------------------------------------------------

        public void LoginServerSocket_OnPacketsReceived(IPacketSend socket, Queue<BasePacket> listOfServer2ServerPackets)
        {
            lock (responsesLock)
            {
                unprocessedLoginServerResponses.AddRange(listOfServer2ServerPackets);
            }
        }
    }

    //------------------------------------------------------------------------------------
}