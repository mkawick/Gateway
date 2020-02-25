using CommonLibrary;
using Packets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vectors;

namespace Test_game_server
{
    public class PlayerState
    {
        public int connectionId;
        public int characterId;
        public int accountId;
        public int entityId;
        public Vector3 position = new Vector3();
        public Vector3 rotation = new Vector3();
    }

    class TestGameServerController
    {
        // meant to function a little like a game server but only for hammering the gateway
        Int64 applicationId = 15;
        int numPacketsReceived = 0;
        bool isBoundToGateway = false;

        int entityCounter = 1024;
        int GetNewEntityId() { return entityCounter++;  }
        int nextConnectionId;

        List<PlayerState> playerIds;
        IPacketSend socket; // TODO: multiple connections.

        #region Construction
        public TestGameServerController(string gatewayRemoteAddr, ushort gatewayPort, Int64 appId)
        {
            SocketWrapper sock = new SocketWrapper("localhost", 11004);

            applicationId = appId;
            playerIds = new List<PlayerState>();

            sock.OnPacketsReceived += Sock_OnPacketsReceived;
            sock.OnConnect += Sock_OnConnect;
            sock.OnDisconnect += Sock_OnDisconnect;
            socket = sock;
            socket.Connect();
        }

        public void Close()
        {
            if(socket != null && socket.IsConnected == true)
            {
                socket.Disconnect();
                socket = null;
            }
        }
        #endregion Construction

        #region BoilerplateConnections
        private void Sock_OnConnect(IPacketSend sender)
        {
            if (socket != sender)
                return;

           // socket = sender;
            ServerIdPacket serverId = (ServerIdPacket)IntrepidSerialize.TakeFromPool(PacketType.ServerIdPacket);
            serverId.Type = ServerIdPacket.ServerType.Game;
            serverId.Id = (int) applicationId;
            serverId.MapId = 1;
            sender.Send(serverId);
        }
        public void Sock_OnDisconnect(IPacketSend sender, bool willRetry)
        {
            socket.OnConnect -= Sock_OnConnect;
            socket.OnDisconnect -= Sock_OnDisconnect;
            socket.OnConnect -= Sock_OnConnect;
            socket = null;
        }
#endregion BoilerplateConnections

        private void Sock_OnPacketsReceived(IPacketSend arg1, Queue<BasePacket> listOfPackets)
        {
            // all of these boolean checks should be replaced by a Strategy
            if (isBoundToGateway == true)
            {
                foreach (var packet in listOfPackets)
                {
                    numPacketsReceived++;
                    // normal processing

                    KeepAlive ka = packet as KeepAlive;
                    if(ka != null)
                    {
                        KeepAliveResponse kar = (KeepAliveResponse)IntrepidSerialize.TakeFromPool(PacketType.KeepAliveResponse);
                        socket.Send(kar);
                        continue;
                    }
                    WorldEntityPacket wep = packet as WorldEntityPacket;
                    if (wep != null)
                    {
                        foreach(var playerId in playerIds)
                        {
                            if(playerId.entityId == wep.entityId)
                            {
                                playerId.position = wep.position.Get();
                                playerId.rotation = wep.rotation.Get();

                                SendAllEntityPositions();
                            }
                        }
                        continue;
                    }

                    ServerConnectionHeader sch = packet as ServerConnectionHeader;
                    if (sch != null)
                    {
                        nextConnectionId = sch.connectionId;
                        continue;
                    }
                    PlayerSaveStatePacket pss = packet as PlayerSaveStatePacket;
                    if (pss != null)
                    {
                        HandlePlayerSaveState(pss);
                        continue;
                    }
                    if(packet is ServerPingHopperPacket)
                    {
                        HandleServerHopping(packet as ServerPingHopperPacket);
                        continue;
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

        void HandlePlayerSaveState(PlayerSaveStatePacket pss)
        {
            // send all entities in area to player eventually.
            // notify all other players that this player is here.
            PlayerState ps = new PlayerState();
            ps.connectionId = nextConnectionId;
            ps.accountId = pss.state.accountId;
            ps.characterId = pss.state.characterId;
            ps.entityId = GetNewEntityId();
            playerIds.Add(ps);

            ServerConnectionHeader gatewayHeader = (ServerConnectionHeader)IntrepidSerialize.TakeFromPool(PacketType.ServerConnectionHeader);
            gatewayHeader.connectionId = ps.connectionId;
            EntityPacket entityNotification = (EntityPacket)IntrepidSerialize.TakeFromPool(PacketType.Entity);
            entityNotification.entityId = ps.entityId;

            socket.Send(gatewayHeader);
            socket.Send(entityNotification);
        }
        void HandleServerHopping(ServerPingHopperPacket packet)
        {
            ServerConnectionHeader gatewayHeader = (ServerConnectionHeader)IntrepidSerialize.TakeFromPool(PacketType.ServerConnectionHeader);
            gatewayHeader.connectionId = nextConnectionId;
            socket.Send(gatewayHeader);

            ServerPingHopperPacket hopper = packet as ServerPingHopperPacket;
            string name = Assembly.GetCallingAssembly().GetName().Name;
            hopper.Stamp(name + " received");
            Send(packet);
        }
        void SendAllEntityPositions()
        {
            foreach (var playerId in playerIds)
            {
                if (playerId.connectionId != 0)
                {
                    foreach (var destPlayerId in playerIds) // n^2
                    {
                        //if (playerId.connectionId != destPlayerId.connectionId)
                        {
                            ServerConnectionHeader gatewayHeader = (ServerConnectionHeader)IntrepidSerialize.TakeFromPool(PacketType.ServerConnectionHeader);
                            gatewayHeader.connectionId = destPlayerId.connectionId;

                            WorldEntityPacket entityNotification = (WorldEntityPacket)IntrepidSerialize.TakeFromPool(PacketType.WorldEntity);
                            entityNotification.entityId = playerId.entityId;
                            entityNotification.position.Set(playerId.position);
                            entityNotification.rotation.Set( playerId.rotation);
                            socket.Send(gatewayHeader);
                            socket.Send(entityNotification);
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