using Network;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace CommonLibrary
{
    public class ListenServer
    {
        public ushort Port;
        public string Address;
        public string ServerName;

        private Socket listenSocket;
        private Thread listenThread;

        private bool isRunning;

        // Thread signal.  
        private ManualResetEvent allDone = new ManualResetEvent(false);

        //Events
        public event Action<Socket> OnNewConnection;

        public ListenServer(ushort port, string address, string name)
        {
            this.Port = port;
            this.Address = address;
            this.ServerName = name;

            listenThread = new Thread(new ThreadStart(ListenLoop));
        }

        public void StartListening()
        {
            if (isRunning == true)
            {
                return;
            }
            isRunning = true;
            listenThread.Start();
        }


        public void StopListening()
        {
            if (isRunning == false)
            {
                return;
            }
            isRunning = false;
            allDone.Set();
            listenThread.Abort();
            listenSocket.Close();
        }

        private void ListenLoop()
        {
            try
            {
                IPAddress ipAddress = Network.Utils.ResolveIPAddress(Address);
           
                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, Port);

                // Create a TCP/IP socket.  
                listenSocket = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                listenSocket.Bind(localEndPoint);
                listenSocket.Listen(100);
                Console.WriteLine("Listening for connections on " + localEndPoint.ToString());


                while (isRunning)
                {
                    // Set the event to nonsignaled state.  
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.  
                    Console.WriteLine("Waiting for a connection...");
                    listenSocket.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listenSocket);

                    // Wait until a connection is made before continuing.  
                    allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();

            if (ar == null || ar.AsyncState == null)
            {
                return;
            }

            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler;

            try
            {
                handler = listener.EndAccept(ar);
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("socket accept failed");
                return;
            }
            catch(SocketException except)
            {
                Console.WriteLine("socket accept exception: {0}", except.ErrorCode);
                return;
            }            

            if (OnNewConnection != null)
            {
                OnNewConnection(handler);
            }
            else
            {
                handler.Disconnect(false);
                throw new Exception("Listen server received a new connection, but no one wants to handle it");
            }
        }
    }
}