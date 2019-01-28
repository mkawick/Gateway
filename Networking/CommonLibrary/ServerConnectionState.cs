using System.Collections.Generic;
using System.Net.Sockets;
using Packets;
using System.Net;
using System.Reflection;

namespace CommonLibrary
{
    public class ServerConnectionState : ConnectionState
    { 
        public ServerIdPacket.ServerType serverType = ServerIdPacket.ServerType.None;
        public IPEndPoint remoteIpEndPoint = null;
        public bool skipNextPacket = false;
        public List<int> clientConnectionIds = new List<int>();// temporary design... should be pulled into a server-connection
        public void AddConnection(int connId) { if (clientConnectionIds.IndexOf(connId) != -1) return; clientConnectionIds.Add(connId); }
        public void RemoveConnection(int connId) { if (clientConnectionIds.IndexOf(connId) == -1) return; clientConnectionIds.Remove(connId); }

        public ServerConnectionState(Socket handler,
            int bufferSize = 51200,
            int maxRetryAttempts = 0,
            long millisBetweenRetries = 0) : base(handler, bufferSize, maxRetryAttempts, millisBetweenRetries)
        {
            remoteIpEndPoint = handler.RemoteEndPoint as IPEndPoint;
        }

        public bool ProcessIdPackets()// TODO: only process id packets
        {
            List<BasePacket> packets = RetrieveData();
            if (packets.Count == 0)
            {
                // We're still waiting for their packet
                return true;
            }
            else if (packets.Count == 1)
            {
                BasePacket packet = packets[0];
                ServerIdPacket id = packet as ServerIdPacket;
                if (id != null)
                {
                    gameId = id.Id;
                    serverType = id.Type;
                    int mapId = id.MapId;
                    versionAndHandshakeComplete = true;
                    IntrepidSerialize.ReturnToPool(packet);
                    return true;
                }
           /*     if (packet is ServerPingHopperPacket)
                {
                    HandleServerHopping(packet as ServerPingHopperPacket);
                    return true;
                }*/
                IntrepidSerialize.ReturnToPool(packet);
            }

            // If we're here, it means we either received more than one packet
            // or it wasn't a ServerIdPacket, which means the server isn't behaving
            // properly
            return false;
        }

        public IPAddress Address { get { return remoteIpEndPoint.Address; } }

        void HandleServerHopping(ServerPingHopperPacket packet)
        {
            // TODO... not correct
            ServerPingHopperPacket hopper = packet as ServerPingHopperPacket;
            string name = Assembly.GetCallingAssembly().GetName().Name;
            hopper.Stamp(name + " received");
            Send(packet);
        }
    }
    
}
