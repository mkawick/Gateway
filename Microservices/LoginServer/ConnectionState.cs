using System.Net.Sockets;

namespace LoginServer
{
    public class ConnectionState : CommonLibrary.ConnectionState
    {
        public int connectionId = 0; // not always used but helpful for tracking
        
        public ConnectionState(Socket handler) : base(handler)
        {}
    }
}
