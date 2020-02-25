using CommonLibrary;
using Packets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Vectors;

namespace Test_Direct_ServerToClient
{
    public class PlayerState
    {
        public int connectionId;
        public int characterId;
        public int accountId;
        public int entityId;
        public Vector3 position = new Vector3();
        public Vector3 rotation = new Vector3();
    }

    public class ServerController : ThreadWrapper
    {
            object connectedLock = new object();
            bool extremeLogging = false;

            List<ServerConnectionState> newServersAwaitingConfirmation;
            List<PlayerConnectionState> newPlayersAwaitingConfirmation;

            //ServerRegistry servers;
            List<UserSocket> players;
            List<UserSocket> inactivePlayers;

            int nextGateWayPlayerConnectionId = 1024;

            UserSocket currentPlayerForHeader = null;
            int currentConnectionIdForHeader = 0;

            private PacketQueues containers = new PacketQueues();

            /// <summary>
            /// Lock object for the list of packets inside the PacketContainers object.
            /// </summary>
            Object containersLock = new Object();

            ListenServer playerConnectionListener;
            ListenServer serverListener;
            //LoginServerProxy loginServerProxy;

            Stopwatch screenRefreshTimer;
            DateTime launchDate = DateTime.Now;
            long numPlayerPackets = 0, numPlayerPacketsLastTimeStamp = 0;
            long numServerPackets = 0, numServerPacketsLastTimeStamp = 0;

            public ServerController() : base()
            {
                //servers = new ServerRegistry();
                players = new List<UserSocket>();
                inactivePlayers = new List<UserSocket>();

                //loginServerProxy = loginServer;
                //loginServerProxy.OnNewPlayerLoggedIn += NewPlayerLoginResult;

                playerConnectionListener = new ListenServer(NetworkConstants.defaultGatewayToClientPort, "0.0.0.0", "client-side");
                playerConnectionListener.OnNewConnection += OnNewPlayerConnection;

                serverListener = new ListenServer(NetworkConstants.defaultGatewayToServerPort, "0.0.0.0", "server-side");
                serverListener.OnNewConnection += OnNewServerConnection;

                Console.WriteLine("-------------------------------------------------------");
                newServersAwaitingConfirmation = new List<ServerConnectionState>();
                newPlayersAwaitingConfirmation = new List<PlayerConnectionState>();

                serverListener.StartListening();
                playerConnectionListener.StartListening();
                screenRefreshTimer = new Stopwatch();
                screenRefreshTimer.Start();
            }

            public override void Cleanup()
            {
                EndService();
                playerConnectionListener.StopListening();
                serverListener.StopListening();
            }


            //------------------------------------------------------------------------------

            #region KeepAlive
            void ManageServerKeepAlive()
            {
                if (extremeLogging == true)
                    Console.WriteLine("ManageServerKeepAlive");

                List<int> gamesToDisconnectPlayers = null;
                if (gamesToDisconnectPlayers != null)
                {
                    List<UserSocket> tempPlayers;
                    lock (connectedLock)
                    {
                        tempPlayers = new List<UserSocket>(players);
                    }
                    foreach (var gameId in gamesToDisconnectPlayers)
                    {
                        foreach (var player in tempPlayers)
                        {
                            if (player.connection.MarkedAsSocketClosed == false &&
                                player.gameId == gameId)
                            {
                                player.connection.Disconnect();
                            }
                        }
                    }
                }
            }

            void ManageClientKeepAlive()
            {
                if (extremeLogging == true)
                    Console.WriteLine("ManageClientKeepAlive");

                List<UserSocket> tempPlayers;
                lock (connectedLock)
                {
                    tempPlayers = new List<UserSocket>(players);
                }
                foreach (var player in tempPlayers)
                {
                    if (player.connection.IsKeepAliveValid() == false)
                    {
#if !DEBUG
                    player.connection.Disconnect();
#endif
                    }
                }
            }

            #endregion KeepAlive

            #region THREAD
            protected override void ThreadTick()
            {
                try
                {
                    MigratePendingPlayersToLoginServer();
                    MoveDisconnectedClientConnectionsToInactive();

                    ManageAllClientPackets();
                    MoveServerPacketsIntoOutgoingClients();

                    PromoteNewServers();
                    HandleAllDisconnectedServers();

                    ManageServerKeepAlive();
                    ManageClientKeepAlive();

                    LogAllStats();
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                }
            }
            #endregion THREAD

            #region PLAYER_STATE
            void MigratePendingPlayersToLoginServer()
            {
                if (extremeLogging == true)
                    Console.WriteLine("MigratePendingPlayersToLoginServer");

                List<PlayerConnectionState> tempList;
                lock (connectedLock)
                {
                    tempList = new List<PlayerConnectionState>(newPlayersAwaitingConfirmation);
                }

                foreach (var newPlayer in tempList)
                {
                    var packet = newPlayer.RetrievePacket();
                    if (packet == null)
                    {
                        continue;
                    }
                    var clientIdPacket = packet as ClientIdPacket;
                    if (clientIdPacket != null)
                    {
                        newPlayer.gameId = clientIdPacket.Id;
                        newPlayer.versionAndHandshakeComplete = true;
                        //loginServerProxy.HandleNewConnection(newPlayer);
                        IntrepidSerialize.ReturnToPool(packet);
                        continue;
                    }

                    // If we're here, it means the client sent the wrong packet, so disconnect them
                    lock (connectedLock)
                    {
                        newPlayersAwaitingConfirmation.Remove(newPlayer);
                    }
                    if (extremeLogging == true)
                        Console.WriteLine("Expected ClientIdPacket, received " + packet.GetType());
                    // TODO: Send disconnect packet?
                    newPlayer.Disconnect();

                    IntrepidSerialize.ReturnToPool(packet);
                }

                lock (connectedLock)
                {
                    for (int i = newPlayersAwaitingConfirmation.Count - 1; i >= 0; i--)
                    {
                        var newPlayer = newPlayersAwaitingConfirmation[i];
                        if (newPlayer.versionAndHandshakeComplete == true)
                        {
                            newPlayersAwaitingConfirmation.RemoveAt(i);
                        }
                    }
                }
            }

            private void OnNewPlayerConnection(Socket s)
            {
                PlayerConnectionState state = new PlayerConnectionState(s);
                lock (connectedLock)
                {
                    newPlayersAwaitingConfirmation.Add(state);
                }

                NotifyEndpoint_ServerId(state);
            }

            //Todo: It's not really the gateways job to kick inactive players?
            //Also, "inactive" is not the correct word here, it seems to be a way to handle half-connected sockets (client has disconnected, but we haven't tidied up yet)
            void MoveDisconnectedClientConnectionsToInactive()
            {
                lock (connectedLock)
                {
                    for (int i = players.Count - 1; i >= 0; i--)
                    {
                        var player = players[i];
                        if (player.connection.MarkedAsSocketClosed == true)
                        {
                            inactivePlayers.Add(player);
                            players.Remove(player);
                            NotifyGameServerThatPlayerHasDisconnected(player.connectionId, player.gameId, player.accountId);
                        }
                    }
                }
            }

            public void NewPlayerLoginResult(PlayerConnectionState playerConnection, bool success, PlayerSaveState save)
            {
                if (success == true)
                {
                    int connectionId = nextGateWayPlayerConnectionId++;

                    UserSocket gp = new UserSocket(connectionId, this, playerConnection, save.accountId);
                    playerConnection.finishedLoginSuccessfully = success;

                    // Get the server instance id
                    lock (connectedLock)
                    {
                        players.Add(gp);
                    }

                    PassSaveStateToGameServer(playerConnection.gameId, connectionId, save);
                    gp.HasNotifiedGameServer = true;
                }
                LoginCredentialValid lcv = (LoginCredentialValid)IntrepidSerialize.TakeFromPool(PacketType.LoginCredentialValid);
                lcv.isValid = success;

                playerConnection.Send(lcv);
            }
            #endregion PLAYER_STATE

            #region SERVER_STATE
            private void OnNewServerConnection(Socket socket)
            {
                ServerConnectionState wrapper = new ServerConnectionState(socket);
                lock (connectedLock)
                {
                    newServersAwaitingConfirmation.Add(wrapper);
                }

                IPEndPoint remoteIpEndPoint = socket.RemoteEndPoint as IPEndPoint;
                //Console.WriteLine("OnNewServerConnection {0}", remoteIpEndPoint.Address);
                NotifyEndpoint_ServerId(wrapper);
            }

            private void NotifyEndpoint_ServerId(ConnectionState connection)
            {
                ServerIdPacket packet = (ServerIdPacket)IntrepidSerialize.TakeFromPool(PacketType.ServerIdPacket);
                packet.Type = ServerIdPacket.ServerType.Gateway;
                packet.MapId = 0;
                packet.Id = 0;
                connection.Send(packet);
            }

            void HandleAllDisconnectedServers()
            {
                if (extremeLogging == true)
                    Console.WriteLine("HandleAllDisconnectedServers");

                for (int i = newServersAwaitingConfirmation.Count - 1; i >= 0; i--)
                {
                    var server = newServersAwaitingConfirmation[i];
                    if (server.MarkedAsSocketClosed == true)
                    {
                        lock (connectedLock)
                        {
                            newServersAwaitingConfirmation.RemoveAt(i);
                        }
                    }
                }
             /*   foreach (var server in servers)
                {
                    if (server.MarkedAsSocketClosed == true)
                    {
                        servers.Remove(server);
                        Console.WriteLine("Server removed:{0} of type {1}", server.gameId, server.serverType);
                    }
                }*/
            }

            void PromoteNewServers()
            {
                if (extremeLogging == true)
                    Console.WriteLine("PromoteNewServers");

                for (int i = newServersAwaitingConfirmation.Count - 1; i >= 0; i--)
                {
                    var server = newServersAwaitingConfirmation[i];
                    if (server.MarkedAsSocketClosed == true)
                    {
                        continue;
                    }
                    if (!server.ProcessIdPackets())
                    {
                        // Server either gave us too much data, or the wrong packet
                        Console.WriteLine("Server gave us too many packets, or not a ServerIdPacket, disconnecting: {0}", server);
                        // TODO: Send disconnect packet?
                        server.Disconnect();
                    }
                   /* if (server.versionAndHandshakeComplete == true)// todo: timeout servers within a few seconds... prevent hacking.
                    {
                        if (!servers.Add(server))
                        {
                            Console.WriteLine("Server with duplicate game id, disconnecting: {0}", server.gameId);
                            // TODO: Send disconnect packet?
                            server.Disconnect();
                        }
                        lock (connectedLock)
                        {
                            newServersAwaitingConfirmation.RemoveAt(i);
                        }
                        IPAddress remoteIpEndPoint = server.Address;
                        Console.WriteLine("OnNewServerConnection {0}", remoteIpEndPoint);
                    }*/
                }
            }
            void NotifyGameServerThatPlayerHasDisconnected(int clientConnectionId, int gameId, int accountId)
            {
                Console.WriteLine("Disconnect on gateway (2), connectionId: " + clientConnectionId);

              /*  foreach (var server in servers)
                {
                    if (server.serverType != ServerIdPacket.ServerType.Game
                        || server.gameId == gameId)
                    {
                        ClientDisconnectPacket cdp = (ClientDisconnectPacket)IntrepidSerialize.TakeFromPool(PacketType.ClientDisconnect);
                        cdp.connectionId = clientConnectionId;
                        cdp.accountId = accountId;
                        server.Send(cdp);
                    }
                }*/
            }

            #endregion SERVER_STATE

            //------------------------------------------------------------------------------

            #region MARSHALLING_PACKETS
            void PassSaveStateToGameServer(int gameId, int connectionId, PlayerSaveState save)
            {
                PlayerSaveStatePacket player = (PlayerSaveStatePacket)IntrepidSerialize.TakeFromPool(PacketType.PlayerSaveState);
                player.state = save;
                AddIncomingPacket(player, connectionId, gameId);
            }

            void PassOutgoingPacketsOntoClients()
            {
                Queue<SocketPacketPair> tempPackets;
                lock (containersLock)
                {
                    tempPackets = containers.outgoingPackets;
                    containers.outgoingPackets = new Queue<SocketPacketPair>();
                }

                lock (connectedLock)
                {
                    foreach (var pair in tempPackets)
                    {
                        BasePacket bp = pair.packet;
                        int connectionId = pair.connectionId;
                        int gameId = pair.gameId;

                        bool found = false;
                        foreach (var player in players)
                        {
                            if (player.connectionId == connectionId)
                            {
                                player.AddPacket(bp);
                                found = true;
                                break;// first to match
                            }
                        }
                        if (found == false)
                        {
                            IntrepidSerialize.ReturnToPool(bp);
                        }
                    }
                }
            }

        /*    void PassPendingPacketsOntoServers()
            {
                List<SocketPacketPair> tempPackets;
                lock (containersLock)
                {
                    tempPackets = new List<SocketPacketPair>(containers.incomingPackets);
                    containers.incomingPackets.Clear();
                }
                numPlayerPackets += tempPackets.Count;
                foreach (var pair in tempPackets)
                {
                    servers.RoutePacketToServers(pair);
                }
            }*/

            void MoveServerPacketsIntoOutgoingClients()
            {
                if (extremeLogging == true)
                    Console.WriteLine("MoveServerPacketsIntoOutgoingClients");

                List<UserSocket> tempPlayerList;
                lock (connectedLock)
                {
                    tempPlayerList = new List<UserSocket>(players);
                }
               /* foreach (var server in servers)
                {
                    if (server.HasNewData() == true)
                    {
                        List<BasePacket> newData = server.RetrieveData();
                        numServerPackets += newData.Count;
                        foreach (var packet in newData)
                        {
                            if (packet is ServerConnectionHeader)
                            {
                                currentConnectionIdForHeader = (packet as ServerConnectionHeader).connectionId;
                                currentPlayerForHeader = FindPlayer(currentConnectionIdForHeader, tempPlayerList);

                                if (currentPlayerForHeader == null)
                                {
                                    ClientDisconnectPacket clientDisconnect = (ClientDisconnectPacket)IntrepidSerialize.TakeFromPool(PacketType.ClientDisconnect);
                                    clientDisconnect.connectionId = currentConnectionIdForHeader;
                                    Console.WriteLine("Disconnecting connectionId (1): " + clientDisconnect.connectionId);
                                    server.Send(clientDisconnect);
                                    server.skipNextPacket = true;
                                }
                                else
                                {
                                    server.skipNextPacket = false;
                                }

                                IntrepidSerialize.ReturnToPool(packet);
                            }
                            else
                            {
                                if (currentPlayerForHeader != null)
                                {
                                    if (packet.PacketType == PacketType.ServerPingHopper)
                                    {
                                        string name = Assembly.GetCallingAssembly().GetName().Name;
                                        (packet as ServerPingHopperPacket).Stamp(name + ": game to client");
                                    }
                                    // copy packet
                                    currentPlayerForHeader.connection.Send(packet);

                                    //Reset state for next packet pair.
                                    currentPlayerForHeader = null;
                                    currentConnectionIdForHeader = 0;
                                }
                                else
                                {
                                    if (!server.skipNextPacket)
                                    {
                                        Console.WriteLine("Received packet without ServerConnectionHeader packet: {0} from {1} with id {2}", packet, server.serverType, server.gameId);
                                    }
                                    else
                                    {
                                        server.skipNextPacket = false;
                                    }
                                    IntrepidSerialize.ReturnToPool(packet);
                                }
                            }
                        }
                    }
                }*/
            }

            public void AddOutgoingPacket(BasePacket packet, int connectionId)
            {
                lock (containersLock)
                {
                    containers.outgoingPackets.Enqueue(new SocketPacketPair(connectionId, packet));
                }
            }

            public void AddIncomingPacket(BasePacket packet, int clientConnectionId)
            {
                lock (containersLock)
                {
                    containers.incomingPackets.Enqueue(new SocketPacketPair(clientConnectionId, packet));
                }
            }
            public void AddIncomingPacket(BasePacket packet, int clientConnectionId, int gameId)
            {
                lock (containersLock)
                {
                    containers.incomingPackets.Enqueue(new SocketPacketPair(clientConnectionId, gameId, packet));
                }
            }
            void ManageAllClientPackets()
            {
                if (extremeLogging == true)
                    Console.WriteLine("ManageAllClientPackets");

                List<UserSocket> tempPlayers;
                lock (connectedLock)
                {
                    tempPlayers = new List<UserSocket>(players);
                }
                foreach (var player in tempPlayers)
                {
                    player.Update();
                    //PassPendingPacketsOntoServers();
                }

                PassOutgoingPacketsOntoClients();
            }
            #endregion MARSHALLING_PACKETS

            //-----------------------------------------------------------------------------

            //-----------------------------------------------------------------------------

            static UserSocket FindPlayer(int connectionId, List<UserSocket> tempPlayerList)
            {
                foreach (var player in tempPlayerList)
                {
                    if (player.connectionId == connectionId)
                        return player;
                }
                return null;
            }

            #region STATS
            public static void ClearCurrentConsoleLine()
            {
                int currentLineCursor = Console.CursorTop;
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, currentLineCursor);
            }

            void LogAllStats()
            {
                long elapsedTime = screenRefreshTimer.ElapsedMilliseconds;
                if (elapsedTime < 1000)
                {
                    return;
                }
                screenRefreshTimer.Restart();
                Dictionary<int, int> gameIdCounter = new Dictionary<int, int>();
                /*int numServers = servers.GetNumberOfServers(ServerIdPacket.ServerType.Game);
                List<int> ids = servers.GetGameServerIds();
                foreach (var id in ids)
                {
                    gameIdCounter[id] = 0;
                }*/
                int numPlayers = players.Count;
                foreach (var p in players)
                {
                    int gameId = p.gameId;
                    if (gameIdCounter.ContainsKey(gameId))
                        gameIdCounter[gameId]++;
                }

                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("Gateway launched {0}", launchDate.ToShortTimeString());
                TimeSpan interval = DateTime.Now - launchDate;
                Console.WriteLine("Gateway runtime length {0} seconds", (int)interval.Duration().TotalSeconds);
                Console.CursorTop = 3;
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("----------------------------------------------");

                Console.WriteLine("Player listen: \n\t{0} for connections on {1}:{2}", playerConnectionListener.ServerName, playerConnectionListener.Address, playerConnectionListener.Port);
                Console.WriteLine("Server listen: \n\t{0} for connections on {1}:{2}", serverListener.ServerName, serverListener.Address, serverListener.Port);
                Console.WriteLine("----------------------------------------------");




                foreach (var count in gameIdCounter)
                {
                    if (count.Value == 0)
                        Console.ForegroundColor = ConsoleColor.Red;
                    else if (count.Value < 10)
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    else
                        Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Game ID: {0}, players: {1}", count.Key, count.Value);
                }

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(String.Format("{0, -24}{1, 12}", "Total players:", numPlayers));
                //Console.WriteLine(String.Format("{0, -24}{1, 12}", "Total servers:", numServers));
                Console.WriteLine(String.Format("{0, -24}{1, 12}", "Players packet velocity:", (numPlayerPackets - numPlayerPacketsLastTimeStamp)));
                Console.WriteLine(String.Format("{0, -24}{1, 12}", "Servers packet velocity:", (numServerPackets - numServerPacketsLastTimeStamp)));
                Console.WriteLine(String.Format("{0, -24}{1, 12}", "Packets from players:", numPlayerPackets));
                Console.WriteLine(String.Format("{0, -24}{1, 12}", "Packets from servers:", numServerPackets));

                numPlayerPacketsLastTimeStamp = numPlayerPackets;
                numServerPacketsLastTimeStamp = numServerPackets;

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("----------------------------------------------");

            }
            #endregion
        }


}