using CommonLibrary;
using Packets;
using Server.Game;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Server
{
    public class ConnectedClient
    {
        public int AccountId { get; protected set; }

        public int EntityId { get; protected set; }
        // Id for the player's socket
        public int SocketId { get; protected set; }

        private ServerNetworking gameServer;

        protected List<BasePacket> outgoingPackets;
        protected List<BasePacket> incomingPackets;

        public PlayerConnectedServer connectedServer;
        public bool isDisconnected = false;

        public PlayerSaveState saveState;

        public ConnectedClient(int entityId, int socketId, ServerNetworking gameServer, PlayerSaveState saveState)
        {
            outgoingPackets = new List<BasePacket>();
            incomingPackets = new List<BasePacket>();
            SocketId = socketId;
            EntityId = entityId;
            this.saveState = saveState;
            this.gameServer = gameServer;
        }


        protected bool HasDataToSend()
        {
            return outgoingPackets.Count > 0;
        }

        public void AddOutgoingPacket(BasePacket bp)
        {
            outgoingPackets.Add(bp);
        }

        protected bool HasDataToProcess()
        {
            return incomingPackets.Count > 0;
        }

        public void AddIncomingPacket(BasePacket bp)
        {
            lock (incomingPackets)
            {
                incomingPackets.Add(bp);
            }
        }

        private void Disconnect()
        {
            gameServer.DisconnectPlayer(SocketId);
        }

        public virtual void ProcessOutgoingData()
        {
            if (HasDataToSend())
            {
                for (int i = 0; i < outgoingPackets.Count; i++)
                {
                    gameServer.AddOutgoingPacket(outgoingPackets[i], SocketId);
                }
                outgoingPackets.Clear();
            }
        }

        public virtual void ProcessIncomingData()
        {
            if (HasDataToProcess())
            {
                List<BasePacket> arrayOfStuff;
                lock (incomingPackets)
                {
                    arrayOfStuff = incomingPackets;
                    incomingPackets = new List<BasePacket>();
                }
                for (int i = 0; i < arrayOfStuff.Count; i++)
                {
                    var packet = arrayOfStuff[i];
                    if (packet is ClientDisconnectPacket)
                    {
                        Disconnect();
                        break;
                    }
                    // CS: We don't signal entity packets to their given entity id,
                    // as we expect the client to only send entity packets for their
                    // own entity id - we check it here..
#if DEBUG
                    if (packet is EntityPacket)
                    {
                        if ((packet as EntityPacket).entityId != EntityId)
                        {
                            Console.Error.WriteLine("Client sent malformed entity packet: {0}", packet.PacketType);
                            continue;
                        }
                    }
#endif
                    gameServer.SignalListener(EntityId, packet);
                    gameServer.SignalListener(packet);

                    //TODO: Return packets to the pool?
                }
            }
        }
    }
}