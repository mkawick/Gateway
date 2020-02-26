using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using CommonLibrary;
using Packets;
using static Network.Utils;
using System.IO;
using Network;
using Vectors;

namespace Test_Direct_ServerToClient
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

    class ServerMockConnectionState : ServerConnectionState
    {
        ServerController controller;
        DatablobAccumulator accumulator = new DatablobAccumulator();
        byte[] fileBytes;
        int nextConnectionId;
        int userIdCounter = 1024;
        int GetNewUserId() { return userIdCounter++; }
        List<PlayerState> playerIds;

        public ServerMockConnectionState(ServerController network)
        {
            serverType = ServerIdPacket.ServerType.Mock;
            controller = network;
            playerIds = new List<PlayerState>();

            SetupFileToPass();
        }
        void SetupFileToPass()
        {
            fileBytes = File.ReadAllBytes("c:/temp/skull.png");
            
        }
        protected override void Socket_OnPacketsReceived(IPacketSend externalSocket, Queue<BasePacket> packets)
        {
            if (packets.Count == 1)
            {
            }
        }

        public void ConnectMock()
        {
            ServerIdPacket serverId = (ServerIdPacket)IntrepidSerialize.TakeFromPool(PacketType.ServerIdPacket);
            serverId.Type = ServerIdPacket.ServerType.Game;
            serverId.Id = (int)1234;
            serverId.MapId = 1;
            deserializedPackets.Add(serverId);
            //controller.Send(serverId);
        }

     /*   void validateReceivedBuffer(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                byte val = bytes[i];
                //Console.Write(val.ToString() + " ");
                Debug.Assert(i % 256 == val);
            }
            Console.WriteLine("all went well");
            var Timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            Console.WriteLine(Timestamp);

        }*/
        public override void  Send(BasePacket packet)
        {
            ServerConnectionHeader sch = packet as ServerConnectionHeader;
            if (sch != null)
            {
                nextConnectionId = sch.connectionId;
                
            }
          /*  if (packet.PacketType == PacketType.DataBlob)
            {
                if(accumulator.Add(packet as DataBlob) == true)
                {
                    byte[] bytes = accumulator.ConvertDatablobsIntoRawData();
                    validateReceivedBuffer(bytes);
                    accumulator.Clear();
                }
                return;
            }*/
            if(packet.PacketType == PacketType.RequestPacket)
            {
                if(fileBytes != null)
                {
                    Utils.DatablobAccumulator acc = new Utils.DatablobAccumulator();
                    List<DataBlob> blobs = acc.PrepToSendRawData(fileBytes, fileBytes.Length);

                    var Timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                    Console.WriteLine(Timestamp);
                    foreach (var blob in blobs)
                    {
                        ServerConnectionHeader gatewayHeader = (ServerConnectionHeader)IntrepidSerialize.TakeFromPool(PacketType.ServerConnectionHeader);
                        gatewayHeader.connectionId = nextConnectionId;
                        deserializedPackets.Add(gatewayHeader);
                        deserializedPackets.Add(blob);
                    }
                }
            }

            HandlePlayerPackets(packet);
            IntrepidSerialize.ReturnToPool(packet);
        }

        void HandlePlayerPackets(BasePacket packet)
        {
            if (packet.PacketType == PacketType.ServerConnectionHeader)
            {
                ServerConnectionHeader sch = packet as ServerConnectionHeader;
                if (sch != null)
                {
                    nextConnectionId = sch.connectionId;
                    return;
                }
            }
            if (packet.PacketType == PacketType.PlayerSaveState)
            {
                HandlePlayerSaveState(packet as PlayerSaveStatePacket);
                return;
            }
            if(packet.PacketType == PacketType.WorldEntity)
            {
                WorldEntityPacket wep = packet as WorldEntityPacket;
                if (wep != null)
                {
                    foreach (var playerId in playerIds)
                    {
                        if (playerId.entityId == wep.entityId)
                        {
                            playerId.position = wep.position.Get();
                            playerId.rotation = wep.rotation.Get();

                            //SendAllEntityPositions();
                        }
                    }
                    return;
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
            ps.entityId = GetNewUserId();
            playerIds.Add(ps);

            ServerConnectionHeader gatewayHeader = (ServerConnectionHeader)IntrepidSerialize.TakeFromPool(PacketType.ServerConnectionHeader);
            gatewayHeader.connectionId = ps.connectionId;
            EntityPacket entityNotification = (EntityPacket)IntrepidSerialize.TakeFromPool(PacketType.Entity);
            entityNotification.entityId = ps.entityId;

            socket.Send(gatewayHeader);
            socket.Send(entityNotification);
        }

        public override bool MarkedAsSocketClosed
        {
            get { return false; }
        }

        public override IPAddress Address 
        {
            
            get {
                byte[] address = { 192, 168, 0, 1 };
                IPAddress addr = new IPAddress(address); 
                return remoteIpEndPoint.Address; 
            } 
        }
    }
}
