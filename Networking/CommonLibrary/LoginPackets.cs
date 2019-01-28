
using System.IO;
namespace Packets
{
    public class LoginCredentials : BasePacket
    {
        public override PacketType PacketType { get { return PacketType.LoginCredentials; } }
        public StringUtils.FixedLengthString32 playerName { get; set; }
        public StringUtils.FixedLengthString32 password { get; set; }
        // public FixedLengthStringBase name = new FixedLengthStringBase();

        public LoginCredentials()
        {
            playerName = new StringUtils.FixedLengthString32();
            password = new StringUtils.FixedLengthString32();
        }
        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);

            string tempName = StringUtils.Sanitize(playerName.MakeString());
            string tempPassword = StringUtils.Sanitize(password.MakeString());
            playerName.Copy(tempName);
            password.Copy(tempPassword);
          /*  writer.Write(tempName);
            writer.Write(tempPassword);
            writer.Write(playerName.GetRaw());*/
            playerName.Write(writer);
            password.Write(writer);
        }
        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            playerName.Read(reader);
            password.Read(reader);
        }

        public override void CopyFrom(BasePacket packet)
        {
            base.CopyFrom(packet);
            var typedPacket = (LoginCredentials)packet;
            playerName.Copy(typedPacket.playerName);
            password.Copy(typedPacket.password);
        }
    }
    
    public class LoginCredentialValid : BasePacket
    {
        public override PacketType PacketType { get { return PacketType.LoginCredentialValid; } }
        public bool isValid { get; set; }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);

            writer.Write(isValid);
        }
        public override void Read(BinaryReader reader)
        {
            base.Read(reader);

            isValid = reader.ReadBoolean();
        }

        public override void CopyFrom(BasePacket packet)
        {
            base.CopyFrom(packet);
            var typedPacket = (LoginCredentialValid)packet;
            isValid = typedPacket.isValid;
        }
    }


    public class LoginClientReady : BasePacket
    {
        public override PacketType PacketType { get { return PacketType.LoginClientReady; } }
    }

    public class LogoutClient : BasePacket
    {
        public int clientConnectionId;
        public override PacketType PacketType { get { return PacketType.LogoutClient; } }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);

            writer.Write(clientConnectionId);
        }
        public override void Read(BinaryReader reader)
        {
            base.Read(reader);

            clientConnectionId = reader.ReadInt32();
        }
        public override void CopyFrom(BasePacket packet)
        {
            base.CopyFrom(packet);
            var typedPacket = (LogoutClient)packet;
            clientConnectionId = typedPacket.clientConnectionId;
        }

    }
}