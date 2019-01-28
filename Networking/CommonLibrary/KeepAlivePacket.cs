
using System.IO;
namespace Packets
{
    
    public class KeepAlive : BasePacket
    {
        public override PacketType PacketType { get { return PacketType.KeepAlive; } }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
        }
        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
        }
    }

    public class KeepAliveResponse : BasePacket
    {
        public override PacketType PacketType { get { return PacketType.KeepAliveResponse; } }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
        }
        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
        }
    }
}