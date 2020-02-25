using System;
using System.Threading;
using Packets;
using Vectors;
using CommonLibrary;

namespace Test_Direct_ServerToClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Gateway");
            // Console.WriteLine("  Press L to login (auto login is set).");
            // Console.WriteLine("  Press P to update player position.");
            Console.WriteLine("  Press esc to exit.\n\n");

            bool testAgainstRealLoginServer = false;
            SocketWrapperSettings socketSettings = null;
            if (testAgainstRealLoginServer)
            {
                socketSettings = new SocketWrapperSettings("localhost", 11002);
            }
            //LoginServerProxy loginServer = new LoginServerProxy(socketSettings);
            //GatewayMain gateway = new GatewayMain(loginServer);
            ServerController gateway = new ServerController();

            gateway.SetMaxFPS(NetworkConstants.GatewayFPS);
            //loginServer.StartService();
            gateway.StartService();

            ConsoleKey key;
            do
            {
                while (!Console.KeyAvailable)
                {
                    Thread.Sleep(20);
                }
                key = Console.ReadKey(true).Key;

            } while (key != ConsoleKey.Escape);
            //loginServer.Cleanup();
            gateway.Cleanup();
        }
    }
}
