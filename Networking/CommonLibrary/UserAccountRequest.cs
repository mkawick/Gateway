using Packets;
using System.IO;
using static StringUtils;

namespace Packets
{
    public class UserAccountRequest : BasePacket
    {
        public override PacketType PacketType { get { return PacketType.UserAccountRequest; } }

        public int socketId;
        public int connectionId;
        public FixedLengthString32 username;
        public FixedLengthString32 password;
        public FixedLengthString32 product_name;

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(socketId);
            username.Write(writer);
            password.Write(writer);
            product_name.Write(writer);
         /*   writer.Write(username);
            writer.Write(password);
            writer.Write(product_name);*/
        }
        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            socketId = reader.ReadInt32();
            username.Read(reader);
            password.Read(reader);
            product_name.Read(reader);
            /* username = reader.ReadString();
             password = reader.ReadString();
             product_name = reader.ReadString();*/
        }
        public override void CopyFrom(BasePacket packet)
        {
            base.CopyFrom(packet);
            var typedPacket = (UserAccountRequest)packet;
            socketId = typedPacket.socketId;
            username = typedPacket.username;
            password = typedPacket.password;
            product_name = typedPacket.product_name;
        }
    }

    public class UserAccountResponse : BasePacket
    {
        public override PacketType PacketType { get { return PacketType.UserAccountResponse; } }

        public int socketId;
        public int connectionId;
        public bool isValidAccount;
        public PlayerSaveState state;

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(socketId);
            writer.Write(connectionId);
            writer.Write(isValidAccount);
            if (isValidAccount)
            {
                state.Write(writer);
            }
        }
        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            socketId = reader.ReadInt32();
            connectionId = reader.ReadInt32();
            isValidAccount = reader.ReadBoolean();
            if (isValidAccount)
            {
                state = new PlayerSaveState();
                state.Read(reader);
            }
        }

        public override void CopyFrom(BasePacket packet)
        {
            base.CopyFrom(packet);
            var typedPacket = (UserAccountResponse)packet;
            socketId = typedPacket.socketId;
            isValidAccount = typedPacket.isValidAccount;
        }
    }
}
