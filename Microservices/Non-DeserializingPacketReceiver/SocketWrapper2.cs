using Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

namespace Non_DeserializingPacketReceiver
{

    public interface IPacketSend
    {
        event Action<IPacketSend> OnConnect;
        event Action<IPacketSend, Queue<BasePacket>> OnPacketsReceived;
        event Action<IPacketSend, bool> OnDisconnect;

        /// TODO: Make more specific events / handlers
        event Action<IPacketSend, Exception> OnThreadException;

        bool IsConnected { get; }
        void Send(BasePacket bp);
        void Connect();
        void Disconnect();
    }

    [Serializable]
    public class SocketWrapperSettings
    {
        public const int DEFAULT_RECEIVE_BUFFER_SIZE = 50 * 1024;
        public const int DEFAULT_MAX_RETRY_CONNECT_ATTEMPTS = 5;
        /// Value for RetryAttemptsLeft which indicates we should keep trying to connect forever.
        public const int INFINITE_RETRY_ATTEMPTS = -1;
        public const int DEFAULT_MILLIS_BETWEEN_CONNECT_RETRIES = 500;

        public SocketWrapperSettings()
        { }

        public SocketWrapperSettings(
            string ipAddress,
            int port,
            int bufferSize = DEFAULT_RECEIVE_BUFFER_SIZE,
            int maxRetryAttempts = DEFAULT_MAX_RETRY_CONNECT_ATTEMPTS,
            long millisBetweenRetries = DEFAULT_MILLIS_BETWEEN_CONNECT_RETRIES)
        {
            this.ipAddress = ipAddress;
            this.port = port;
            this.bufferSize = bufferSize;
            this.maxRetryAttempts = maxRetryAttempts;
            this.millisBetweenRetries = millisBetweenRetries;
        }

        public string ipAddress;
        public int port;
        public int bufferSize = DEFAULT_RECEIVE_BUFFER_SIZE;
        public int maxRetryAttempts = DEFAULT_MAX_RETRY_CONNECT_ATTEMPTS;
        public long millisBetweenRetries = DEFAULT_MILLIS_BETWEEN_CONNECT_RETRIES;
    }

    //------------------------------------------------------------------
    public class SocketWrapper2 : CommonLibrary.ThreadWrapper, IPacketSend
    {
        private const float READ_BUFFER_GROWTH_FACTOR = 2f;
        private const int MAX_BUFFER_RESIZE_SIZE = 256 * 1024;

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

        private Queue<BasePacket> packetsReceived;
        private Queue<BasePacket> packetsToSend;

        private MemoryStream memoryStream;
        private BinaryWriter binaryWriter;

        static int incrementalId = 1024;
        int id = incrementalId++;
        public int Id { get { return id; } set { id = value; } }


        //-------------------------------------------------------------------------------
        public SocketWrapper2(SocketWrapperSettings settings)
        {
            Initialize(settings);
        }

        public SocketWrapper2(
            string ipAddress,
            int port,
            int bufferSize = SocketWrapperSettings.DEFAULT_RECEIVE_BUFFER_SIZE,
            int maxRetryAttempts = SocketWrapperSettings.DEFAULT_MAX_RETRY_CONNECT_ATTEMPTS,
            long millisBetweenRetries = SocketWrapperSettings.DEFAULT_MILLIS_BETWEEN_CONNECT_RETRIES)
        {
            Initialize(new SocketWrapperSettings(
                ipAddress,
                port,
                bufferSize,
                maxRetryAttempts,
                millisBetweenRetries));
        }

        // remember to call Connect after creation
        public SocketWrapper2(
            Socket socket,
            int bufferSize = SocketWrapperSettings.DEFAULT_RECEIVE_BUFFER_SIZE,
            int maxRetryAttempts = 0,
            long millisBetweenRetries = SocketWrapperSettings.DEFAULT_MILLIS_BETWEEN_CONNECT_RETRIES)
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
            ThreadUpdate();
        }
        protected void ThreadUpdate()
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
            (stateInfo as SocketWrapper2).ThreadUpdate();
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
                if (readBufferOffset == MAX_BUFFER_RESIZE_SIZE)
                {
                    // Oops - we've got a full buffer, but we're at our max - panic!
                    throw new Exception("Need to grow receive buffer, but already at max!");
                }

                // We've got a full read buffer - make it bigger
                int newBufferSize = Math.Min(MAX_BUFFER_RESIZE_SIZE, (int)Math.Round(readBuffer.Length * READ_BUFFER_GROWTH_FACTOR));
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
                    Console.WriteLine("Received {0} bytes, giving {1} bytes unparsed", bytesRead, readBufferOffset + bytesRead);
#endif
                    ConvertBytesToPackets(readBuffer, readBufferOffset + bytesRead);

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

        private void ConvertBytesToPackets(byte[] bytes, int numBytes)
        {
            byte[] cheatArray = new byte[numBytes];// this is a terrible temp solution
            Buffer.BlockCopy(bytes, 0, cheatArray, 0, numBytes);
            //Console.WriteLine("Received: " + BitConverter.ToString(cheatArray));
            int bytesParsed = 0;
            List<BasePacket> dataIn = IntrepidSerialize.Deserialize(cheatArray, numBytes, ref bytesParsed);
            lock (packetsReceived)
            {
                dataIn.ForEach((bp) => { packetsReceived.Enqueue(bp); });
            }

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

    public class StateObject
    {
        public Socket workSocket = null;
        public const int BufferSize = 1024;
        public byte[] buffer = new byte[BufferSize];
        public StringBuilder stringBuilder = new StringBuilder();
        public bool hasFailed = false;

        public StateObject(Socket socket)
        {
            workSocket = socket;
            ThreadPool.QueueUserWorkItem(SetupReceive, this);
        }
        public static void Receive(IAsyncResult ar)
        {
            
          /*  StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;
            // Create the state object.  
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(Listener.readCallback), state);*/
        }

        static void SetupReceive(object obj)
        {
            StateObject so = obj as StateObject;
            so.workSocket.BeginReceive(so.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(readCallback), so);
        }
        public static void readCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket.  
            int read = handler.EndReceive(ar);

            // Data was read from the client socket.  
            if (read > 0)
            {
                state.stringBuilder.Append(Encoding.ASCII.GetString(state.buffer, 0, read));
                SetupReceive(state);
            }
            else
            {
                if (state.stringBuilder.Length > 1)
                {
                    // All the data has been read from the client;  
                    // display it on the console.  
                    string content = state.stringBuilder.ToString();
                    Console.WriteLine("Read {0} bytes from socket.\n Data : {1}",
                       content.Length, content);
                }
                state.hasFailed = true;
                handler.Close();
            }
        }
    }

    class Listener
    {
        static ManualResetEvent allDone = new ManualResetEvent(false);
        public bool hasFailed = false;
        public void StartListening()
        {
            IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
            IPEndPoint localEP = new IPEndPoint(ipHostInfo.AddressList[0], 11000);

            Console.WriteLine("Local address and port : {0}", localEP.ToString());

            Socket listener = new Socket(localEP.Address.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEP);
                listener.Listen(10);

                while (true)
                {
                    allDone.Reset();

                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(
                        new AsyncCallback(Listener.acceptCallback),
                        listener);

                    allDone.WaitOne();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("Closing the listener...");
        }
        public static void acceptCallback(IAsyncResult ar)
        {
            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Signal the main thread to continue.  
            allDone.Set();

            // Create the state object.  
            StateObject state = new StateObject(handler);
           /* state.workSocket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(Listener.readCallback), state);*/
        }
        public static void readCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket.  
            int read = handler.EndReceive(ar);

            // Data was read from the client socket.  
            if (read > 0)
            {
                state.stringBuilder.Append(Encoding.ASCII.GetString(state.buffer, 0, read));
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(readCallback), state);
            }
            else
            {
                if (state.stringBuilder.Length > 1)
                {
                    // All the data has been read from the client;  
                    // display it on the console.  
                    string content = state.stringBuilder.ToString();
                    Console.WriteLine("Read {0} bytes from socket.\n Data : {1}",
                       content.Length, content);
                }
                state.hasFailed = true;
                handler.Close();
            }
        }
    }
}
