//#define DEBUG_NETWORK_PACKETS
//#define DEBUG_NETWORK_STREAM

using Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace CommonLibrary
{

    public interface IPacketSend
    {
        /// <summary>
        /// The socket has established a connection with the remote host.
        /// </summary>
        event Action<IPacketSend> OnConnect;

        /// <summary>
        /// Called when there are received packets to be processed.
        /// </summary>
        event Action<IPacketSend, Queue<BasePacket>> OnPacketsReceived;

        /// <summary>
        /// The connection has been disconnected.
        /// The parameter indicates whether or not a reconnection attempt is in progress.
        /// </summary>
        event Action<IPacketSend, bool> OnDisconnect;

        /// <summary>
        /// Called whenever an exception occurrs in any part of the socket
        /// TODO: Make more specific events / handlers
        /// </summary>
        event Action<IPacketSend, Exception> OnThreadException;

        /// <summary>
        /// Is the packet sender connected to the remote host
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Queue the given packet for sending to the remote host.
        /// </summary>
        /// <param name="bp"></param>
        void Send(BasePacket bp);

        /// <summary>
        /// Attempt to connect to the remote host.
        /// </summary>
        void Connect();

        /// <summary>
        /// Disconnect from the remote host.
        /// </summary>
        void Disconnect();

    }

    public interface IPacketAnalyzer
    {
        void Send(BasePacket bp);
        void Receive(BasePacket bp);

        void Update();
        void Analyze();
        void Clear();
    }

    [Serializable]
    public class SocketWrapperSettings
    {
        /// <summary>
        /// Default size of the receive buffer
        /// </summary>
        public const int DEFAULT_BUFFER_SIZE = 256 * 1024;
        /// <summary>
        /// Default number of connection attempts before we stop
        /// </summary>
        public const int DEFAULT_MAX_RETRY_ATTEMPTS = 5;
        /// <summary>
        /// Value for RetryAttemptsLeft which indicates we should keep trying to connect forever.
        /// </summary>
        public const int INFINITE_RETRY_ATTEMPTS = -1;
        /// <summary>
        /// Milliseconds to wait between connection attempts.
        /// </summary>
        public const int DEFAULT_MILLIS_BETWEEN_RETRIES = 500;

        public SocketWrapperSettings()
        { }

        public SocketWrapperSettings(
            string ipAddress, 
            int port, 
            int bufferSize = DEFAULT_BUFFER_SIZE,
            int maxRetryAttempts = DEFAULT_MAX_RETRY_ATTEMPTS,
            long millisBetweenRetries = DEFAULT_MILLIS_BETWEEN_RETRIES)
        {
            this.ipAddress = ipAddress;
            this.port = port;
            this.bufferSize = bufferSize;
            this.maxRetryAttempts = maxRetryAttempts;
            this.millisBetweenRetries = millisBetweenRetries;
        }

        public string ipAddress;
        public int port;
        public int bufferSize = DEFAULT_BUFFER_SIZE;
        public int maxRetryAttempts = DEFAULT_MAX_RETRY_ATTEMPTS;
        public long millisBetweenRetries = DEFAULT_MILLIS_BETWEEN_RETRIES;
    }

    //------------------------------------------------------------------
    public class SocketWrapper : ThreadWrapper, IPacketSend
    {
        /// <summary>
        /// Multiplier used when growing the read buffer,
        /// new buffer size = growth factor * old buffer size
        /// </summary>
        private const float READ_BUFFER_GROWTH_FACTOR = 2f;

        /// <summary>
        /// Max size that we will scale the buffer up to
        /// </summary>
        private const int MAX_BUFFER_SIZE = 8 * 1024 * 1024; 

        public bool IsConnected
        {
            get
            {
                return hasCreatedSocket;
            }
        }

        public event Action<IPacketSend> OnConnect;
        public event Action<IPacketSend, Queue<BasePacket>> OnPacketsReceived;
        public event Action<IPacketSend, bool> OnDisconnect;
        public event Action<IPacketSend, Exception> OnThreadException;

        private SocketWrapperSettings settings;
        private Socket socketConnection = null;
        private volatile bool isCreatingSocket = false;
        private volatile bool hasCreatedSocket = false;
        private volatile bool isWaitingToListen = false;

        private object retryLock = new object();
        private int retryAttemptsLeft;
        private long lastConnectAttemptTimestamp = 0;
        
        private byte[] readBuffer;
        private int readBufferOffset = 0;
        int totalPacketsReceived = 0;
        
        private Queue<BasePacket> packetsReceived;
        private Queue<BasePacket> packetsToSend;

        private MemoryStream memoryStream;
        private BinaryWriter binaryWriter;

        static int incrementalId = 1024;
#if DEBUG_NETWORK_STREAM
        int numReceives = 0;
#endif
        IPacketAnalyzer analyzer;
        int id = incrementalId++;
        public int Id { get { return id; } set { id = value; } }
        

        //-------------------------------------------------------------------------------
        public SocketWrapper(SocketWrapperSettings settings)
        {
            Initialize(settings);
        }

        public SocketWrapper(
            string ipAddress, 
            int port, 
            int bufferSize = SocketWrapperSettings.DEFAULT_BUFFER_SIZE,
            int maxRetryAttempts = SocketWrapperSettings.DEFAULT_MAX_RETRY_ATTEMPTS,
            long millisBetweenRetries = SocketWrapperSettings.DEFAULT_MILLIS_BETWEEN_RETRIES)
        {
            Initialize(new SocketWrapperSettings(
                ipAddress,
                port,
                bufferSize,
                maxRetryAttempts,
                millisBetweenRetries));

            CreateSocket();
        }

        /// <summary>
        /// Creates a wrapper with an existing socket, typically from a ListenServer.
        /// Callers still need to call Connect() in order for the SocketWrapper to function, which
        /// should be done once handlers have been added for the various events, e.g. OnPacketsReceived.
        /// MaxRetryAttempts defaults to 0, as it doesn't make sense to try to reconnect with a socket
        /// created from the ListenServer.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="bufferSize"></param>
        /// <param name="maxRetryAttempts"></param>
        /// <param name="millisBetweenRetries"></param>
        public SocketWrapper(
            Socket socket,
            int bufferSize = SocketWrapperSettings.DEFAULT_BUFFER_SIZE,
            int maxRetryAttempts = 0,
            long millisBetweenRetries = SocketWrapperSettings.DEFAULT_MILLIS_BETWEEN_RETRIES)
        {
            if (!socket.Connected)
            {
                throw new ArgumentException("Socket must already be connected", "socket");
            }

            // Extract the ipAddress and port, for the purposes of reconnection
            IPEndPoint remoteIpEndPoint = socket.RemoteEndPoint as IPEndPoint;
            string ipAddress = remoteIpEndPoint.Address.ToString();
            int port = remoteIpEndPoint.Port;

            Initialize(new SocketWrapperSettings(
                ipAddress,
                port,
                bufferSize,
                maxRetryAttempts,
                millisBetweenRetries));

            socketConnection = socket;
            hasCreatedSocket = true;
            BeginReceive();
            ThreadTick();
        }

        //-------------------------------------------------------------------------------
        private void Initialize(SocketWrapperSettings settings)
        {
            this.settings = settings;

            packetsReceived = new Queue<BasePacket>();
            packetsToSend = new Queue<BasePacket>();

            retryAttemptsLeft = settings.maxRetryAttempts;

            readBuffer = new byte[settings.bufferSize];

            memoryStream = new MemoryStream();
            binaryWriter = new BinaryWriter(memoryStream);
        }

        public void Set(IPacketAnalyzer an)
        {
            analyzer = an;
        }

        //---------------------------------------------------------------

        public void Connect()
        {
            StartService();
        }

        public void Disconnect()
        {
            EndService();
        }

        protected override void ThreadTick()
        {
            try
            {
                if (hasCreatedSocket == true)
                {
                    Send();
                    lock (packetsReceived)
                    {
                        if (packetsReceived.Count > 0)
                        {
                            OnPacketsReceived?.Invoke(this, packetsReceived);
                            packetsReceived.Clear();
                        }
                    }
                    if (isWaitingToListen)
                    {
                        BeginReceive();
                        isWaitingToListen = false;
                    }
                    if(analyzer != null)
                    {
                        analyzer.Update();
                    }
                }
                else if (CanAttemptToConnectNow())
                {
                    CreateSocket();
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                OnThreadException?.Invoke(this, e);
            }
        }

        static void ThreadProc(Object stateInfo)
        {
            (stateInfo as SocketWrapper).ThreadTick();
        }

        //---------------------------------------------------------------

        private void CreateSocket()
        {
            if (isCreatingSocket || hasCreatedSocket)
                return;

            // Connect to a remote device.  
            IPAddress ip = Network.Utils.ResolveIPAddress(settings.ipAddress);
            IPEndPoint remoteEP = new IPEndPoint(ip, settings.port);

            isCreatingSocket = true;
            // Create a TCP/IP socket.  
            socketConnection = new Socket(ip.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            // Connect to the remote endpoint.  
            socketConnection.BeginConnect(remoteEP,
                new AsyncCallback(ConnectCallback), this);
        }

        private void ResetRetryAttemptCount()
        {
            lock (retryLock)
            {
                retryAttemptsLeft = settings.maxRetryAttempts;
                lastConnectAttemptTimestamp = DateTime.UtcNow.ToUnixMilliseconds();
            }
        }

        private void RecordFailedConnectAttempt()
        {
            lock (retryLock)
            {
                if (settings.maxRetryAttempts != SocketWrapperSettings.INFINITE_RETRY_ATTEMPTS)
                {
                    retryAttemptsLeft--;
                }
                lastConnectAttemptTimestamp = DateTime.UtcNow.ToUnixMilliseconds();
            }
        }

        private bool CanAttemptToConnectNow()
        {
            lock (retryLock)
            {
                return !hasCreatedSocket
                    && !isCreatingSocket
                    && HasReconnectAttemptsLeft()
                    && (DateTime.UtcNow.ToUnixMilliseconds() - lastConnectAttemptTimestamp)
                        > settings.millisBetweenRetries;
            }
        }

        private bool HasReconnectAttemptsLeft()
        {
            lock (retryLock)
            {
                return (settings.maxRetryAttempts == SocketWrapperSettings.INFINITE_RETRY_ATTEMPTS
                        || retryAttemptsLeft > 0);
            }
        }


        private void ConnectCallback(IAsyncResult ar)
        {
            // Have we already freed these up?
            if (socketConnection == null)
            {
                return;
            }

            try
            {
                socketConnection.EndConnect(ar);

                if (socketConnection.IsBound == false)
                {
                    throw new SocketException();
                }
                else
                {
                    Console.WriteLine("Connected to {0}:{1}", settings.ipAddress, settings.port);
                    hasCreatedSocket = true;
                    OnConnect?.Invoke(this);
                    BeginReceive();
                }
            }
            catch (SocketException e)
            {
                // We most likely failed to establish the socket connection
                Console.WriteLine(e.ToString());
                RecordFailedConnectAttempt();
                Console.WriteLine("Failed to connect to {0}:{1}, {2} retries remaining",
                        settings.ipAddress, settings.port, retryAttemptsLeft);
            }
            catch (Exception e)
            {
                // An unexpected exception occurred
                Console.WriteLine(e.ToString());
                RecordFailedConnectAttempt();
                OnThreadException?.Invoke(this, e);
            }
            finally
            {
                isCreatingSocket = false;
            }
        }

        public override void EndService()
        {
            base.EndService();
            // Flush any packets out
            Send();

            CloseAndShutdownSocket();
            OnDisconnect?.Invoke(this, false);
        }

        private void CloseAndShutdownSocket()
        {
            try
            {
                socketConnection.Shutdown(SocketShutdown.Both);
            }
            catch (Exception ex)
            {
                // We expect this to throw exceptions sometimes
                Console.WriteLine(ex.ToString());
            }
            try
            {
                socketConnection.Close();
            }
            catch (Exception ex)
            {
                // We expect this to throw exceptions sometimes
                Console.WriteLine(ex.ToString());
            }

            socketConnection = null;
            isWaitingToListen = false;
            readBufferOffset = 0;
            hasCreatedSocket = false;
        }

       /* bool IsSocketConnected()
        {
            bool part1 = socketConnection.Poll(1000, SelectMode.SelectRead);
            bool part2 = (socketConnection.Available == 0);
            if (part1 && part2)
                return false;
            else
                return true;
        }*/

        private void FailSocket()
        {
            // Socket has most likely been closed
            // make sure it has been tidied up
            CloseAndShutdownSocket();

            // Try again
            ResetRetryAttemptCount();
            bool willAttemptReconnect = HasReconnectAttemptsLeft();
            if (!willAttemptReconnect)
            {
                // We're done with this socket
                base.EndService();
            }
            hasCreatedSocket = false;
            OnDisconnect?.Invoke(this, willAttemptReconnect);
        }

        private void BeginReceive()
        {
            try
            {
                GrowReadBufferIfFull();
                socketConnection.BeginReceive(readBuffer, readBufferOffset, readBuffer.Length - readBufferOffset, 0,
                    new AsyncCallback(ReceiveCallback), this);
            }
            catch (SocketException)
            {
                FailSocket();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                OnThreadException?.Invoke(this, e);
            }
        }

        // Checks if the read buffer is full, and grows it if it is
        private void GrowReadBufferIfFull()
        {
            if (readBufferOffset == readBuffer.Length)
            {
                if (readBufferOffset == MAX_BUFFER_SIZE)
                {
                    // Oops - we've got a full buffer, but we're at our max - panic!
                    throw new Exception("Need to grow receive buffer, but already at max!");
                }

                // We've got a full read buffer - make it bigger
                int newBufferSize = Math.Min(MAX_BUFFER_SIZE, (int)Math.Round(readBuffer.Length * READ_BUFFER_GROWTH_FACTOR));
                byte[] newReadBuffer = new byte[newBufferSize];
                Buffer.BlockCopy(readBuffer, 0, newReadBuffer, 0, readBuffer.Length);
                readBuffer = newReadBuffer;
                Console.WriteLine("Resizing read buffer, now {0} bytes", readBuffer.Length);
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            // Have we already freed these up?
            if (socketConnection == null)
            {
                return;
            }

            int bytesRead = 0;
            try
            {
                bytesRead = socketConnection.EndReceive(ar);
                if (bytesRead > 0)
                {
#if DEBUG_NETWORK_STREAM
                    numReceives++;
                    Console.WriteLine("Received {2}x for {0} bytes, giving {1} bytes unparsed", bytesRead, readBufferOffset + bytesRead, numReceives);
#endif
                    IncommingBytesToPackets(readBuffer, readBufferOffset + bytesRead);
                    
                    // Try to grab more immediately
                    BeginReceive();
                }
                else
                {
                    // We didn't get anything this time round, so wait a little bit
                    isWaitingToListen = true;
                }
            }
            catch (SocketException)
            {
                FailSocket();
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                OnThreadException?.Invoke(this, e);
            }
            //ThreadPool.QueueUserWorkItem(ThreadProc, this);
        }

        private void IncommingBytesToPackets(byte[] bytes, int numBytes)
        {
            byte[] cheatArray = new byte[numBytes];// this is a terrible temp solution
            Buffer.BlockCopy(bytes, 0, cheatArray, 0, numBytes);
#if DEBUG_NETWORK_PACKETS
            Console.WriteLine("Received: " + BitConverter.ToString(cheatArray));
#endif
            int bytesParsed = 0;
            List<BasePacket> dataIn = IntrepidSerialize.Deserialize(cheatArray, numBytes, ref bytesParsed);
            lock (packetsReceived)
            {
                int num = packetsReceived.Count;
                dataIn.ForEach((bp) => { packetsReceived.Enqueue(bp); });
                totalPacketsReceived = packetsReceived.Count - num;
            }
            if (analyzer != null)
            {
                dataIn.ForEach((bp) => { analyzer.Send(bp); });
            }
#if DEBUG_NETWORK_PACKETS
            Console.WriteLine("totalCount of packets received {0}", totalPacketsReceived);
#endif

            if (bytesParsed < numBytes)
            {
                int numBytesUnParsed = numBytes - bytesParsed;
                Buffer.BlockCopy(cheatArray, bytesParsed, readBuffer, 0, numBytesUnParsed);
                readBufferOffset = numBytesUnParsed;
            }
            else
            {
                readBufferOffset = 0;
            }
        }

        public void Send(BasePacket bp)
        {
            lock (packetsToSend)
            {
                packetsToSend.Enqueue(bp);
            }
            if (analyzer != null)
            {
                analyzer.Send(bp);
            }
            //ThreadPool.QueueUserWorkItem(ThreadProc, this);
        }
        
        private void Send()
        {
            BasePacket[] packetList = PrepPacketsToSend();
            if (packetList == null)
                return;

            memoryStream.Seek(0, SeekOrigin.Begin);

            for (int i = 0; i < packetList.Length; i++)
            {
                BasePacket packet = packetList[i];

                ManageDebuggingPackets(packet);

                int pos = Network.Utils.SetupWrite(binaryWriter);
                packet.Write(binaryWriter);
                Network.Utils.FinishWrite(binaryWriter, pos);

                IntrepidSerialize.ReturnToPool(packet);
            }
            SendRecursive(packetList);
        }

        BasePacket[] PrepPacketsToSend()
        {
            BasePacket[] packetList;
            lock (packetsToSend)
            {
                if (packetsToSend.Count == 0)
                    return null;

                packetList = new BasePacket[packetsToSend.Count];// TODO: probably a problem here
                packetsToSend.CopyTo(packetList, 0);
                packetsToSend = new Queue<BasePacket>();
            }
            return packetList;
        }

        void ManageDebuggingPackets(BasePacket packet)
        {
#if DEBUG_NETWORK_PACKETS
            if (IntrepidSerialize.DebugLogPacket(packet))
            {
                Console.WriteLine("Attempting to send {0}", packet);
            }
        /*    if (packet is ServerPingHopperPacket)
            {
                ServerPingHopperPacket hopper = packet as ServerPingHopperPacket;
                string name = Assembly.GetCallingAssembly().GetName().Name;
                hopper.Stamp(name + " gateway send to client");
               // Send(packet);
           } */
#endif
        }

        private void SendRecursive(BasePacket[] packetList, int retry = 0)
        {
            try
            {
                socketConnection.Send(memoryStream.ToArray(), (int)memoryStream.Position, 0);
            }
            catch (SocketException)
            {
                /*#if DEBUG_NETWORK_PACKETS
                                Console.WriteLine("Failed to send packet {0}", packet);
#endif*/

            lock (packetsToSend)
                {
                    foreach (var bp in packetList)
                    {
                        packetsToSend.Enqueue(bp);
                    }
                }
                retry++;
                if (retry > settings.maxRetryAttempts)
                    FailSocket();
                else
                    SendRecursive(packetList, retry);
            }
            catch (Exception e)
            {
                /*
#if DEBUG_NETWORK_PACKETS
                Console.WriteLine("Failed to send packet {0} so skipping, root exception: {1}", packet, e.ToString());
#endif
                */
                OnThreadException?.Invoke(this, e);
            }
        }
    }
}
