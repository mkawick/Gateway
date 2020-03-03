using System;
using System.Threading;
using CommonLibrary;
using Testing;

namespace Test_OptimizingDataPacketsServer
{
    class OptimizingServer
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Server listening for optimization server");
            Console.WriteLine("  Press esc to exit.\n\n");

            bool testAgainstRealLoginServer = false;
            SocketWrapperSettings socketSettings = null;
            if (testAgainstRealLoginServer)
            {
                socketSettings = new SocketWrapperSettings("localhost", 11002);
            }
            IntrepidSerialize.Init();
            LoginServerProxy loginServer = new LoginServerProxy(socketSettings);
            ServerController controller = new ServerController(loginServer);
            ServerMockConnectionState mock = new ServerMockConnectionState(controller);

            controller.SetMaxFPS(NetworkConstants.GatewayFPS);
            controller.StartService();
            loginServer.StartService();
            mock.ConnectMock();

            Thread.Sleep(300);// allow systems to init
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

        // a test should blast 20 megs to the client and then measure the time.
        // 1) send start test packet to signal to the client to measure
        // 2) choose a size of each datablob packet
        // 3) send all of the blobs
        // 4) send a closure packet
        // 5) Client send a response packet with total time from start packet
        // 6) Server goes quiscent
        // 7) Server tries again with smaller datablobs and measures the time
        // 8) Server tries again with larger.. 
        // 9) whichever was fastest, move the median in that direction and try again
        // 10) settle after 30 tests.
        public class TestRunner
        {
            enum state
            {
                preparing,
                settingupNextTest,
                packetizeTheSendBuffer,
                sendingStartPacket,
                sendDataBlobs,
                sendingClosurePacket,
                waitingForClientTimePacket,
                storingResult,
                decideWhereToGoAndHowFast,
                endOfTest
            }
            int size = 20 * 1024 * 1024;
            bool isRunning = false;
            ServerMockConnectionState smcs = null;
            byte[] sendBuffer;

            public TestRunner(ServerMockConnectionState _smcs)
            {
                smcs = _smcs;
                sendBuffer = new byte[size];
                for(int i=0; i<size; i++)
                {
                    sendBuffer[i] = (byte) (i % 64);
                }
            }

            bool IsTestRunning { get{ return isRunning; } }
            public void Update()
            {
                AdvanceStateMachine();
            }
            void AdvanceStateMachine()
            { }
        }
    }
}
