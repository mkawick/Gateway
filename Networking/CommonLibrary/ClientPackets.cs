
using System.IO;
namespace Packets
{
    
    public class ClientGameInfoRequest : BasePacket
    {
        public override PacketType PacketType { get { return PacketType.ClientGameInfoRequest; } }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
        }
        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
        }
    }

    public class ClientGameInfoResponse : BasePacket
    {
        public override PacketType PacketType { get { return PacketType.ClientGameInfoResponse; } }
        public int GameId { get; set; }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(GameId);
        }
        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            GameId = reader.ReadInt32();
        }

        public override void CopyFrom(BasePacket packet)
        {
            base.CopyFrom(packet);
            var typedPacket = (ClientGameInfoResponse)packet;
            GameId = typedPacket.GameId;
        }
    }

    public class ClientIdPacket : BasePacket
    {
        public override PacketType PacketType { get { return PacketType.ClientIdPacket; } }
        public int Id { get; set; }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(Id);
        }
        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            Id = reader.ReadInt32();
        }

        public override void CopyFrom(BasePacket packet)
        {
            base.CopyFrom(packet);
            var typedPacket = (ClientIdPacket)packet;
            Id = typedPacket.Id;
        }
    }
    public class ClientDisconnectPacket : BasePacket
    {
        public override PacketType PacketType { get { return PacketType.ClientDisconnect; } }
        public int accountId { get; set; }
        public int connectionId { get; set; }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(accountId);
            writer.Write(connectionId);
        }
        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            accountId = reader.ReadInt32();
            connectionId = reader.ReadInt32();
        }
        public override void CopyFrom(BasePacket packet)
        {
            base.CopyFrom(packet);
            var typedPacket = (ClientDisconnectPacket)packet;
            accountId = typedPacket.accountId;
            connectionId = typedPacket.connectionId;
        }
    }
}