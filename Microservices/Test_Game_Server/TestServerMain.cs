using CommonLibrary;
using Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Test_game_server;
using Vectors;

namespace Test_Game_Server
{
    class TestServerMain
    {
        static void Main(string[] args)
        {
            CommonLibrary.Parser.ParseCommandLine(args);
            Int64 applicationId = CommonLibrary.Parser.ApplicationId;
            if (applicationId == 0)
            {
                applicationId = Network.Utils.GetIPBasedApplicationId();
            }
            float sleepTime = 1000.0f / (float)CommonLibrary.Parser.FPS;
            string ipAddr = CommonLibrary.Parser.ipAddr;

            Console.WriteLine("Game server talking to Gateway.");
            //Console.WriteLine("  Press L to login (auto login is set).");
            Console.WriteLine("  Press P to update player position.");
            Console.WriteLine("  ** application id = {0} **", applicationId);
            Console.WriteLine("  Press esc to update player position.\n\n");

            ushort port = 11004;
            TestGameServerController testServer = new TestGameServerController(ipAddr, port, applicationId);

            //testServer.Star
            //testServer.
            // open  socket to connect to the gateway
            // upon connect, send ServerIdPacket:Game to connected 
            // receive ServerIdPacket:Gateway
            // upon disconnect, reconnect

            ConsoleKey key;
            do
            {
                while (!Console.KeyAvailable)
                {
                    Thread.Sleep(20);
                }
                key = Console.ReadKey(true).Key;

                if (key == ConsoleKey.P)
                {
                    WorldEntityPacket we = (WorldEntityPacket)IntrepidSerialize.TakeFromPool(PacketType.WorldEntity);
                    we.entityId = 1024;
                    we.position.Set( new Vector3(10, 20, 30));
                    we.rotation.Set(new Vector3(10, 20, 30));

                    testServer.Send(we);
                    Console.WriteLine("position sent.");
                }
                
            } while (key != ConsoleKey.Escape);

            testServer.Close();
        }
    }
}
