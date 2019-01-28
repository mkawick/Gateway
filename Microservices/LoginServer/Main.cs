using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LoginServer
{
    partial class LoginServer : CommonLibrary.ThreadWrapper
    {
        static void Main(string[] args)
        {

            LoginServer loginServer = new LoginServer();
            LoginSocket socketListener = new LoginSocket();
            

            LoginSocket.loginServer = loginServer;

            loginServer.StartService();
            socketListener.StartListening();
            
        }
    }
}