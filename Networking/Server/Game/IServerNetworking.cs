using Packets;
using System;
using System.Collections.Generic;

namespace Server
{
    public interface IServerNetworking : IPacketSource, IFrameTicker
    {
        event Action<int, PlayerSaveState> OnClientConnected;
        event Action<int> OnClientDisconnected;

        /// <summary>
        /// Send this packet to these entities
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="entityIDs"></param>
        void Send(BasePacket packet, IEnumerable<int> entityIDs);

        void StartService();
        void StopService();

        int GetNextEntityID();

        //VERY TEMPORARY!
        void SendToProfileServer(BasePacket packet);
    }
}