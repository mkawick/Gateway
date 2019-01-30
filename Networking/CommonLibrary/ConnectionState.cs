using Packets;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Diagnostics;
using System;
using System.Reflection;

namespace CommonLibrary
{
    public class ConnectionState
    {
        protected SocketWrapper socket;

        protected object packetLock = new object();
        protected List<BasePacket> deserializedPackets = new List<BasePacket>();

        public uint gameId = 0;
        public bool versionAndHandshakeComplete = false;

        /// <summary>
        /// How long we wait for a response to a KeepAlive until we regarded is
        /// as having timed out
        /// </summary>
        public int secsSinceKeepAliveToTimedOut = 5;
        /// <summary>
        /// How long between sending KeepAlive packets
        /// </summary>
        public int secsBetweenKeepAlives = 10;

        public bool IsAwaitingKeepAlive { get; private set; }
        public virtual bool IsKeepAliveValid ()// can be initialized to false if we like
        {
            return true;
        }

        private Stopwatch timestampOfLastKeepAlive = new Stopwatch();
        
        public ConnectionState(Socket handler)
        {
            socket = new SocketWrapper(handler);
            socket.OnPacketsReceived += Socket_OnPacketsReceived;
            socket.Connect(); // actually start receiving
        }

        public ConnectionState(Socket handler,
            int bufferSize,
            int maxRetryAttempts,
            long millisBetweenRetries)
        {
            socket = new SocketWrapper(handler, bufferSize, maxRetryAttempts, millisBetweenRetries);
            socket.OnPacketsReceived += Socket_OnPacketsReceived;
            socket.Connect(); // actually start receiving
        }

        private void Socket_OnPacketsReceived(IPacketSend externalSocket, Queue<BasePacket> packets)
        {
            if (packets.Count == 1)
            {
                BasePacket packet = packets.Dequeue();
                if (packet is ServerDisconnectPacket)
                {
                    socket.Disconnect();
                    return;
                }

                if (packet is KeepAliveResponse)
                {
                    KeepAliveReceived();
                }                
                else
                {
                    lock (packetLock)
                    {
                        deserializedPackets.Add(packet);
                    }
                }
            }
            else
            {
                lock (packetLock)
                {
                    deserializedPackets.AddRange(packets);
                }
            }
        }
        
        public bool HasNewData()
        {
            // CS: Probably don't need to lock for this?
            return deserializedPackets.Count > 0;
        }

        public bool MarkedAsSocketClosed
        {
            get { return !socket.IsConnected; }
        }

        #region KeepAlive

        /// <summary>
        /// Sends a keep alive if appropriate, and checks for a timeout
        /// if we've sent one already.
        /// This is designed to be called repeatedly.  When it returns
        /// false, the keep alive has timed out.
        /// </summary>
        /// <returns></returns>
        public bool KeepAlive()
        {
#if NO_KEEPALIVE
            return true;
#endif
            if (MarkedAsSocketClosed)
            {
                return false;
            }

            if (IsAwaitingKeepAlive)
            {
                if (HasKeepAliveExpired())
                {
#if !DEBUG
                    return false;
#endif
                }
            }
            if (!timestampOfLastKeepAlive.IsRunning)
            {
                // We've not yet sent a keep alive, so wait for a bit
                timestampOfLastKeepAlive.Start();
            }
            else if (timestampOfLastKeepAlive.Elapsed.Seconds >= secsBetweenKeepAlives)
            {
                // We're still connected, and it's been a while since our last keep alive
                SendKeepAlive();
            }
            return true;
        }

        public bool HasKeepAliveExpired()
        {
            if (socket.IsConnected == false)
                return true;

            if (IsAwaitingKeepAlive == false)
                return false;

            TimeSpan ts = timestampOfLastKeepAlive.Elapsed;
            if (ts.Seconds > secsSinceKeepAliveToTimedOut)
                return true;

            return false;
        }

        private void SendKeepAlive()
        {
            if (IsAwaitingKeepAlive == true)
                return;

            IsAwaitingKeepAlive = true;
            timestampOfLastKeepAlive.Restart();
            KeepAlive ka = (KeepAlive)IntrepidSerialize.TakeFromPool(PacketType.KeepAlive);
            Send(ka);
        }

        private void KeepAliveReceived()
        {
            IsAwaitingKeepAlive = false;
            timestampOfLastKeepAlive.Restart();
        }

#endregion KeepAlive
        /// <summary>
        /// Gets all unprocessed packets, and clears the unprocessed packet list
        /// </summary>
        public List<BasePacket> RetrieveData()
        {
            List<BasePacket> result;
            lock (packetLock)
            {
                result = deserializedPackets;
                deserializedPackets = new List<BasePacket>();
            }
            return result;
        }

        /// <summary>
        /// Gets the oldest unprocessed packet, or null if no such packet exists.
        /// </summary>
        /// <returns></returns>
        public BasePacket RetrievePacket()
        {
            BasePacket result = null;
            lock (packetLock)
            {
                if (deserializedPackets.Count > 0)
                {
                    result = deserializedPackets[0];
                    deserializedPackets.RemoveAt(0);
                }
            }
            return result;
        }

        public void Send(BasePacket packet)
        {
            socket.Send(packet);
        }
        
        public void Disconnect()
        {
            socket.Disconnect();
        }
    }
}
