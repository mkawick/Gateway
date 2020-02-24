using System;
using System.Collections.Generic;
using Network;

namespace Test_client_login
{
    
    class TestClientLoginMain
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

            ushort port = 11002;
            TestLoginController game = new TestLoginController(ipAddr, port);
            ConsoleKey key;
            do
            {
                while (!Console.KeyAvailable)
                {

                }
                key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.UpArrow)
                {
                    Console.WriteLine("Send request set to succeed");
                    game.SendLoginRequest("mickey", "password", "hungry hippos");
                }
                if (key == ConsoleKey.DownArrow)
                {
                    Console.WriteLine("Send request set to fail");
                    game.SendLoginRequest("mickey", "password1", "hungry hippos");
                }
                if (key == ConsoleKey.RightArrow)
                {
                    Console.WriteLine("Send request set to fail");
                    game.SendLoginRequest("tim", "password", "hungry hippos");
                }
                if (key == ConsoleKey.LeftArrow)
                {
                    Console.WriteLine("Send request set to fail");
                    game.SendLoginRequest("mickey", "password", "hungry hippos 123");
                }
                if (key == ConsoleKey.E)
                {
                    Console.WriteLine("Drop DB");
                    game.SendLoginRequest("mickey", "password DROP TABLE users;", "hungry hippos 123");
                }
                if (key == ConsoleKey.S)
                {
                    Console.WriteLine("Special characters");
                    game.SendLoginRequest("mickey", "password", "'%s' '\n' p @pass #1 pass");
                }
                if (key == ConsoleKey.C)
                {
                    Console.WriteLine("Profile");
                    game.SendLoginRequest("chris", "password", "hungry hippos");
                }
                if (key == ConsoleKey.I)
                {
                    Console.WriteLine("Create character");
                    var state = new PlayerSaveStateData();
                    state.state = "{}";
                    game.SendCreateCharacter(2, "hungry hippos", "TestChar", state);
                }
                if (key == ConsoleKey.U)
                {
                    Console.WriteLine("Update character");
                    var state = new PlayerSaveStateData();
                    state.state = "{\"test\":\"data\"}";
                    game.SendUpdateCharacter(7, state);
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
                    //test.state = "{\"test\":\"data\"}";
                    game.Send(test);
                }
            } while (key != ConsoleKey.Escape);
            game.Disconnect();
        }
    }
}
