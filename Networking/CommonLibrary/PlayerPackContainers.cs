using Packets;
using System.Collections.Generic;


namespace CommonLibrary
{
    public interface PlayerConnectedServer
    {
        void AddOutgoingPacket(BasePacket packet, int socketId);
        void AddIncomingPacket(BasePacket packet, int socketId);
    }

    public class PacketQueues
    {
        public Queue<SocketPacketPair> incomingPackets;
        public Queue<SocketPacketPair> outgoingPackets;
        public PacketQueues()
        {
            incomingPackets = new Queue<SocketPacketPair>();
            outgoingPackets = new Queue<SocketPacketPair>();
        }
    }
}