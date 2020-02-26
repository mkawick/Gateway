using System;
using System.Threading;
using CommonLibrary;

namespace Test_Direct_ServerToClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("listening server");
            // Console.WriteLine("  Press L to login (auto login is set).");
            // Console.WriteLine("  Press P to update player position.");
            Console.WriteLine("  Press esc to exit.\n\n");

            bool testAgainstRealLoginServer = false;
            SocketWrapperSettings socketSettings = null;
            if (testAgainstRealLoginServer)
            {
                socketSettings = new SocketWrapperSettings("localhost", 11002);
            }
            Packets.IntrepidSerialize.Init();
            LoginServerProxy loginServer = new LoginServerProxy(socketSettings);
            ServerController controller = new ServerController(loginServer);

            ServerMockConnectionState mock = new ServerMockConnectionState(controller);

            controller.SetMaxFPS(NetworkConstants.GatewayFPS);

          
            controller.StartService();
            
            
            loginServer.StartService();
            mock.ConnectMock();

            Thread.Sleep(1000);// allow systems to init
            controller.NewServerConnection(mock);
            ConsoleKey key;
            do
            {
                while (!Console.KeyAvailable)
                {
                    Thread.Sleep(20);
                }
                key = Console.ReadKey(true).Key;

            } while (key != ConsoleKey.Escape);
            loginServer.Cleanup();
            controller.Cleanup();
        }
    }
}
