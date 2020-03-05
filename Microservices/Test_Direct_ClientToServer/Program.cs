using CommonLibrary;
using Network;
using Packets;
using System;
using System.Collections.Generic;
using System.Threading;
using Vectors;

namespace Test_Direct_ClientToServer
{
    internal class Program
    {
        public static object Uitls { get; private set; }

        private static void ImageReceived(byte[] bytes, int size)
        {
            Console.WriteLine("Data received");
        }

        private static void Main(string[] args)
        {
            CommonLibrary.Parser.ParseCommandLine(args);
            Int64 applicationId = 1234;/*CommonLibrary.Parser.ApplicationId;
            if (applicationId == 0)
            {
                applicationId = Network.Utils.GetIPBasedApplicationId();
            }*/
            float sleepTime = 1000.0f / (float)CommonLibrary.Parser.FPS;
            string ipAddr = CommonLibrary.Parser.ipAddr;
            ipAddr = "192.168.30.214";

            Console.WriteLine("Client talking to Server.");
            Console.WriteLine("  Press L to login (auto login is set).");
            Console.WriteLine("  Press P to update player position.");
            Console.WriteLine("  Press K to change to main player position.");
            Console.WriteLine("  ** application id = {0} **", applicationId);
            Console.WriteLine("  Press esc to update player position.\n\n");
            ushort port = 11000;
            Testing.ClientController testClient = new Testing.ClientController(ipAddr, port, applicationId);
            testClient.OnImageReceived += ImageReceived;

            ConsoleKey key;
            do
            {
                while (!Console.KeyAvailable)
                {
                    Thread.Sleep(20);
                    testClient.Update();
                }
                key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.L)
                {
                    LoginCredentials cred = (LoginCredentials)IntrepidSerialize.TakeFromPool(PacketType.LoginCredentials);
                    cred.password.Copy("password");
                    cred.playerName.Copy("mickey");
                    testClient.Send(cred);
                    Console.WriteLine("login sent.");
                }
                if (key == ConsoleKey.D4)
                {
                    RenderSettings settings = (RenderSettings)IntrepidSerialize.TakeFromPool(PacketType.RenderSettings);
                    settings.screenWidth = 1280;
                    settings.screenHeight = 720;
                    settings.screenFormat = RenderSettings.RenderFormat.RGBA;
                    settings.maxFPS = 20;

                    testClient.Send(settings);
                    Console.WriteLine("settings sent.");
                }
                if (key == ConsoleKey.D5)
                {
                    RenderSettings settings = (RenderSettings)IntrepidSerialize.TakeFromPool(PacketType.RenderSettings);
                    settings.screenWidth = 1280;
                    settings.screenHeight = 720;
                    settings.screenFormat = RenderSettings.RenderFormat.RGBA;
                    settings.maxFPS = 4;

                    testClient.Send(settings);
                    Console.WriteLine("settings sent.");
                }
                if (key == ConsoleKey.D6)
                {
                    RenderSettings settings = (RenderSettings)IntrepidSerialize.TakeFromPool(PacketType.RenderSettings);
                    settings.screenWidth = 1280;
                    settings.screenHeight = 720;
                    settings.screenFormat = RenderSettings.RenderFormat.RGBA;
                    settings.maxFPS = -1;

                    testClient.Send(settings);
                    Console.WriteLine("settings sent.");
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
                    we.position.Set(new Vector3(44, 0.20f, 21));
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
                if (key == ConsoleKey.B)
                {
                    Console.WriteLine("Sending blob");

                    //DataBlob blob = new DataBlob();
                    DataBlob blob = (DataBlob)IntrepidSerialize.TakeFromPool(PacketType.DataBlob);

                    int size = 800;
                    byte[] data = new byte[size];
                    for (int i = 0; i < size; i++)
                    {
                        data[i] = (byte)i;
                    }
                    blob.Prep(data, size);

                    testClient.Send(blob);
                }
                if (key == ConsoleKey.A)
                {
                    int size = 8000;
                    byte[] data = new byte[size];
                    for (int i = 0; i < size; i++)
                    {
                        data[i] = (byte)i;
                    }
                    Utils.DatablobAccumulator acc = new Utils.DatablobAccumulator();
                    List<DataBlob> blobs = acc.PrepToSendRawData(data, size);

                    foreach (var blob in blobs)
                    {
                        testClient.Send(blob);
                    }
                }
                if (key == ConsoleKey.S)
                {
                    int size = 2000000;
                    byte[] data = new byte[size];
                    for (int i = 0; i < size; i++)
                    {
                        data[i] = (byte)i;
                    }
                    Utils.DatablobAccumulator acc = new Utils.DatablobAccumulator();
                    List<DataBlob> blobs = acc.PrepToSendRawData(data, size);

                    var Timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                    Console.WriteLine(Timestamp);
                    foreach (var blob in blobs)
                    {
                        testClient.Send(blob);
                    }
                }
                if (key == ConsoleKey.R)
                {
                    RequestPacket request = (RequestPacket)IntrepidSerialize.TakeFromPool(PacketType.RequestPacket);
                    request.type = RequestPacket.RequestType.RequestRenderFrame;
                    var Timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                    //Console.WriteLine("request start {0} ms", Timestamp);
                    testClient.startTimeMilliseconds = Timestamp;
                    testClient.Send(request);
                }
            } while (key != ConsoleKey.Escape);

            testClient.Close();
        }
    }
}
