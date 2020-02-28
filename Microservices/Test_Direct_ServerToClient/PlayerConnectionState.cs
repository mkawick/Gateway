using CommonLibrary;
using Packets;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;

namespace Test_Direct_ServerToClient
{
    public class PlayerConnectionState : ConnectionState
    {
        public int tempId = 0;
        public bool finishedLoginSuccessfully = false;
        //private bool shouldSendPacketAndCloseImmediately = false;
        //private Stopwatch sw;
        //Timer aTimer;

        public PlayerConnectionState(Socket handler) : base(handler)
        {
            
        }

        void SendPacketAndSetForImmediateDisconnect(BasePacket bp)
        {
            socket.Disconnect();
        }

          public bool IsKeepAliveValid()
          {
              return true;
          }
    }
}
