//#define DEBUG_NETWORK_PACKETS

using System;
using System.IO;
using Network;
// steps to add new packets
// 1) Add the Packet type in the enum.. be sure that it's unique
// 2) Pick a place to put the new packet type. We have a lot of packet files. Right now, there are no cs files in the project and it "self-adds" the files on load. 
//  If you add a new one, do it from Windows Explorer
// 3) use WorldEntityPacket as a template and build out that packet
// 4) add that to the SetupPacketFactory
// 5) profit

namespace Packets
{
    public enum PacketType
    {
        None = 0,
        ServerConnectionHeader = 1,
        KeepAlive = 2,
        KeepAliveResponse = 3,

        Entity = 100,
        WorldEntity = 101,
        EntityFull = 102,

        LoginCredentials = 121,
        LoginCredentialValid = 122,
        LoginClientReady = 123,
        LogoutClient = 124,

        PlayerSaveState = 131,
        UpdatePlayerSaveState = 132,

        CharacterFull = 141,
        PlayerFull = 142,
        EntityDestroy = 143,
        NPCFull = 144,

        RequestPacket = 170, 

        UserAccountRequest = 201,
        UserAccountResponse = 202,

        ProfileCreateCharacterRequest = 220,
        ProfileCreateCharacterResponse = 221,
        ProfileUpdateCharacter = 222,

        ServerIdPacket = 241,        
        ClientIdPacket = 242,
        ClientDisconnect = 243,
        ServerDisconnect = 244,
        ServerPingHopper = 245,

        ClientGameInfoRequest = 281,
        ClientGameInfoResponse = 282,

        // all packets from here on contain the server tick.
        ServerTick = 319,

        NPC_BTState = 320,
        NPC_BlackBoard = 321,

        Combat_AttackRequest = 360,// player only
        Combat_AttackOriginate = 361,// server only
        Combat_AttackStop = 362,

        /*  Combat_ApplyDOT = 363,
          Combat_RemoveDOT = 364,*/
        Combat_BuffApply = 365,// list of ints
        Combat_BuffRemove = 366,// list of ints
        //Combat_AilmentApply = 365,
        //Combat_BuffRemove = 366,

        Combat_HealthChange = 367,
        Combat_StaminaChange = 368,

        Entity_MoveTo = 390,
        Entity_MoveAway = 391,

        DataBlob = 401,
        
        RenderSettings = 500,
      /*  UserMove = 600,
        UserSetPositionAndOrientation = 601,
        UserCamera = 602,*/

        TestPacket = 1200
    }


    public class SocketPacketPair
    {
        public int connectionId;
        public int gameId;
        public BasePacket packet;

        public SocketPacketPair(int _socketId, BasePacket _packet)
        {
            connectionId = _socketId;
            packet = _packet;
            gameId = 0;
        }
        public SocketPacketPair(int _socketId, int _gameId, BasePacket _packet)
        {
            connectionId = _socketId;
            packet = _packet;
            gameId = _gameId;
        }
    }
    
    public abstract class BasePacket : IBinarySerializable
    {
        // Should construction of packets be allowed?
        // If false, calls to BasePacket() will throw an exception,
        // this is intended to catch code that creates packets rather than taking them
        // from the packet pool
        public static bool AllowConstruction = false;

        public virtual PacketType PacketType { get { return 0; } }

        // Is this packet currently waiting in a pool
        protected internal bool IsInPool = false;

        public BasePacket()
        {
            if (!AllowConstruction)
            {
                //throw new InvalidOperationException("Packet construction is not allowed");
            }
        }

        public virtual void Dispose() { }

        virtual public void Read(BinaryReader reader)
        {
            //reader.ReadInt32();// just to advance the reader
            //packetType = reader.ReadInt32();
        }
        virtual public void Write(BinaryWriter writer)
        {
            ushort type = (ushort) PacketType;
            writer.Write(type);
        }
        virtual public void CopyFrom(BasePacket packet)
        {
            if (!packet.GetType().Equals(GetType()))
            {
                throw new Exception(String.Format("Cannot CopyFrom a different class.  Us: {0}, Them: {1}", 
                    GetType().FullName, packet.GetType().FullName));
            }
        }
    }


    //Only the below are proper "BasePacket" packets.
    public class EntityPacket : BasePacket
    {
        public override PacketType PacketType { get { return PacketType.Entity; } }
        public int entityId;

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            entityId = reader.ReadInt32();
        }
        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(entityId);
        }
        public override void CopyFrom(BasePacket packet)
        {
            base.CopyFrom(packet);
            var typedPacket = (EntityPacket)packet;
            entityId = typedPacket.entityId;
        }
    }

    
    public class WorldEntityPacket : EntityPacket
    {
        public override PacketType PacketType { get { return PacketType.WorldEntity; } }

        public PositionCompressed position = new PositionCompressed();
        public RotationPacker rotation = new RotationPacker();

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);

            position.Read(reader);
            rotation.Read(reader);
        }
        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);

            position.Write(writer);
            rotation.Write(writer);
        }
        public override void CopyFrom(BasePacket packet)
        {
            base.CopyFrom(packet);
            var typedPacket = (WorldEntityPacket)packet;
            position.CopyFrom(typedPacket.position);
            rotation.CopyFrom(typedPacket.rotation);
        }
    }

    public class EntityFullPacket : WorldEntityPacket
    {
        public override PacketType PacketType { get { return PacketType.EntityFull; } }
    }

    public class CharacterFullPacket : EntityFullPacket
    {
        public override PacketType PacketType { get { return PacketType.CharacterFull; } }
        
        public string Name = "";

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            Name = reader.ReadString();
        }
        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(Name);
        }
        public override void CopyFrom(BasePacket packet)
        {
            base.CopyFrom(packet);
            var typedPacket = (CharacterFullPacket)packet;
            Name = typedPacket.Name;
        }
    }

    public class PlayerFullPacket : CharacterFullPacket
    {
        public override PacketType PacketType { get { return PacketType.PlayerFull; } }
    }

    public class EntityDestroyPacket : EntityPacket
    {
        public override PacketType PacketType { get { return PacketType.EntityDestroy; } }
    }

    public class NPCFullPacket : EntityFullPacket
    {
        public override PacketType PacketType { get { return PacketType.NPCFull; } }
        public int AgentID { get; set; }
        public int ConfigID { get; set; }

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            AgentID = reader.ReadInt32();
            ConfigID = reader.ReadInt32();
        }
        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(AgentID);
            writer.Write(ConfigID);
        }
        public override void CopyFrom(BasePacket packet)
        {
            base.CopyFrom(packet);
            var typedPacket = (NPCFullPacket)packet;
            AgentID = typedPacket.AgentID;
            ConfigID = typedPacket.ConfigID;
        }
    }
    public class ServerTick : BasePacket
    {
        public override PacketType PacketType { get { return PacketType.ServerTick; } }
        public int TickCount { get; set; }
        public short NumTicksSinceLastSend { get; set; }

        public ServerTick() : base() { }
        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            TickCount = reader.ReadInt32();
            NumTicksSinceLastSend = reader.ReadInt16();
        }
        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(TickCount);
            writer.Write(NumTicksSinceLastSend);
        }
        public override void CopyFrom(BasePacket packet)
        {
            base.CopyFrom(packet);
            var typedPacket = (ServerTick)packet;
            TickCount = typedPacket.TickCount;
            NumTicksSinceLastSend = typedPacket.NumTicksSinceLastSend;
        }
    }
    
 /*   public class PacketHistoryBucket
    {
        int frameId;
        List<BasePacket> packetsByEntityId;

        public bool AddPacket(BasePacket bp);
        public void GetPackets(int entityId, ref List<BasePacket> packets);
    }
    public class PacketHistoryManager
    {
        public int HistoryLength = 20;
        Dictionary<tickId, PacketHistoryBucket>...
        public void SetFrameId(int frameId);
        List<BasePacket> GetPacketHistory(int entityId); // only entity packets

        List<BasePacket> GetPacketHistory(int tickId); // only entity packets
    }*/

    //Todo: Break this up a bit, before it becomes too large to maintain.
    

}