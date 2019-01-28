using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Packets;
using CommonLibrary;

namespace Server
{
    public class ServerNetworking : ThreadWrapper, PlayerConnectedServer, IServerNetworking
    {
        public abstract class PacketHandlerStrategy// interface
        {
            protected ServerNetworking server;

            public PacketHandlerStrategy(ServerNetworking _server) { server = _server; }
            public abstract void HandlePackets(Queue<BasePacket> listOfPackets);
        }
        public class UnboundToGatewayPacketHandler : PacketHandlerStrategy
        {
            public UnboundToGatewayPacketHandler(ServerNetworking _server) : base(_server) { }
            public override void HandlePackets(Queue<BasePacket> listOfPackets)
            {
                server.numPacketsReceived += listOfPackets.Count;
                foreach (var packet in listOfPackets)
                {
                    if (packet is ServerIdPacket)
                    {
                        ServerIdPacket id = packet as ServerIdPacket;
                        if (id != null && id.Type == ServerIdPacket.ServerType.Gateway)
                        {
                            server.hasConnectedToGateway = true;
                            server.CurrentPacketHandling = server.BoundPacketHandler;
                            break;
                        }
                    }
                }
            }
        }

        public class BoundToGatewayPacketHandler : PacketHandlerStrategy
        {
            public BoundToGatewayPacketHandler(ServerNetworking _server) : base(_server) { }
            public override void HandlePackets(Queue<BasePacket> listOfPackets)
            {
                server.numPacketsReceived += listOfPackets.Count;
                foreach (var packet in listOfPackets)
                {
                    // normal processing
                    ServerConnectionHeader sch = packet as ServerConnectionHeader;// should be converted to a wrapper
                    if (sch != null)
                    {
                        server.NextConnectionId = sch.connectionId;
                        continue;
                    }
                    PlayerSaveStatePacket pss = packet as PlayerSaveStatePacket;
                    if (pss != null)
                    {
                        server.LogInPlayer(server.NextConnectionId, pss.state);

                        // send all entities in area to player eventually.
                        // notify all other players that this player is here.
                        continue;
                    }

                    //--------------------------------------------
                    KeepAlive ka = packet as KeepAlive;
                    if (ka != null)
                    {
                        KeepAliveResponse kar = (KeepAliveResponse)IntrepidSerialize.TakeFromPool(PacketType.KeepAliveResponse);
                        //gatewaySocket.Send(kar);
                        server.AddOutgoingPacket(kar, ServerNetworking.InvalidConnectionId);
                        continue;
                    }
                    
                    if (server.connectedClients.ContainsKey(server.NextConnectionId))
                    {
                        server.connectedClients[server.NextConnectionId].AddIncomingPacket(packet);
                    }
                }
            }
        }

        const int InvalidConnectionId = -1;
        
        int applicationId;

        Dictionary<int, ConnectedClient> connectedClients;

        object newConnectedLock = new object();
        List<ConnectedClient> newlyConnectedClients;
        Object newDisconnectLock = new object();
        List<ConnectedClient> newlyDisconnectedClients;

        int frameId = 1000;
        long snapShotFrameTime = 0;
        Stopwatch frameTickTimer;
        long snapShotTime = 0;
        Stopwatch startTime;
        long frameClampTime = 16;
        
        ProfileServerProxy profileServer;

        int nextEntityId = 1024;

        SocketWrapperSettings gatewayServerSettings;
        SocketWrapper gatewaySocket; // TODO: multiple connections.
        
        PacketQueues containers = new PacketQueues();
        /// <summary>
        /// Lock object for the list of packets inside the PacketContainers object.
        /// </summary>
        Object containersLock = new Object();
        
        int numPacketsReceived = 0;
        public string defaultSaveState;
        bool hasConnectedToGateway = false;

        PacketDispatcher packetDispatcher;

        PacketHandlerStrategy CurrentPacketHandling { get; set; }
        UnboundToGatewayPacketHandler UnboundPacketHandler { get; set; }
        BoundToGatewayPacketHandler BoundPacketHandler { get; set; }

        int NextConnectionId { get; set; }

        public int Frame { get { return frameId; } set { frameId = value; } }

        public int FrameID
        {
            get
            {
                return Frame;
            }
        }

        public event Action OnTick;
        public event Action<int, PlayerSaveState> OnClientConnected;
        public event Action<int> OnClientDisconnected;

        #region Construction
        public ServerNetworking(SocketWrapperSettings profileServerSettings, SocketWrapperSettings _gatewayServerSettings, int appId)
        {
            this.OnExceptionFromThread += GameServer_OnExceptionFromThread;

            SetMaxFPS(NetworkConstants.GameServerFPS);

            connectedClients = new Dictionary<int, ConnectedClient>();
            newlyConnectedClients = new List<ConnectedClient>();
            newlyDisconnectedClients = new List<ConnectedClient>();

            packetDispatcher = new PacketDispatcher();

            profileServer = new ProfileServerProxy(profileServerSettings);
            profileServer.OnReceivePacket += ProfileServer_OnReceivePacket;
            profileServer.Connect();

            applicationId = appId;

           // _gatewayServerSettings.ipAddress = "localhost";
            gatewayServerSettings = _gatewayServerSettings;
            ConnectToGateway();

            startTime = Stopwatch.StartNew();
            frameTickTimer = Stopwatch.StartNew();

            UnboundPacketHandler = new UnboundToGatewayPacketHandler(this);
            BoundPacketHandler = new BoundToGatewayPacketHandler(this);
            CurrentPacketHandling = UnboundPacketHandler;
        }

        private void GameServer_OnExceptionFromThread(Exception obj)
        {
            Console.WriteLine(obj);
        }

        public void Close()
        {
            if (gatewaySocket != null && gatewaySocket.IsConnected == true)
            {
                gatewaySocket.Disconnect();
                gatewaySocket = null;
                hasConnectedToGateway = false;
            }
        }
#endregion Construction

#region BoilerplateConnections

        void ConnectToGateway()
        {
            if (gatewaySocket == null)
            {
                gatewaySocket = new SocketWrapper(gatewayServerSettings);
                gatewaySocket.OnPacketsReceived += Sock_OnPacketsReceived;
                gatewaySocket.OnConnect += Sock_OnConnect;
                gatewaySocket.OnDisconnect += Sock_OnDisconnect;
                gatewaySocket.Connect();
            }
        }
        private void Sock_OnConnect(IPacketSend sender)
        {
            if (gatewaySocket != sender)
                return;

            ServerIdPacket serverId = (ServerIdPacket)IntrepidSerialize.TakeFromPool(PacketType.ServerIdPacket);
            serverId.Type = ServerIdPacket.ServerType.Game;
            serverId.Id = applicationId;
            serverId.MapId = 1;
            sender.Send(serverId);
        }
        public void Sock_OnDisconnect(IPacketSend sender, bool willRetry)
        {
            gatewaySocket.OnConnect -= Sock_OnConnect;
            gatewaySocket.OnDisconnect -= Sock_OnDisconnect;
            gatewaySocket.OnConnect -= Sock_OnConnect;
            gatewaySocket = null;
            hasConnectedToGateway = false;
        }

#endregion BoilerplateConnections

        public void AddOutgoingPacket(BasePacket packet, int socketId)
        {
            lock (containersLock)
            {
                containers.outgoingPackets.Enqueue(new SocketPacketPair(socketId, packet));
            }
        }

        public void AddIncomingPacket(BasePacket packet, int socketId)
        {
            lock (containersLock)
            {
                containers.incomingPackets.Enqueue(new SocketPacketPair(socketId, packet));
            }
        }

        

        protected override void ThreadTick()
        {
            ProcessNewlyConnectedClients();
            ProcessNewlyDisconnectedClients();

            // Grab all incoming packets and push them to the players
            ProcessAllIncomingPackets();

            OnTick?.Invoke();

            if (gatewaySocket != null && hasConnectedToGateway == true)
            {
                FlushAllOutgoingPackets();
            }
            else
            {
                ClearAllOutgoingPackets();
                ConnectToGateway();
            }

            profileServer.Tick();

            frameId++;
            var milliSec = startTime.ElapsedMilliseconds;
            if (milliSec - snapShotTime > 1000)
            {
                snapShotTime = milliSec;
#if DEBUG_FRAMES
                Console.Write("frame# {0}, ms elapsed {1}, ms per frame {2}\n", frameId, milliSec, (float)milliSec / (float)frameId);
#endif
            }
            SendFrameTickToAllConnectedClients();
        }

#region PlayerConnectionMgmt

        void ProcessNewlyConnectedClients()
        {
            ConnectedClient[] newClientList;
            lock (newConnectedLock)
            {
                newClientList = newlyConnectedClients.ToArray();
                newlyConnectedClients.Clear();
            }
            foreach (var newClient in newClientList)
            {
                //Ignore players that are already connected
                if (connectedClients.ContainsKey(newClient.SocketId))
                {
                    continue;
                }

                connectedClients.Add(newClient.SocketId, newClient);
                OnClientConnected?.Invoke(newClient.EntityId, newClient.saveState);
            }
        }

        public void DisconnectPlayer(int socketId)
        {
            Console.WriteLine("Disconnecting, socketID: " + socketId);
            lock (newConnectedLock)
            {
                foreach (var player in newlyConnectedClients)
                {
                    if (player.SocketId == socketId)
                    {
                        newlyConnectedClients.Remove(player);
                        return;
                    }
                }
            }
            if (connectedClients.ContainsKey(socketId))
            {
                ConnectedClient client = connectedClients[socketId];
                lock (newDisconnectLock)
                {
                    newlyDisconnectedClients.Add(client);
                }
            }
        }
        void ProcessNewlyDisconnectedClients()
        {
            ConnectedClient[] disconnectedCopy = null;
            lock (newDisconnectLock)
            {
                disconnectedCopy = newlyDisconnectedClients.ToArray();
                newlyDisconnectedClients.Clear();
            }
            foreach (var disconnectedClient in disconnectedCopy)
            {
                connectedClients.Remove(disconnectedClient.SocketId);
                OnClientDisconnected?.Invoke(disconnectedClient.EntityId);
            }
        }

#endregion PlayerConnectionMgmt

#region RegularUpdates

        void SendFrameTickToAllConnectedClients()
        {
            long elapsed = frameTickTimer.ElapsedMilliseconds;
            if (elapsed < frameClampTime)
                return;

            int diffTicks = (int) ((elapsed - snapShotFrameTime) / frameClampTime);
            snapShotFrameTime = elapsed;
            frameId += diffTicks - 1;
            ServerTick packet = (ServerTick) IntrepidSerialize.TakeFromPool(PacketType.ServerTick);
            packet.TickCount = frameId;
            packet.NumTicksSinceLastSend = (short)diffTicks;
            Send(packet);
        }



        void ProcessAllIncomingPackets()
        {
            // Flush all packets from here to their relevant players
            ForwardAllIncomingPackets();
            // Process all player packets
            foreach (var client in connectedClients)
            {
                client.Value.ProcessIncomingData();
            }
        }


        void ForwardAllIncomingPackets()
        {
            Dictionary<int, ConnectedClient> currentClientList;
            currentClientList = new Dictionary<int, ConnectedClient>(connectedClients);

            lock (containersLock)
            {
                foreach (var packetPair in containers.incomingPackets)
                {
                    if (currentClientList.ContainsKey(packetPair.connectionId))
                    {
                        currentClientList[packetPair.connectionId].AddIncomingPacket(packetPair.packet);
                    }
                }
                containers.incomingPackets.Clear();
            }
        }

        void FlushAllOutgoingPackets()
        {
            // Flush outgoing packets from player to game
            foreach (var client in connectedClients)
            {
                client.Value.ProcessOutgoingData();
            }
            //Flush outgoing packets from game to gateway
            ForwardAllOutgoingPackets();
        }

        void ForwardAllOutgoingPackets()
        {
            lock (containersLock)
            {
                foreach (var packetPair in containers.outgoingPackets)
                {
                    if (packetPair.connectionId != InvalidConnectionId)
                    {
                        ServerConnectionHeader gatewayHeader = (ServerConnectionHeader)IntrepidSerialize.TakeFromPool(PacketType.ServerConnectionHeader);
                        gatewayHeader.connectionId = packetPair.connectionId;
                        gatewaySocket.Send(gatewayHeader);
                    }
                    gatewaySocket.Send(packetPair.packet);
                    
                }
                containers.outgoingPackets.Clear();
            }
        }

        #endregion 

        /// <summary>
        /// Sends a packet to all clients.
        /// Packet will be returned to pool once complete.
        /// </summary>
        public void Send(BasePacket packet)
        {
            Send(packet, connectedClients.Values);
        }

        /// <summary>
        /// Sends a packet to all given players
        /// Packet will be returned to pool once complete.
        /// </summary>
        public void Send(BasePacket packet, IEnumerable<int> entityIDs)
        {
            var clients = connectedClients.Where(e => entityIDs.Contains(e.Value.EntityId)).Select(e => e.Value);
            Send(packet, clients);
        }

        private void Send(BasePacket packet, IEnumerable<ConnectedClient> clients)
        {
            List<SocketPacketPair> listOfPacketsToSend = new List<SocketPacketPair>();

            foreach (var client in clients)// this should be improved. I try to save the results and then do them all at once. Prevents locks.
            {
                BasePacket bp = IntrepidSerialize.ReplicatePacket(packet);
                listOfPacketsToSend.Add(new SocketPacketPair(client.SocketId, bp));
            }
            lock (containersLock)
            {
                foreach (var item in listOfPacketsToSend)
                {
                    containers.outgoingPackets.Enqueue(item);
                }
            }
            IntrepidSerialize.ReturnToPool(packet);
        }

        void ClearAllOutgoingPackets()
        {
            lock (containersLock)
            {
                containers.outgoingPackets.Clear();
            }
        }

        private void Sock_OnPacketsReceived(IPacketSend arg1, Queue<BasePacket> listOfPackets)
        {
            CurrentPacketHandling.HandlePackets(listOfPackets);
        }

        public void SendApplicationQuitMessage()
        {
            ServerDisconnectPacket sdp = (ServerDisconnectPacket)IntrepidSerialize.TakeFromPool(PacketType.ServerDisconnect);
            sdp.Type = ServerIdPacket.ServerType.Game;
            sdp.Id = applicationId;
            sdp.MapId = 1;
            // send immediately
            gatewaySocket.Send(sdp);
        }

        private void ProfileServer_OnReceivePacket(BasePacket packet)
        {
            ProfileCreateCharacterResponse response = packet as ProfileCreateCharacterResponse;
            if (response != null)
            {
                // Find the player by accountId
                // TODO: Have players indexed by accountId somewhere?
                foreach (var client in connectedClients)
                {
                    if (client.Value.AccountId == response.accountId)
                    {
                        client.Value.AddIncomingPacket(response);
                        break;
                    }
                }
            }
        }
        public void SendToProfileServer(BasePacket packet)
        {
            profileServer.Send(packet);
        }
        public int GetNextEntityID()
        {
            return nextEntityId++;
        }

        public void LogInPlayer(int socketId, PlayerSaveState saveState)// deserialized
        {
            ConnectedClient connectedClient = new ConnectedClient(GetNextEntityID(), socketId, this, saveState);
            lock (newConnectedLock)
            {
                if (newlyConnectedClients.Any(e => e.SocketId == socketId))
                    return;
                newlyConnectedClients.Add(connectedClient);
            }
        }

        public void SignalListener(int entityId, BasePacket packet)
        {
            packetDispatcher.SignalListener(entityId, packet);
        }

        public void SignalListener(BasePacket packet)
        {
            packetDispatcher.SignalListener(packet);
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

        public void StopService()
        {
            SendApplicationQuitMessage();
            EndService();
            // This flushes before closing the socket
            gatewaySocket.Disconnect();
        }
    }
}