using CommonLibrary;
using Packets;
using System.Collections.Generic;
using System.Reflection;

namespace Gateway
{
    public class GatewayPlayer
    {
        protected List<BasePacket> outGoingPackets;

        public PlayerConnectionState connection;
        private GatewayMain gateway;

        // Unique, GatewayServer-assigned id for the connected socket
        public int connectionId;
        public int gameId = 0;
        public int accountId = 0;

        //bool credentialsReceived = false;
        bool loginComplete = false;
        bool loginStatusPassedToGame = false;

        public GatewayPlayer(int _connectionId, GatewayMain _game, PlayerConnectionState _pcs, int _accountId)
        {
            connectionId = _connectionId;
            Gateway = _game;
            connection = _pcs;
            accountId = _accountId;
            outGoingPackets = new List<BasePacket>();
        }

        public bool IsLoginComplete
        {
            get { return loginComplete; }
        }
        public bool HasNotifiedGameServer
        {
            get { return loginStatusPassedToGame;  }
            set { loginStatusPassedToGame = value; }
        }

        public GatewayMain Gateway
        {
            get
            {
                return gateway;
            }

            set
            {
                gateway = value;
            }
        }

        protected bool HasDataToSend()
        {
            return outGoingPackets.Count > 0;
        }

        public void ProcessIncomingData()
        {
            if (connection.HasNewData() == true)
            {
                List<BasePacket> packetList = connection.RetrieveData();
                foreach (var bp in packetList)
                {
                    if (loginComplete == true && 
                        gameId != 0)//TODO: replace with strategy pattern
                    {
                        ProcessLoggedIn(bp);
                    }
                    else
                    {
                        ProcessUnloggedIn(bp);
                    }
                }
            }
        }

        public void Update()
        {
            ProcessIncomingData();
            ProcessOutgoingData();
        }

        public void ProcessUnloggedIn(BasePacket packet)
        {
            var packetType = packet.PacketType;

            if (packetType == PacketType.LoginCredentials)
            {
                LoginCredentials lc = packet as LoginCredentials;
                
                //credentialsReceived = true;
                //SendInitData(this);
            }
            else if (packetType == PacketType.LoginClientReady)
            {
                LoginClientReady lc = packet as LoginClientReady;
                loginComplete = true;
                // signal game server that player has logged in.
                // Also, all other servers should be signalled. Once we have a real login server, then the gateway will not need to do any of this.
            }
            else if(packetType== PacketType.ClientGameInfoResponse)
            {
                ClientGameInfoResponse lc = packet as ClientGameInfoResponse;
                gameId = lc.GameId;
            }
            IntrepidSerialize.ReturnToPool(packet);
        }
        public void ProcessLoggedIn(BasePacket packet)
        {
            var packetType = packet.PacketType;

            // add possible attack vectors to this list
            // these cannot be processed once we are logged in.
            if (packetType == PacketType.LoginCredentials ||
                packetType == PacketType.LoginClientReady ||
                packetType == PacketType.ClientGameInfoResponse
                )
            {
                IntrepidSerialize.ReturnToPool(packet);
                return;
            }
            if(packetType == PacketType.ServerPingHopper)
            {
                string name = Assembly.GetCallingAssembly().GetName().Name;
                (packet as ServerPingHopperPacket).Stamp(name + " received from client");
            }
            Gateway.AddIncomingPacket(packet, connectionId, gameId);
        }

        public void AddPacket(BasePacket bp)
        {
            outGoingPackets.Add(bp);
        }

        public void ProcessOutgoingData()
        {
            if (HasDataToSend())
            {
                foreach (var packet in outGoingPackets)
                {
                    if (packet.PacketType == PacketType.ServerPingHopper)
                    {
                        string name = Assembly.GetCallingAssembly().GetName().Name;
                        (packet as ServerPingHopperPacket).Stamp(name + " sending to client");
                    }
                    connection.Send(packet);
                }
                outGoingPackets.Clear();
            }
        }
    }
}
