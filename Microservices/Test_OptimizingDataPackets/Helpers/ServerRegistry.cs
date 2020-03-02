using CommonLibrary;
using Packets;
using System.Collections.Generic;
using System.Collections;

namespace Testing
{
    public class ServerRegistry : IEnumerable<ServerConnectionState>
    {
        private List<ServerGroupByType> groups;

        public ServerRegistry()
		{
			groups = new List<ServerGroupByType>();
		}

        public int GetNumberOfServers(ServerIdPacket.ServerType type)
        {
            ServerGroupByType group = groups.Find(g => g.Type == type);
            if (group == null)
            {
                return 0;
            }
            return group.Count;
        }

        public List<int> GetGameServerIds()
        {
            List<int> serverIds = new List<int>();

            ServerIdPacket.ServerType type = ServerIdPacket.ServerType.Game;
            ServerGroupByType group = groups.Find(g => g.Type == type);
            if (group != null)
            {
                foreach (var g in group)
                {
                    serverIds.Add(g.gameId);
                }
            }
            return serverIds;
        }

        public ServerConnectionState Find(ServerIdPacket.ServerType type, int gameId)
		{
			ServerGroupByType group = groups.Find(g => g.Type == type);
			if (group == null)
			{
				return null;
			}
			return group.Find(gameId);
		}

		public bool Add(ServerConnectionState server)
		{
			ServerIdPacket.ServerType type = server.serverType;
			ServerGroupByType group = groups.Find(g => g.Type == type);
			if (group == null)
			{
				group = new ServerGroupByType(type);
				groups.Add(group);
			}
			return group.Add(server);
		}

		public bool Remove(ServerConnectionState server)
		{
			for (int i = groups.Count - 1; i >= 0; i--)
			{
				var group = groups[i];
				if (group.Type == server.serverType)
				{
					if (group.Remove(server))
					{
						if (group.IsEmpty())
						{
							groups.RemoveAt(i);
						}
						return true;
					}
				}
			}
			return false;
		}

		public bool RoutePacketToServers(SocketPacketPair pair)
		{
			foreach (var group in groups)
			{
				// Assumes only one server accepts the packet
				if (group.RoutePacketToServer(pair.gameId, pair.connectionId, pair.packet))
				{
					return true;
				}
			}
			return false;
		}

		public IEnumerator<ServerConnectionState> GetEnumerator()
		{
			for (int i = groups.Count - 1; i >= 0; i--)
			{
				ServerGroupByType group = groups[i];
				var groupEnum = group.GetEnumerator();
				while (groupEnum.MoveNext())
				{
					yield return groupEnum.Current;
					if (groups.Count <= i || group != groups[i])
					{
						// We've removed the group we're iterating over,
						// so skip over it
						break;
					}
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			for (int i = groups.Count - 1; i >= 0; i--)
			{
				ServerGroupByType group = groups[i];
				var groupEnum = group.GetEnumerator();
				while (groupEnum.MoveNext())
				{
					yield return groupEnum.Current;
					if (group != groups[i])
					{
						// We've removed the group we're iterating over,
						// so skip over it
						break;
					}
				}
			}
		}
    }
}
