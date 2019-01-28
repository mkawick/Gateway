using System;
using Packets;
using CommonLibrary;
using System.Collections.Generic;

namespace Client.Game
{
    public class ClientNetworking : IClientNetworking
    {
        public int FrameID { get; private set; }

        public event Action OnConnect;
        public event Action<bool> OnDisconnect;
        public event Action<LoginResponse> OnLoginResponse;
        public event Action OnTick;

        // State of our connection, and packet buffer
        private ClientPlayerConnectionState cpcs;

        private PacketDispatcher packetDispatcher;

        private long nextClientTick;
        private int lastServerTickFrameId;
        private long lastServerTickPacketArrivalTime;
        //private float estimatedServerFrameTime = 0f;

        public ClientNetworking(SocketWrapperSettings settings, int appId)
        {
            FrameID = NetworkConstants.BeforeStartOfGameFrameId;

            packetDispatcher = new PacketDispatcher();
            cpcs = new ClientPlayerConnectionState(new SocketWrapper(settings), appId);

            // Forward events from the CPCS to IClientNetworking
            cpcs.OnConnect += () => { OnConnect?.Invoke(); };
            cpcs.OnDisconnect += (retry) => { OnDisconnect?.Invoke(retry); };
            cpcs.OnLoginResponse += (response) => { OnLoginResponse?.Invoke(response); };

            // We want to know when the server tells us it's frameid, so we can keep in sync
            packetDispatcher.AddListener<ServerTick>(OnServerTickPacket);
        }

        //----------------------------------------------------------------------

        public void Connect()
        {
            cpcs.ConnectClient();
        }

        public bool IsConnected()
        {
            return cpcs.IsConnected();
        }

        public void Disconnect()
        {
            cpcs.EndClient();
        }

        public void SendLogin(string username, string password)
        {
            cpcs.Login(username, password);
        }

        public bool IsLoggedIn { get { return cpcs.IsLoggedIn; } }

        public void Send(BasePacket packet)
        {
            cpcs.Send(packet);
        }

        public void ProcessReceivedPackets()
        {
            // Fire listeners for all packets received since the last ProcessReceivedPackets call
            SignalPacketListeners(cpcs.TakeUnprocessedPackets());

            // Fire as many OnTick events as we need to keep up with the server
            UpdateFrameId();
        }

        private void SignalPacketListeners(IEnumerable<BasePacket> packets)
        {
            foreach (var packet in packets)
            {
                if (packet is EntityPacket)
                {
                    packetDispatcher.SignalListener((packet as EntityPacket).entityId, packet);
                }
                packetDispatcher.SignalListener(packet);
                IntrepidSerialize.ReturnToPool(packet);
            }
        }

        private void OnServerTickPacket(ServerTick packet)
        {
            //var now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            // If this is our first packet, just set the frame id
            if (FrameID == NetworkConstants.BeforeStartOfGameFrameId)
            {
                FrameID = packet.TickCount;
            }
            // We've had at least two ticks, so estimate a new server frame time
            //else
            //{
            //    // the number of ticks we need to get through, is the number elapsed on the server
            //    // plus the difference between our frameId and the server frame id, in order to speed
            //    // or slow down if we're not quite in sync
            //    int ticksUntilNextPacket = packet.NumTicksSinceLastSend + (packet.TickCount - FrameID);

            //    // frame time = elapsed time / number of frames
            //    estimatedServerFrameTime = ((float)(now - lastServerTickPacketArrivalTime)) / ticksUntilNextPacket;
            //    Console.WriteLine("Got server tick: server frameId:{0}, our frameId:{1}, ticksUntilNext:{2}, serverFrameTime:{3}", packet.TickCount, FrameID, ticksUntilNextPacket, estimatedServerFrameTime);
            //}
            lastServerTickFrameId = packet.TickCount;
            //nextClientTick = now + (int)estimatedServerFrameTime;
            //lastServerTickPacketArrivalTime = now;
            //Console.WriteLine("Server tick: {0}", packet.TickCount);
        }

        private void UpdateFrameId()
        {
            // Can't tick until we get our first tick packet
            if (FrameID == NetworkConstants.BeforeStartOfGameFrameId)
            {
                return;
            }

            /*Console.WriteLine("Attempting tick");
            // Don't tick until we're ready
            var now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            if (now < nextClientTick)
            {
                return;
            }

            // Work out how many ticks we need
            int elapsedTicks = 1;
            if (estimatedServerFrameTime > 0f)
            {
                elapsedTicks += (int)((now - nextClientTick) / estimatedServerFrameTime);
            }*/

            int elapsedTicks = lastServerTickFrameId - FrameID;

            // Perform that number of ticks
            for (int i = 0; i < elapsedTicks; i++)
            {
                //Console.WriteLine("Doing tick: frameId {0}", FrameID);
                OnTick?.Invoke();
                FrameID++;
            }

            // Schedule next tick
            //nextClientTick = now + (int)estimatedServerFrameTime;
        }

        public long AddListener<PACKET_TYPE>(int entityID, Action<PACKET_TYPE> action) where PACKET_TYPE : BasePacket
        {
            return packetDispatcher.AddListener(entityID, action);
        }

        public long AddListener<PACKET_TYPE>(Action<PACKET_TYPE> action) where PACKET_TYPE : BasePacket
        {
            return packetDispatcher.AddListener(action);
        }

        public void RemoveListener<PACKET_TYPE>(int entityId, long listenerHandle) where PACKET_TYPE : BasePacket
        {
            packetDispatcher.RemoveListener<PACKET_TYPE>(entityId, listenerHandle);
        }

        public void RemoveListener<PACKET_TYPE>(long listenerHandle) where PACKET_TYPE : BasePacket
        {
            packetDispatcher.RemoveListener<PACKET_TYPE>(listenerHandle);
        }

        public void RemoveListenersForEntity(int entityId)
        {
            packetDispatcher.RemoveListenersForEntity(entityId);
        }
    }
}
