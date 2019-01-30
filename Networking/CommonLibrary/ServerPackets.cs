
using System.IO;
using Network;

namespace Packets
{
    public class ServerConnectionHeader : BasePacket
    {
        public override PacketType PacketType { get { return PacketType.ServerConnectionHeader; } }

        public int connectionId;
        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);

            writer.Write(connectionId);
        }
        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            connectionId = reader.ReadInt32();
        }

        public override void CopyFrom(BasePacket packet)
        {
            base.CopyFrom(packet);
            var typedPacket = (ServerConnectionHeader)packet;
            connectionId = typedPacket.connectionId;
        }
    }

    public class ServerIdPacket : BasePacket
    {
        public enum ServerType
        {
            Gateway,
            Game,
            Login,
            Database,
            Chat,
            Friends,
            Groups,
            Purchase,
            None
        }
        public override PacketType PacketType { get { return PacketType.ServerIdPacket; } }
        public uint Id { get; set; }
        public int MapId { get; set; } // usually 0
        public ServerType Type { get; set; }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);

            writer.Write(Id);
            writer.Write(MapId);
            int typeValue = (int)Type;
            writer.Write(typeValue);
        }
        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            Id = reader.ReadUInt32();
            MapId = reader.ReadInt32();
            int typeValue = reader.ReadInt32();
            Type = (ServerType)typeValue;
        }
        public override void CopyFrom(BasePacket packet)
        {
            base.CopyFrom(packet);
            var typedPacket = (ServerIdPacket)packet;
            Id = typedPacket.Id;
            MapId = typedPacket.MapId;
            Type = typedPacket.Type;
        }
    }

    public class ServerDisconnectPacket : ServerIdPacket
    {
        public override PacketType PacketType { get { return PacketType.ServerDisconnect; } }
    }

    public class Ping : IBinarySerializable
    {
        public StringUtils.FixedLengthString32 name = new StringUtils.FixedLengthString32();
        public int diffTime;
        public void Write(BinaryWriter writer)
        {
            name.Write(writer);
            writer.Write(diffTime);
        }
        public void Read(BinaryReader reader)
        {
            name.Read(reader);
            diffTime = reader.ReadInt32 ();
        }
    }
    public class ServerPingHopperPacket : ServerIdPacket
    {
        public int topOfList = 0;
        const int maxItems = 18;
        public Ping[] pingList;
        public override PacketType PacketType { get { return PacketType.ServerPingHopper; } }
        public ServerPingHopperPacket()
        {
            topOfList = 0;
            pingList = new Ping[maxItems];
            for(int i=0; i< maxItems; i++)
            {
                pingList[i] = new Ping();
            }
        }
        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(topOfList);
            for(int i=0; i< topOfList; i++)
            {
                pingList[i].Write(writer);
            }
        }
        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            topOfList = reader.ReadInt32();
            for (int i = 0; i < topOfList; i++)
            {
                pingList[i].Read(reader);
            }
        }
        public void Stamp(string name)
        {
            if (topOfList >= maxItems - 1)
                throw new System.Exception("big problem");
            int ms = (int)(System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond);
            pingList[topOfList].name.Copy(name);
            pingList[topOfList].diffTime = ms;
            topOfList++;
        }

        public void PrintList()
        {
            System.Console.WriteLine("ServerPingHopperPacket result");
            System.Console.WriteLine("-----------------------------");
            System.Console.WriteLine(" num hops: {0}", topOfList);
            int diffTime = pingList[0].diffTime;
            for (int i=0; i<topOfList; i++)
            {
                int workingTime = pingList[i].diffTime;
                System.Console.WriteLine("  {0}: {1} ms: {2}", i, workingTime - diffTime, pingList[i].name.MakeString());
                diffTime = workingTime;
            }

            System.Console.WriteLine("-----------------------------");
        }
    }
}