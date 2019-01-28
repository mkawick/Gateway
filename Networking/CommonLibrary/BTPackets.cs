using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using PacketTypes;

namespace Packets
{
    public class NPC_BTState: EntityPacket
    {
        public int frameId;
        public StringUtils.FixedLengthString40 guid = new StringUtils.FixedLengthString40();

        public override PacketType PacketType
        {
            get
            {
                return PacketType.NPC_BTState;
            }
        }

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            frameId = reader.ReadInt32();
            guid.Read(reader);
        }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);

            writer.Write(frameId);
            guid.Write(writer);
        }

        public override void CopyFrom(BasePacket packet)
        {
            base.CopyFrom(packet);
            NPC_BTState bts = packet as NPC_BTState;
            frameId = bts.frameId;
            guid.Copy(bts.guid);
        }
    }

    public class NPC_BlackBoard : EntityPacket
    {
        public override PacketType PacketType { get { return PacketType.NPC_BlackBoard; } }
        public byte[] bbDelta;

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            int count = reader.ReadInt32();
            bbDelta = reader.ReadBytes(count);
        }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(bbDelta.Length);
            writer.Write(bbDelta);
        }

        public override void CopyFrom(BasePacket packet)
        {
            base.CopyFrom(packet);
            NPC_BlackBoard bbs = packet as NPC_BlackBoard;
            bbDelta = new byte[bbs.bbDelta.Length];
            Array.Copy(bbs.bbDelta, bbDelta, bbs.bbDelta.Length);
        }

    }
}