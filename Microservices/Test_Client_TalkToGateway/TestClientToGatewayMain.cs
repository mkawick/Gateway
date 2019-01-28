using Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vectors;

namespace Test_Client_TalkToGateway
{
    class TestClientToGatewayMain
    {

        static void Main(string[] args)
        {
            CommonLibrary.Parser.ParseCommandLine(args);
            int applicationId = CommonLibrary.Parser.ApplicationId;
            if (applicationId == 0)
            {
                applicationId = Network.Utils.GetIPBasedApplicationId();
            }
            float sleepTime = 1000.0f / (float)CommonLibrary.Parser.FPS;
            string ipAddr = CommonLibrary.Parser.ipAddr;

            Console.WriteLine("Client talking to Gateway.");
            Console.WriteLine("  Press L to login (auto login is set).");
            Console.WriteLine("  Press P to update player position.");
            Console.WriteLine("  Press K to change to main player position.");
            Console.WriteLine("  ** application id = {0} **", applicationId);
            Console.WriteLine("  Press esc to update player position.\n\n");
            ushort port = 11000;
            TestClientToGatewayController testClient = new TestClientToGatewayController(ipAddr, port, applicationId);

            ConsoleKey key;
            do
            {
                while (!Console.KeyAvailable)
                {

                }
                key = Console.ReadKey(true).Key;
                if(key == ConsoleKey.L)
                {
                    LoginCredentials cred = (LoginCredentials)IntrepidSerialize.TakeFromPool(PacketType.LoginCredentials);
                    cred.password.Copy("password");
                    cred.playerName.Copy("mickey");
                    testClient.Send(cred);
                    Console.WriteLine("login sent.");
                }

                if (key == ConsoleKey.P)
                {
                    WorldEntityPacket we = (WorldEntityPacket)IntrepidSerialize.TakeFromPool(PacketType.WorldEntity);
                    we.entityId = 1024;
                    we.position.Set(new Vector3(10, 20, 30));
                    we.rotation.Set(new Vector3(10, 20, 30));

                    testClient.Send(we);
                    Console.WriteLine("position sent.");
                }
                if (key == ConsoleKey.K)
                {
                    WorldEntityPacket we = (WorldEntityPacket)IntrepidSerialize.TakeFromPool(PacketType.WorldEntity);
                    we.entityId = 1024;
                    we.position.Set( new Vector3(44, 0.20f, 21));
                    we.rotation.Set(new Vector3(5, 25, 30));

                    testClient.Send(we);
                    Console.WriteLine("position sent.");
                }
                if (key == ConsoleKey.M)
                {
                    Console.WriteLine("Major list of crap to send");
                    var test = new Packets.TestPacket();
                    Packets.TestDataBlob blob1 = new Packets.TestDataBlob(1, 2);
                    Packets.TestDataBlob blob2 = new Packets.TestDataBlob(3, 4);
                    Packets.TestDataBlob blob3 = new Packets.TestDataBlob(5, 6);
                    test.listOfBlobs.listOfSerializableItems.Add(blob1);
                    test.listOfBlobs.listOfSerializableItems.Add(blob2);
                    test.listOfBlobs.listOfSerializableItems.Add(blob3);
                    testClient.Send(test);
                }
                if (key == ConsoleKey.T)
                {
                    Console.WriteLine("Major list of crap to send");
                    ServerPingHopperPacket hopper = (ServerPingHopperPacket)IntrepidSerialize.TakeFromPool(PacketType.ServerPingHopper);
                    hopper.Stamp("client start");
                    testClient.Send(hopper);
                }
            } while (key != ConsoleKey.Escape);

            testClient.Close();
        }
    }
}
