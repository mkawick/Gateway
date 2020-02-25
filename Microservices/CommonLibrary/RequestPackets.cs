﻿using System;
using System.IO;
using Vectors;

namespace Packets
{
    public class RequestPackets : BasePacket
    {
        public override PacketType PacketType { get { return PacketType.RequestPackets; } }

        public enum RequestType
        {
            Default,
            RequestRenderFrame
        };
        
        public RequestType type = RequestType.Default;
        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write((int) type);
        }
        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            type = (RequestType) reader.ReadInt32();
        }
    }
    //
}
