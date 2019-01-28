using CommonLibrary;
using Packets;
using System;
using System.Collections.Generic;

namespace Server
{
    public class ProfileServerProxy
    {
        private SocketWrapper socket;
        private object packetLock = new object();
        private Queue<BasePacket> incomingPackets;
        private Queue<BasePacket> outgoingPackets;

        public event Action<BasePacket> OnReceivePacket;

        private static int fakeCharacterId = 1024;
        
        public ProfileServerProxy(SocketWrapperSettings socketSettings)
        {
            incomingPackets = new Queue<BasePacket>();
            outgoingPackets = new Queue<BasePacket>();
            
            if (socketSettings != null)
            {
                socket = new SocketWrapper(socketSettings);
                socket.OnPacketsReceived += ProfileServer_OnPacketsReceived;
            }
        }

        public void Connect()
        {
            socket?.Connect();
        }

        public void Tick()
        {
            ForwardAllOutgoingPackets();
            ForwardAllIncomingPackets();
        }

        public void Send(BasePacket packet)
        {
            lock(packetLock)
            {
                outgoingPackets.Enqueue(packet);
            }
        }

        private void ProfileServer_OnPacketsReceived(IPacketSend socket, Queue<BasePacket> packets)
        {
            lock (packetLock)
            {
                foreach (var packet in packets)
                {
                    incomingPackets.Enqueue(packet);
                }
            }
        }

        private void ForwardAllOutgoingPackets()
        {
            Queue<BasePacket> packets;
            lock (packetLock)
            {
                packets = outgoingPackets;
                outgoingPackets = new Queue<BasePacket>();
            }
            foreach (var packet in packets)
            {
                if (socket != null)
                {
                    socket.Send(packet);
                }
                else
                {
                    CreateFakeResponse(packet);
                }
            }
        }

        private void ForwardAllIncomingPackets()
        {
            Queue<BasePacket> packets;
            lock (packetLock)
            {
                packets = incomingPackets;
                incomingPackets = new Queue<BasePacket>();
            }
            foreach (var packet in packets)
            {
                OnReceivePacket?.Invoke(packet);
            }
        }

        private void CreateFakeResponse(BasePacket packet)
        {
            // Fake a response
            ProfileCreateCharacterRequest request = packet as ProfileCreateCharacterRequest;
            if (request != null)
            {
                ProfileCreateCharacterResponse response = (ProfileCreateCharacterResponse)IntrepidSerialize.TakeFromPool(PacketType.ProfileCreateCharacterResponse);
                response.accountId = request.accountId;
                response.characterId = fakeCharacterId++;
                lock (packetLock)
                {
                    incomingPackets.Enqueue(response);
                }
            }
        }

        public void Disconnect()
        {
            socket?.Disconnect();
        }
    }
}
