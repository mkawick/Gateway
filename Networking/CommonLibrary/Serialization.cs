﻿using Packets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace CommonLibrary
{
    public class IntrepidSerialize
    {
        static Dictionary<PacketType, Func<BasePacket>> listOfConstructors = null;
        static PacketPoolManager packetPoolManager;
        static BufferPoolManager bufferPool;

        static IntrepidSerialize()
        {
            Init();
        }

        public static void Init()
        {
            // Initialize the factories and pools before
            // anyone asks us to create packets or deserialize anything
            SetupPacketFactory();
            SetupPacketPoolManager();
            SetupBuffers();
        }

        //---------------------------- packet allocation -------------------------------
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
            if (listOfConstructors.ContainsKey(type) == false)
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
        public static byte[] AllocateBuffer (int size = NetworkConstants.DataBlobMaxPacketSize)
        {
            return bufferPool.Allocate();
        }
        public static void ReturnBufferToPool(byte[] buffer)
        {
            bufferPool.Free(buffer);
        }

        //------------------------------------ setup -----------------------------------
        static void SetupPacketPoolManager()
        {
            if (packetPoolManager != null)
                return;
            packetPoolManager = new PacketPoolManager();
        }
        static void SetupBuffers()
        {
            bufferPool = new BufferPoolManager();
        }
        static void SetupPacketFactory()
        {
            if (listOfConstructors != null)
                return;

            listOfConstructors = new Dictionary<PacketType, Func<BasePacket>>();

            listOfConstructors.Add(PacketType.ServerConnectionHeader, () => { return new ServerConnectionHeader(); });
            listOfConstructors.Add(PacketType.KeepAlive, () => { return new KeepAlive(); });
            listOfConstructors.Add(PacketType.KeepAliveResponse, () => { return new KeepAliveResponse(); });
            listOfConstructors.Add(PacketType.Entity, () => { return new EntityPacket(); });
            listOfConstructors.Add(PacketType.WorldEntity, () => { return new WorldEntityPacket(); });
            listOfConstructors.Add(PacketType.EntityFull, () => { return new EntityFullPacket(); });

            listOfConstructors.Add(PacketType.LoginCredentials, () => { return new LoginCredentials(); });
            listOfConstructors.Add(PacketType.LoginCredentialValid, () => { return new LoginCredentialValid(); });
            listOfConstructors.Add(PacketType.LoginClientReady, () => { return new LoginClientReady(); });
            listOfConstructors.Add(PacketType.LogoutClient, () => { return new LogoutClient(); });

            listOfConstructors.Add(PacketType.PlayerSaveState, () => { return new PlayerSaveStatePacket(); });
            listOfConstructors.Add(PacketType.UpdatePlayerSaveState, () => { return new UpdatePlayerSaveStatePacket(); });

            listOfConstructors.Add(PacketType.CharacterFull, () => { return new CharacterFullPacket(); });
            listOfConstructors.Add(PacketType.PlayerFull, () => { return new PlayerFullPacket(); });
            listOfConstructors.Add(PacketType.EntityDestroy, () => { return new EntityDestroyPacket(); });
            listOfConstructors.Add(PacketType.NPCFull, () => { return new NPCFullPacket(); });

            listOfConstructors.Add(PacketType.RequestPacket, () => { return new RequestPacket(); });

            listOfConstructors.Add(PacketType.UserAccountRequest, () => { return new UserAccountRequest(); });
            listOfConstructors.Add(PacketType.UserAccountResponse, () => { return new UserAccountResponse(); });

            listOfConstructors.Add(PacketType.ProfileCreateCharacterRequest, () => { return new ProfileCreateCharacterRequest(); });
            listOfConstructors.Add(PacketType.ProfileCreateCharacterResponse, () => { return new ProfileCreateCharacterResponse(); });
            listOfConstructors.Add(PacketType.ProfileUpdateCharacter, () => { return new ProfileUpdateCharacter(); });

            listOfConstructors.Add(PacketType.ServerIdPacket, () => { return new ServerIdPacket(); });
            listOfConstructors.Add(PacketType.ClientIdPacket, () => { return new ClientIdPacket(); });
            listOfConstructors.Add(PacketType.ClientDisconnect, () => { return new ClientDisconnectPacket(); });
            listOfConstructors.Add(PacketType.ServerDisconnect, () => { return new ServerDisconnectPacket(); });
            listOfConstructors.Add(PacketType.ServerPingHopper, () => { return new ServerPingHopperPacket(); });


            listOfConstructors.Add(PacketType.ClientGameInfoRequest, () => { return new ClientGameInfoRequest(); });
            listOfConstructors.Add(PacketType.ClientGameInfoResponse, () => { return new ClientGameInfoResponse(); });

            listOfConstructors.Add(PacketType.ServerTick, () => { return new ServerTick(); });
            listOfConstructors.Add(PacketType.NPC_BTState, () => { return new NPC_BTState(); });
            listOfConstructors.Add(PacketType.NPC_BlackBoard, () => { return new NPC_BlackBoard(); });

            listOfConstructors.Add(PacketType.Combat_AttackRequest, () => { return new Combat_AttackRequest(); });
            listOfConstructors.Add(PacketType.Combat_AttackOriginate, () => { return new Combat_AttackOriginate(); });
            listOfConstructors.Add(PacketType.Combat_AttackStop, () => { return new Combat_AttackStop(); });

            listOfConstructors.Add(PacketType.Combat_BuffApply, () => { return new Combat_BuffApply(); });
            listOfConstructors.Add(PacketType.Combat_BuffRemove, () => { return new Combat_BuffRemove(); });

            listOfConstructors.Add(PacketType.Combat_HealthChange, () => { return new Combat_HealthChange(); });
            listOfConstructors.Add(PacketType.Combat_StaminaChange, () => { return new Combat_StaminaChange(); });

            listOfConstructors.Add(PacketType.Entity_MoveTo, () => { return new Entity_MoveTo(); });
            listOfConstructors.Add(PacketType.Entity_MoveAway, () => { return new Entity_MoveAway(); });
            listOfConstructors.Add(PacketType.DataBlob, () => { return new DataBlob(); });

            listOfConstructors.Add(PacketType.RenderSettings, () => { return new RenderSettings(); });

            listOfConstructors.Add(PacketType.TestPacket, () => { return new TestPacket(); });

        }
        //-------------------------------- deseriialize --------------------------------
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
#if DEBUG_NETWORK_PACKETS
                            int numBytesToRead =
#endif
                            Network.Utils.SetupRead(reader);

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
