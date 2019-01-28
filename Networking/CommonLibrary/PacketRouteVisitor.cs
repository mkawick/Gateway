
using CommonLibrary;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;

namespace Packets
{
    public class ServerGroupByType : IEnumerable<ServerConnectionState>
    {
        public ServerIdPacket.ServerType Type { get; private set; }
        private List<ServerConnectionState> servers = new List<ServerConnectionState>();
        public int Count => servers.Count; 

        public ServerGroupByType(ServerIdPacket.ServerType _type)
        {
            Type = _type;
            servers = new List<ServerConnectionState>();
        }

        public bool IsEmpty()
        {
            return servers.Count == 0;
        }

        public bool RoutePacketToServer(int gameId, int connectionId, BasePacket packet)
        {
            bool result = RouteToGame(gameId, connectionId, packet);  
                
            return result;
        }

        bool RouteToGame(int gameId, int connectionId, BasePacket packet)
        {
            if(Type == ServerIdPacket.ServerType.Game)
            {
                // CS: Don't filter packets yet - we only get packets in here
                // once we're logged in, and we've got a valid gameId, so
                // we can, for the moment, just assume all packets go to the game server

                //if(packet.PacketType == PacketType.WorldEntity ||
                //    packet.PacketType == PacketType.CharacterFull ||
                //    packet.PacketType == PacketType.PlayerFull ||
                //    packet.PacketType == PacketType.PlayerSaveState
                //    )
                {
                    foreach(var socketWrapper in servers)
                    {
                        if (socketWrapper.gameId == gameId)
                        {
                            socketWrapper.AddConnection(connectionId);

                            ServerConnectionHeader header = (ServerConnectionHeader)IntrepidSerialize.TakeFromPool(PacketType.ServerConnectionHeader);
                            header.connectionId = connectionId;
                            socketWrapper.Send(header);

                            if (packet.PacketType == PacketType.ServerPingHopper)
                            {
                                string name = Assembly.GetCallingAssembly().GetName().Name;
                                (packet as ServerPingHopperPacket).Stamp(name + ": client to game");
                            }
                            socketWrapper.Send(packet);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public ServerConnectionState Find(int gameId)
        {
            return servers.Find(g => g.gameId == gameId);
        }

        public bool Add(ServerConnectionState server)
        {
            if (server.serverType != Type)
            {
                return false;
            }

            if(servers.Find(e=>e.gameId == server.gameId) != null)
            {
                return false;
            }

            servers.Add(server);
            Console.WriteLine("Server added:{0} of type {1}", server.gameId, server.serverType);
            return true;
        }

        public bool Remove(ServerConnectionState server)
        {
            if (server.serverType != Type)
            {
                return false;
            }

            return servers.Remove(server);
        }

        public IEnumerator<ServerConnectionState> GetEnumerator()
        {
            for (int i = servers.Count - 1; i >= 0; i--)
            {
                yield return servers[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = servers.Count - 1; i >= 0; i--)
            {
                yield return servers[i];
            }
        }
    }
 
}