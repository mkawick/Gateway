using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PacketTypes;
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

        TestPacket = 501
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

        virtual public void Write(BinaryWriter writer)
        {
            ushort type = (ushort) PacketType;
            writer.Write(type);
        }
        virtual public void Read(BinaryReader reader)
        {
            //reader.ReadInt32();// just to advance the reader
            //packetType = reader.ReadInt32();
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

        public PositionPacker position = new PositionPacker();
        public RotationPacker rotation = new RotationPacker();

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);

            position.Write(writer);
            rotation.Write(writer);
        }
        public override void Read(BinaryReader reader)
        {
            base.Read(reader);

            position.Read(reader);
            rotation.Read(reader);
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
        
        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(Name);
        }

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            Name = reader.ReadString();
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

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(AgentID);
            writer.Write(ConfigID);
        }

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            AgentID = reader.ReadInt32();
            ConfigID = reader.ReadInt32();
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
        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(TickCount);
            writer.Write(NumTicksSinceLastSend);
        }

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            TickCount = reader.ReadInt32();
            NumTicksSinceLastSend = reader.ReadInt16();
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
    public class IntrepidSerialize
    {
        static Dictionary<PacketType, Func<BasePacket>> listOfConstructors = null;
        static PacketPoolManager packetPoolManager;

        static IntrepidSerialize()
        {
            // Initialize the factories and pools before
            // anyone asks us to create packets or deserialize anything
            SetupPacketFactory();
            SetupPacketPoolManager();
        }

        public static BasePacket TakeFromPool(PacketType type)
        {
            return packetPoolManager.Allocate(type);
        }
        public static void ReturnToPool(BasePacket bp)
        {

            packetPoolManager.Deallocate(bp);
        }
        public static void ReturnToPool(List<BasePacket> packets)
        {
            for (int i = 0; i < packets.Count; i++)
            {
                packetPoolManager.Deallocate(packets[i]);
            }
        }
        public static BasePacket CreatePacket(PacketType type)
        {
            if(listOfConstructors.ContainsKey(type) == false)
            {
                Console.WriteLine("Missing create packet dictionary lookup {0}", type.ToString());
                throw new Exception("Missing create packet dictionary lookup");
            }
            return listOfConstructors[type].Invoke();
        }
        public static PACKET_TYPE ReplicatePacket<PACKET_TYPE>(PACKET_TYPE packet) where PACKET_TYPE : BasePacket
        {
            PACKET_TYPE newPacket = (PACKET_TYPE)TakeFromPool(packet.PacketType);
            newPacket.CopyFrom(packet);
            return newPacket;
        }

        static void SetupPacketPoolManager()
        {
            if (packetPoolManager != null)
                return;
            packetPoolManager = new PacketPoolManager();
        }
        static void SetupPacketFactory()
        {
            if (listOfConstructors != null)
                return;

            listOfConstructors = new Dictionary<PacketType, Func<BasePacket>>();

            listOfConstructors.Add(PacketType.ServerConnectionHeader, () =>             { return new ServerConnectionHeader(); });
            listOfConstructors.Add(PacketType.KeepAlive, () =>                          { return new KeepAlive(); });
            listOfConstructors.Add(PacketType.KeepAliveResponse, () =>                  { return new KeepAliveResponse(); });
            listOfConstructors.Add(PacketType.Entity, () =>                             { return new EntityPacket(); });
            listOfConstructors.Add(PacketType.WorldEntity, () =>                        { return new WorldEntityPacket(); });
            listOfConstructors.Add(PacketType.EntityFull, () =>                         { return new EntityFullPacket(); });
            
            listOfConstructors.Add(PacketType.LoginCredentials, () =>                   { return new LoginCredentials(); });
            listOfConstructors.Add(PacketType.LoginCredentialValid, () =>               { return new LoginCredentialValid(); });
            listOfConstructors.Add(PacketType.LoginClientReady, () =>                   { return new LoginClientReady(); });
            listOfConstructors.Add(PacketType.LogoutClient, () =>                       { return new LogoutClient(); });

            listOfConstructors.Add(PacketType.PlayerSaveState, () =>                    { return new PlayerSaveStatePacket(); });
            listOfConstructors.Add(PacketType.UpdatePlayerSaveState, () =>              { return new UpdatePlayerSaveStatePacket(); });

            listOfConstructors.Add(PacketType.CharacterFull, () =>                      { return new CharacterFullPacket(); });
            listOfConstructors.Add(PacketType.PlayerFull, () =>                         { return new PlayerFullPacket(); });
            listOfConstructors.Add(PacketType.EntityDestroy, () =>                      { return new EntityDestroyPacket(); });
            listOfConstructors.Add(PacketType.NPCFull, () =>                            { return new NPCFullPacket(); });

            listOfConstructors.Add(PacketType.UserAccountRequest, () =>                 { return new UserAccountRequest(); });
            listOfConstructors.Add(PacketType.UserAccountResponse, () =>                { return new UserAccountResponse(); });

            listOfConstructors.Add(PacketType.ProfileCreateCharacterRequest, () =>      { return new ProfileCreateCharacterRequest(); });
            listOfConstructors.Add(PacketType.ProfileCreateCharacterResponse, () =>     { return new ProfileCreateCharacterResponse(); });
            listOfConstructors.Add(PacketType.ProfileUpdateCharacter, () =>             { return new ProfileUpdateCharacter(); });

            listOfConstructors.Add(PacketType.ServerIdPacket, () =>                     { return new ServerIdPacket(); });
            listOfConstructors.Add(PacketType.ClientIdPacket, () =>                     { return new ClientIdPacket(); });
            listOfConstructors.Add(PacketType.ClientDisconnect, () =>                   { return new ClientDisconnectPacket(); });
            listOfConstructors.Add(PacketType.ServerDisconnect, () =>                   { return new ServerDisconnectPacket(); });
            listOfConstructors.Add(PacketType.ServerPingHopper, () =>                   { return new ServerPingHopperPacket(); });
            

            listOfConstructors.Add(PacketType.ClientGameInfoRequest, () =>              { return new ClientGameInfoRequest(); });
            listOfConstructors.Add(PacketType.ClientGameInfoResponse, () =>             { return new ClientGameInfoResponse(); });

            listOfConstructors.Add(PacketType.ServerTick, () =>                         { return new ServerTick(); });
            listOfConstructors.Add(PacketType.NPC_BTState, () =>                        { return new NPC_BTState(); });
            listOfConstructors.Add(PacketType.NPC_BlackBoard, () =>                     { return new NPC_BlackBoard(); });

            listOfConstructors.Add(PacketType.Combat_AttackRequest, () =>               { return new Combat_AttackRequest(); });
            listOfConstructors.Add(PacketType.Combat_AttackOriginate, () =>             { return new Combat_AttackOriginate(); });
            listOfConstructors.Add(PacketType.Combat_AttackStop, () =>                  { return new Combat_AttackStop(); });

            listOfConstructors.Add(PacketType.Combat_BuffApply, () =>                   { return new Combat_BuffApply(); });
            listOfConstructors.Add(PacketType.Combat_BuffRemove, () =>                  { return new Combat_BuffRemove(); });

            listOfConstructors.Add(PacketType.Combat_HealthChange, () =>                { return new Combat_HealthChange(); });
            listOfConstructors.Add(PacketType.Combat_StaminaChange, () =>               { return new Combat_StaminaChange(); });

            listOfConstructors.Add(PacketType.Entity_MoveTo, () =>                      { return new Entity_MoveTo(); });
            listOfConstructors.Add(PacketType.Entity_MoveAway, () =>                    { return new Entity_MoveAway(); });
            listOfConstructors.Add(PacketType.TestPacket, () =>                         { return new TestPacket(); });
            

        }
        public static List<BasePacket> Deserialize(byte[] bytes, int maxBufferSize, ref int amountRead)
        {
            List<BasePacket> storage = new List<BasePacket>();
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    long len = maxBufferSize;
                    // We've got no way of telling if the buffer ends with an incomplete packet,
                    // but we want to deserialize all of the packets that are in the buffer, so
                    // rather than leaving a bit at the end (which is what the commented out code does),
                    // we're just going to handle the error.
                    // TODO: Update the packet format to include information about length where appropriate.
                    /*if(len > 512)// never read until the absolute end.
                    {
                        len = maxBufferSize - 256;
                    }*/
                    Debug.Assert(len > 0);

                    // Catch us falling off the end of the stream - this should only happen
                    // if we've received an incomplete packet
                    try
                    {
                        bool hadParseError = false;
                        while (reader.BaseStream.Position < len)
                        {
                            // Record the position before we attempt to read
                            // a packet.  If the read fails, this ensures the next time
                            // we start the read again from the start of the packet.
                            // critically, this must be the first thing that we do for each packet read.
                            amountRead = (int)reader.BaseStream.Position;
                            /*int numBytesToRead =*/ Network.Utils.SetupRead(reader);                            

                            ushort packetTypeId = reader.ReadUInt16();
                            var packetType = (PacketType)packetTypeId;
                            BasePacket packet = null;
                            // TODO: Replace this with something that looks up everything that descends from
                            // BasePacket, and creates new instances
                            if (listOfConstructors.ContainsKey(packetType) == true)
                            {
                                //packet = listOfConstructors[packetType].Invoke();
                                packet = TakeFromPool(packetType);
                            }
                            else
                            {
                                Console.WriteLine("Unhandled packet type received: {0}", packetTypeId);
                                hadParseError = true;
                            }
                            if (packet != null)
                            {
                                packet.Read(reader);
                                storage.Add(packet);
#if DEBUG_NETWORK_PACKETS
                                if (DebugLogPacket(packet))
                                {
                                    Console.WriteLine("Received {0}", packet.GetType());
                                }
#endif
                            }
                            else
                            {
                                break;
                            }
                        }
                        if (!hadParseError)
                        {
                            // We've read the whole buffer, so record the final amount read
                            amountRead = (int)reader.BaseStream.Position;
                        }
                    }
                    catch (EndOfStreamException)
                    {
                        // We've got an incomplete packet at the end of our buffer.
                        // The amountRead points to the start of that incomplete packet,
                        // so we can just continue.
#if DEBUG_NETWORK_PACKETS
                        Console.WriteLine("Incomplete packet received");
#endif
                    }
                }
            }

            return storage;
        }

#if DEBUG_NETWORK_PACKETS
        public static bool DebugLogPacket(BasePacket packet)
        {
            //Prevent spam
            bool logPacket = true;
#if !DEBUG_WORLD_ENTITY_PACKETS
            logPacket &= packet.PacketType != PacketType.WorldEntity;
#endif
#if !DEBUG_KEEP_ALIVE_PACKETS
            logPacket &= packet.PacketType != PacketType.KeepAlive;
            logPacket &= packet.PacketType != PacketType.KeepAliveResponse;
#endif
#if !DEBUG_CONNECTION_ID_PACKETS
            logPacket &= packet.PacketType != PacketType.ServerConnectionHeader;
#endif
#if !DEBUG_SERVER_TICK_PACKETS
            logPacket &= packet.PacketType != PacketType.ServerTick;
#endif
            return logPacket;
        }
#endif

    }

}