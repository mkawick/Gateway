using System;
using System.IO;
using Vectors;

namespace Packets
{
    public enum MovementType
    {
        Walk, Fly, Swim, Teleport
    }

    // note that this packet can be used for a location object or a real destination
    public class Entity_MoveTo : EntityPacket
    {
        public override PacketType PacketType { get { return PacketType.Entity_MoveTo; } }
        
        public int frameId;
        public int destinationEntityId;
        public MovementType movementType;
        public Vector3 destination;

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            frameId = reader.ReadInt32();
            destinationEntityId = reader.ReadInt32();
            movementType = (MovementType)reader.ReadInt32();
            destination.Read(reader);
        }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);

            writer.Write(frameId);
            writer.Write(destinationEntityId);
            writer.Write((Int32)movementType);
            destination.Write(writer);
        }

        public override void CopyFrom(BasePacket packet)
        {
            base.CopyFrom(packet);
            var typedPacket = (Entity_MoveTo)packet;
            frameId = typedPacket.frameId;
            destinationEntityId = typedPacket.destinationEntityId;
            movementType = typedPacket.movementType;
            destination = typedPacket.destination;
        }
    }

    public class Entity_MoveAway : EntityPacket
    {
        public override PacketType PacketType { get { return PacketType.Entity_MoveAway; } }

        public int frameId;
        public Vector3 awayFrom;

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            frameId = reader.ReadInt32();
            awayFrom.Read(reader);
        }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);

            writer.Write(frameId);
            awayFrom.Write(writer);
        }

        public override void CopyFrom(BasePacket packet)
        {
            base.CopyFrom(packet);
            var typedPacket = (Entity_MoveAway)packet;
            frameId = typedPacket.frameId;
            awayFrom = typedPacket.awayFrom;
        }
    }

}