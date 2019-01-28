using CommonLibrary;
using Packets;
using System;
using System.Collections.Generic;

namespace HeadlessClient01
{
    class TalkingToGatewayController
    {
        int applicationId;
        int numPacketsReceived = 0;
        bool isBoundToGateway = false;

        public bool isLoggedIn = false;
        SocketWrapper socket;
        MyPlayer localPlayer;

        public TalkingToGatewayController(string gatewayRemoteAddr, ushort gatewayPort, MyPlayer player, int appId)
        {
            Set(player);
            socket = new SocketWrapper(gatewayRemoteAddr, gatewayPort);

            applicationId = appId;
            socket.OnPacketsReceived += Sock_OnPacketsReceived;
            socket.OnConnect += Sock_OnConnect;
            socket.OnDisconnect += Sock_OnDisconnect;
            socket.Connect();
        }

        public void Set(MyPlayer player)
        {
            localPlayer = player;
        }
        public void Close()
        {
            if (socket != null && socket.IsConnected == true)
            {
                socket.Disconnect();
                socket = null;
            }
        }

        private void Sock_OnConnect(IPacketSend sender)
        {
            if (socket != sender)
                return;

            ClientIdPacket clientId = (ClientIdPacket)IntrepidSerialize.TakeFromPool(PacketType.ClientIdPacket);
            clientId.Id = applicationId;
            sender.Send(clientId);
        }
        public void Sock_OnDisconnect(IPacketSend sender, bool willRetry)
        {
            socket.OnConnect -= Sock_OnConnect;
            socket.OnDisconnect -= Sock_OnDisconnect;
            socket.OnConnect -= Sock_OnConnect;
            socket = null;
        }

        void HandleNormalPackets(Queue<BasePacket> listOfPackets)
        {
            foreach (var packet in listOfPackets)
            {
                //Console.WriteLine("normal packet received {0} .. isLoggedIn = true", packet.PacketType);
                numPacketsReceived++;
                // normal processing
                if (packet is PlayerFullPacket && localPlayer.entityId == 0)
                {
                    localPlayer.entityId = (packet as PlayerFullPacket).entityId;
                }
                
           /*     ServerTick st = packet as ServerTick;
                if(st != null)
                {
                    Console.WriteLine("server tick {0}", st.TickCount);
                    
                    continue;
                }*/
                KeepAlive ka = packet as KeepAlive;
                if (ka != null)
                {
                    KeepAliveResponse kar = (KeepAliveResponse)IntrepidSerialize.TakeFromPool(PacketType.KeepAliveResponse);
                    socket.Send(kar);
                }

                if (packet is ServerPingHopperPacket)
                {
                    ServerPingHopperPacket hopper = packet as ServerPingHopperPacket;
                    hopper.Stamp("client end");
                    hopper.PrintList();
                }
                if (packet is WorldEntityPacket)
                {
                    WorldEntityPacket wep = packet as WorldEntityPacket;
                    if(localPlayer.entityId == wep.entityId)
                    {
                        localPlayer.position = wep.position.Get();
                        localPlayer.rotation = wep.rotation.Get();
                    }
                }
                EntityPacket ep = packet as EntityPacket;
                if (ep != null)
                {
                    //entityId = ep.entityId;
                    int tempEntityId = ep.entityId;
                    if (localPlayer.entityId == tempEntityId)
                    {
                        Console.Write("This entity packet updated {0}\n", tempEntityId);
                    }
                   /* else
                    {
                        Console.Write("Entity update packet {0}\n", tempEntityId);
                    }*/
                    continue;
                }

                // Console.WriteLine("normal packet received {0}", packet.PacketType);

                IntrepidSerialize.ReturnToPool(packet);
            }
        }
        private void Sock_OnPacketsReceived(IPacketSend arg1, Queue<BasePacket> listOfPackets)
        {
            // all of these boolean checks should be replaced by a Strategy
            if (isBoundToGateway == true)
            {
                if (isLoggedIn == true) 
                {
                    HandleNormalPackets(listOfPackets);
                }
                else
                {
                    foreach (var packet in listOfPackets)
                    {
                        Console.WriteLine("normal packet received {0} .. isLoggedIn = false", packet.PacketType);
                        LoginCredentialValid lcr = packet as LoginCredentialValid;
                        if (lcr != null)
                        {
                            LoginClientReady temp = (LoginClientReady)IntrepidSerialize.TakeFromPool(PacketType.LoginClientReady);
                            Send(temp);

                            ClientGameInfoResponse cgir = (ClientGameInfoResponse)IntrepidSerialize.TakeFromPool(PacketType.ClientGameInfoResponse);
                            cgir.GameId = applicationId;
                            Send(cgir);

                            isLoggedIn = lcr.isValid;
                        }
                    /*    if (localPlayer.entityId == 0)// until we are assigned an entity id, we can't do much
                        {
                            EntityPacket ep = packet as EntityPacket;
                            if (ep != null)
                            {
                                localPlayer.entityId = ep.entityId;
                            }
                        }*/

                        numPacketsReceived++;
                    }
                }
            }
            else
            {
                foreach (var packet in listOfPackets)
                {
                    numPacketsReceived++;
                    if (packet is ServerIdPacket)
                    {
                        ServerIdPacket id = packet as ServerIdPacket;
                        if (id != null && id.Type == ServerIdPacket.ServerType.Gateway)
                        {
                            isBoundToGateway = true;
                            break;
                        }
                    }
                }
            }
        }

        public void Send(BasePacket bp)
        {
            socket.Send(bp);
        }
    }
}
