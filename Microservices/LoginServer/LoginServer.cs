using PacketTypes;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Packets;

namespace LoginServer
{
    class LoginSocket
    {
        public static short port = 11002;
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        public static LoginServer loginServer;

        public void StartListening()
        {
            IPAddress ipAddress = IPAddress.Any;
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

            // Create a TCP/IP socket.  
            Socket listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                while (true)
                {
                    // Set the event to nonsignaled state.  
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.  
                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);

                    // Wait until a connection is made before continuing.  
                    allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();

        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();

            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            ConnectionState state = new ConnectionState(handler);

            loginServer.AddConnection(state);
        }
    }

    partial class LoginServer : CommonLibrary.ThreadWrapper
    {
        class RequestSetup
        {
            public int id;
            public int connectionId;
            public BasePacket request;
        }
        public DBAccountQuerier accountQuerier;
        RequestSetup requestInProcess;

        // Lock for the connections dictionary
        Object connectionsLock = new object();
        // Connection id -> ConnectionState
        Dictionary<int, ConnectionState> connections;// should only be one, but may be temporary

        Queue<RequestSetup> userAccountRequests = new Queue<RequestSetup>();
        int requestId = 1024;
        int connectionId = 1;

        public LoginServer() : base()
        {
            accountQuerier = new DBAccountQuerier();
            connections = new Dictionary<int, ConnectionState>();
            configuredSleep = NetworkConstants.LoginFPS;
            OnExceptionFromThread += LoginServer_OnExceptionFromThread;
        }

        private void LoginServer_OnExceptionFromThread(Exception obj)
        {
            Console.Error.WriteLine(obj);
        }

        public void AddRequest(int connectionId, BasePacket uar)
        {
            RequestSetup rs = new RequestSetup();
            rs.id = requestId++;
            rs.request = uar;
            rs.connectionId = connectionId;

            userAccountRequests.Enqueue(rs);
        }
        
        public void AddConnection(ConnectionState state)
        {
            lock (connectionsLock)
            {
                state.connectionId = ++connectionId;
                connections.Add(connectionId, state);
            }
        }
        public void RemoveConnection(ConnectionState state)
        {
            lock (connectionsLock)
            {
                if(connections.ContainsKey(state.connectionId))
                {
                    connections.Remove(state.connectionId);
                }
            }
        }

        protected override void ThreadTick()
        {
            if (accountQuerier != null)
            {
                FindNewDbQuery();
                SubmitNewQuery();
            }
        }

        void FindNewDbQuery()
        {
            if (requestInProcess == null)
            {
                lock (connectionsLock)
                {
                    foreach (var v in connections)// TODO: if there are multiple connections, then we will need to classify each request to match it to the the connection from which it came
                    {
                        ConnectionState connection = v.Value;
                        if (connection.HasNewData() == true)
                        {
                            List<BasePacket> packets = connection.RetrieveData();
                            foreach(var packet in packets)
                            {
                                AddRequest(connection.connectionId, packet);
                            }
                            break;
                        }
                    }
                }

                if (userAccountRequests.Count > 0)
                {
                    requestInProcess = userAccountRequests.Dequeue();
                }
            }            
        }

        void SubmitNewQuery()
        {
            if (requestInProcess != null)
            {
                if (connections.ContainsKey(requestInProcess.connectionId))
                {
                    switch (requestInProcess.request.PacketType)
                    {
                        case PacketType.UserAccountRequest:
                            ProcessUserAccountRequest(requestInProcess);
                            break;
                        case PacketType.ProfileCreateCharacterRequest:
                            ProcessProfileCreateCharacterRequest(requestInProcess);
                            break;
                        case PacketType.ProfileUpdateCharacter:
                            ProcessProfileUpdateCharacter(requestInProcess);
                            break;
                        default:
                            throw new Exception(string.Format("Unsupported packet type: {0}", requestInProcess.request.PacketType));
                    }
                }
                
                requestInProcess = null;
            }
        }

        private void ProcessUserAccountRequest(RequestSetup requestInProcess)
        {
            UserAccountRequest request = requestInProcess.request as UserAccountRequest;
            PlayerSaveState saveState = accountQuerier.GetPlayerSaveState(
                            request.username.MakeString(),
                            request.password.MakeString(),
                            request.product_name.MakeString());

            UserAccountResponse response = (UserAccountResponse)IntrepidSerialize.TakeFromPool(PacketType.UserAccountRequest);
            response.connectionId = request.connectionId;
            response.isValidAccount = saveState != null;
            if (saveState != null)
            {
                response.state = saveState;
            }

            ConnectionState cs = connections[requestInProcess.connectionId];
            cs.Send(response);
        }

        private void ProcessProfileCreateCharacterRequest(RequestSetup requestInProcess)
        {
            ProfileCreateCharacterRequest request = requestInProcess.request as ProfileCreateCharacterRequest;
            int characterId = accountQuerier.CreateCharacterProfile(
                                            request.accountId,
                                            request.productName.MakeString(),
                                            request.characterName.MakeString(),
                                            request.state);
            if (characterId == -1)
            {
                // We could return a result here - however the game server could request a load, and when that fails it'll know something is up
                Console.Error.WriteLine("Failed to create new character: accountId: {0}, productName: {1}, characterName: {2}, state: {3}",
                    request.accountId, request.productName, request.characterName, request.state);
            }

            ProfileCreateCharacterResponse response = (ProfileCreateCharacterResponse)IntrepidSerialize.TakeFromPool(PacketType.ProfileCreateCharacterResponse);
            response.accountId = request.accountId;
            response.characterId = characterId;

            ConnectionState cs = connections[requestInProcess.connectionId];
            cs.Send(response);
        }

        private void ProcessProfileUpdateCharacter(RequestSetup requestInProcess)
        {
            ProfileUpdateCharacter request = requestInProcess.request as ProfileUpdateCharacter;
            bool result = accountQuerier.UpdateCharacterProfile(
                                            request.characterId,
                                            request.state);
            if (!result)
            {
                // If this fails there is little the game can do about it, so possibly not worth telling it
                Console.Error.WriteLine("Failed to update character: characterId: {0}, state: {1}",
                    request.characterId, request.state);
            }
        }
    }
}
