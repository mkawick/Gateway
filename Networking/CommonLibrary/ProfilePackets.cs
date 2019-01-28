
using System.IO;
using static StringUtils;

namespace Packets
{
    public class ProfileCreateCharacterRequest : BasePacket
    {
        public override PacketType PacketType { get { return PacketType.ProfileCreateCharacterRequest; } }

        public int accountId;
        public FixedLengthString32 productName;
        public FixedLengthString32 characterName;
        public PlayerSaveStateData state;

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(accountId);
            productName.Write(writer);
            characterName.Write(writer);
          /*  writer.Write(productName);
            writer.Write(characterName);*/
            state.Write(writer);
        }

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            accountId = reader.ReadInt32();
            productName.Read(reader);
            characterName.Read(reader);
          /*  productName = reader.ReadString();
            characterName = reader.ReadString();*/
            // TODO: Non-alloc version of this
            state = new PlayerSaveStateData();
            state.Read(reader);
        }

        public override void CopyFrom(BasePacket packet)
        {
            base.CopyFrom(packet);
            var typedPacket = (ProfileCreateCharacterRequest)packet;
            accountId = typedPacket.accountId;
            productName = typedPacket.productName;
            characterName = typedPacket.characterName;
            // TODO: Non-alloc version of this
            state = new PlayerSaveStateData();
            state.CopyFrom(typedPacket.state);
        }
    }

    public class ProfileCreateCharacterResponse : BasePacket
    {
        public override PacketType PacketType { get { return PacketType.ProfileCreateCharacterResponse; } }

        public int accountId;
        public int characterId;

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(accountId);
            writer.Write(characterId);
        }

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            accountId = reader.ReadInt32();
            characterId = reader.ReadInt32();
        }

        public override void CopyFrom(BasePacket packet)
        {
            base.CopyFrom(packet);
            var typedPacket = (ProfileCreateCharacterResponse)packet;
            accountId = typedPacket.accountId;
            characterId = typedPacket.characterId;
        }
    }

    public class ProfileUpdateCharacter : BasePacket
    {
        public override PacketType PacketType { get { return PacketType.ProfileUpdateCharacter; } }

        public int characterId;
        public PlayerSaveStateData state;

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(characterId);
            state.Write(writer);
        }

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            characterId = reader.ReadInt32();
            // TODO: Non-alloc version of this
            state = new PlayerSaveStateData();
            state.Read(reader);
        }

        public override void CopyFrom(BasePacket packet)
        {
            base.CopyFrom(packet);
            var typedPacket = (ProfileUpdateCharacter)packet;
            characterId = typedPacket.characterId;
            // TODO: Non-alloc version of this
            state = new PlayerSaveStateData();
            state.CopyFrom(typedPacket.state);
        }
    }

}