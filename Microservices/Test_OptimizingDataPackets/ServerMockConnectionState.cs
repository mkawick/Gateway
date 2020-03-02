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
using MathNet.Numerics.LinearAlgebra;

namespace Testing
{
    public class PlayerState
    {
        public int connectionId;
        public int characterId;
        public int accountId;
        public int entityId;
        public bool requestedRenderFrame = false;
        public Vector3 position = new Vector3();
        public Vector3 rotation = new Vector3();
        public bool isDirty = false;
        public RenderSettings settings = new RenderSettings();
        long timeStampOfLastRenderFrame = 0;


        public byte[] renderBuffer = null;
        public void Set(Vector3 p, Vector3 r)
        {
            position = p;
            rotation = r;
            isDirty = true;
        }
        public void Clear()
        {
            isDirty = false;
            requestedRenderFrame = false;
        }
        public int SetupRenderBuffer(int w, int h, int bytesPerPixel)
        {
            int size = w * h * bytesPerPixel;
            if (renderBuffer != null)
                return size;

            renderBuffer = new byte[size];
            return size;
        }

        public void TimeStampForFPS(long milliseconds)
        {
            timeStampOfLastRenderFrame = milliseconds;
        }
        public void UpdateForFPS(long milliseconds)
        {

            if (settings.maxFPS == -1)
                return;

            long fps = settings.maxFPS;
            if (fps > 90)
                fps = 90;
            if (fps < 5)
                fps = 5;
            long timeout = 1000 / fps;
            long timeElapsed = milliseconds - timeStampOfLastRenderFrame;
            if(timeElapsed > timeout)
            {
                timeStampOfLastRenderFrame = milliseconds;
                requestedRenderFrame = true;
            }

        }
    }
    class ServerMockConnectionState : ServerConnectionState, INeedsExternalUpdate
    {
        ServerController controller;
        DatablobAccumulator accumulator = new DatablobAccumulator();
        Core.PillarInterfaces.IRenderer renderer;

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
            renderer = null;

            SetupFileToPass();
        }

        public void SetupRenderer(Core.PillarInterfaces.IRenderer r)
        {
            renderer = r;
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

        PlayerState GetPlayerState(int id)
        {
            foreach (var playerId in playerIds)
            {
                if (playerId.entityId == id)
                {
                    return playerId;
                }
            }
            return null;
        }
        public override void Send(BasePacket packet)
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



            if (packet.PacketType == PacketType.RequestPacket)
            {
                // Set camera position on renderer and rotation
                // call to Michaelangelo

                // requestedRenderFrame
                PlayerState ps = GetPlayerState(nextConnectionId);
                if (ps != null)
                    ps.requestedRenderFrame = true;

            }

            HandlePlayerPackets(packet);
            IntrepidSerialize.ReturnToPool(packet);
        }

        void SendAccumulatorToPlayer(int connectionId, byte[] bytes)
        {
            Utils.DatablobAccumulator acc = new Utils.DatablobAccumulator();
            List<DataBlob> blobs = acc.PrepToSendRawData(bytes, bytes.Length);

            var Timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            Console.WriteLine(Timestamp);
            foreach (var blob in blobs)
            {
                ServerConnectionHeader gatewayHeader = (ServerConnectionHeader)IntrepidSerialize.TakeFromPool(PacketType.ServerConnectionHeader);
                gatewayHeader.connectionId = connectionId;
                deserializedPackets.Add(gatewayHeader);
                deserializedPackets.Add(blob);
            }
        }

        public void Update()
        {
            HandleRequestsForFrame();
        }
        void SetupCamerMatrix(PlayerState ps)
        {
            Matrix<float> mat = Matrix<float>.Build.Dense(4, 4);
            float[] id = { 1, 1, 1, 1 };
            mat.SetDiagonal(id);
            //mat.Row(3). = playerId.position.x;
            mat[3, 0] = ps.position.x;
            mat[3, 1] = ps.position.y;
            mat[3, 2] = ps.position.z;
            renderer.UpdateCameraMatrix(mat);
            // TODO, setup rotation too. 
            //ps.rotation;
        }
        void HandleRequestsForFrame()
        {
            long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            foreach (var playerId in playerIds)
            {
                playerId.UpdateForFPS(milliseconds);
                if (playerId.requestedRenderFrame == true)
                {
                    if (renderer != null)
                    {
                        if (playerId.isDirty)
                            SetupCamerMatrix(playerId);

                        int size = playerId.SetupRenderBuffer(1280, 720, 4);
                        renderer.GetRenderFrame(playerId.renderBuffer, size);

                        SendAccumulatorToPlayer(playerId.connectionId, playerId.renderBuffer);
                    }
                    else if (fileBytes != null)
                    {
                        SendAccumulatorToPlayer(playerId.connectionId, fileBytes);
                    }
                    playerId.TimeStampForFPS(milliseconds);
                    playerId.Clear();
                }
            }
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
            if (packet.PacketType == PacketType.WorldEntity)
            {
                WorldEntityPacket wep = packet as WorldEntityPacket;
                if (wep != null)
                {
                    foreach (var playerId in playerIds)
                    {
                        if (playerId.entityId == wep.entityId)
                        {
                            playerId.Set(wep.position.Get(), wep.rotation.Get());

                            //SendAllEntityPositions();
                        }
                    }
                    return;
                }
            }
            if (packet.PacketType == PacketType.RenderSettings)
            {
                RenderSettings rs = packet as RenderSettings;
                if (rs != null)
                {
                    foreach (var playerId in playerIds)
                    {
                        if (playerId.entityId == nextConnectionId)
                        {
                            
                            playerId.settings= rs;

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

            deserializedPackets.Add(gatewayHeader);
            deserializedPackets.Add(entityNotification);
            /* socket.Send(gatewayHeader);
                socket.Send(entityNotification);*/
        }

        public override bool MarkedAsSocketClosed
        {
            get { return false; }
        }

        public override IPAddress Address
        {
            get
            {
                byte[] address = { 192, 168, 0, 1 };
                IPAddress addr = new IPAddress(address);
                return addr;/// remoteIpEndPoint.Address;
            }
        }
    }
}
