using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using CommonLibrary;
using Packets;
using static Network.Utils;
//using <iostream>

namespace Test_Direct_ServerToClient
{
    class ServerMockConnectionState : ServerConnectionState
    {
        ServerController controller;
        DatablobAccumulator accumulator = new DatablobAccumulator();
        public ServerMockConnectionState(ServerController network)
        {
            serverType = ServerIdPacket.ServerType.Mock;
            controller = network;
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

        void validateReceivedBuffer(byte[] bytes)
        {
       /*     for (int i = 0; i < bytes.Length; i++)
            {
                byte val = bytes[i];
                //Console.Write(val.ToString() + " ");
                Debug.Assert(i % 256 == val);
            }*/
            Console.WriteLine("all went well");
            var Timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            Console.WriteLine(Timestamp);

        }
        public override void  Send(BasePacket packet)
        {
            if(packet.PacketType == PacketType.DataBlob)
            {
                if(accumulator.Add(packet as DataBlob) == true)
                {
                    byte[] bytes = accumulator.ConvertDatablobsIntoRawData();
                    validateReceivedBuffer(bytes);
                    accumulator.Clear();
                }
            }
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
