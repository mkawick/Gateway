using CommonLibrary;
using Packets;
using System;
using System.Diagnostics;
using System.Threading;
using Vectors;

namespace HeadlessClient01
{
    internal class MyPlayer
    {
        public int entityId;
        public Vector3 position;
        public Vector3 rotation;
        private TalkingToGatewayController testClient;

        public void Set(TalkingToGatewayController test)
        {
            testClient = test;
        }
        public void SendRandomLocation(System.Random rand, bool setToOrigin)
        {
            int range = 50;
            int x = rand.Next(-range, range);
            int y = 1;// rand.Next(-range, range);
            int z = rand.Next(-range, range);
            if (setToOrigin == true)
            {
                x = y = z = 0;
            }

            position = new Vector3(x, y, z);
            float phi = (float)(rand.NextDouble() * 360);
            rotation = new Vector3(0, phi, 0);
            SendPositionInfo();
        }

        public void SendPositionInfo()
        {
            WorldEntityPacket wep = (WorldEntityPacket)IntrepidSerialize.TakeFromPool(PacketType.WorldEntity);
            wep.rotation.Set(rotation);
            wep.position.Set(position);
            wep.entityId = entityId;
            testClient.Send(wep);
        }

    }

    internal class TestClientToGatewayMain
    {
        private static void Main(string[] args)
        {
            CommonLibrary.Parser.ParseCommandLine(args);
            long applicationId = CommonLibrary.Parser.ApplicationId;
            if (applicationId == 0)
            {
                applicationId = Network.Utils.GetIPBasedApplicationId();
            }
            float sleepTime = 1000.0f / (float)CommonLibrary.Parser.FPS;
            string ipAddr = CommonLibrary.Parser.ipAddr;

            Console.WriteLine("Client talking to Gateway.");
            Console.WriteLine("  ** application id = {0} **", applicationId);
            Console.WriteLine("  Press esc to update player position.\n\n");

            ushort port = 11000;
            bool setToOrigin = false;

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            MyPlayer player = new MyPlayer();
            Random rand = new Random();
            TalkingToGatewayController testClient = new TalkingToGatewayController(ipAddr, port, player, applicationId);
            player.Set(testClient);

            while (testClient.isLoggedIn == false)
            {
                Thread.Sleep(16);
                TimeSpan ts = stopWatch.Elapsed;

                if (ts.Seconds > 180)
                {
                    Console.WriteLine("unable to connect after 3 mins");
                    Console.WriteLine("press a key to exit");
                    while (!Console.KeyAvailable)
                    {
                        // Do something
                    }
                    Environment.Exit(0);
                }
            }

            player.SendRandomLocation(rand, setToOrigin);

            stopWatch.Start();
            ConsoleKey key = ConsoleKey.Backspace;

            ScriptItem script = new ScriptItem();
            int loopCount = 1;
            do
            {
                if (script.isComplete)
                {
                    script.Randomize(player, rand);
                }
                script.Act();
                Thread.Sleep((int)sleepTime);
                if (Console.KeyAvailable)
                {
                    key = Console.ReadKey(true).Key;
                }



                loopCount++;
            } while (key != ConsoleKey.Escape);

            testClient.Close();
        }
    }

    internal class ScriptItem
    {
        private int timeStampInMs;
        private int waitTimeInMs;
        private Vector3 startRotation, destRotation;

        private enum Type
        {
            MoveTo, Wait, Rotate
        }

        private Type scriptType;
        private Vector3 startPostion, destination;
        private MyPlayer player;
        public bool isComplete = true;

        public void Randomize(MyPlayer _player, Random rand)
        {
            timeStampInMs = (int)(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
            waitTimeInMs = rand.Next(1000, 3000);
            int enumMemberCount = Enum.GetNames(typeof(Type)).Length;
            int whichType = rand.Next(enumMemberCount);
            scriptType = (Type)whichType;

            player = _player;
            destination = player.position;
            startPostion = destination;
            float dist = (float)(rand.Next(5, 15));
            float angle = (float)(rand.NextDouble() * 360);

            destination = new Vector3(
                player.position.x + (float)Math.Cos(angle) * dist,
                player.position.y,
                player.position.z + (float)Math.Sin(angle) * dist);

            startRotation = player.rotation;
            int rotationRange = 100;
            destRotation = new Vector3(startRotation.x, startRotation.y + rand.Next(rotationRange) - rotationRange / 2, startRotation.z);

            isComplete = false;
        }

        public void Act()
        {
            switch (scriptType)
            {
                case Type.MoveTo:
                    {
                        Vector3 toTarget = destination - player.position;
                        float distSquared = Vector3.DistanceSquared(destination, player.position);
                        if (distSquared > 1)
                        {
                            int timeDiff = (int)(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - timeStampInMs;
                            if (timeDiff <= 0)
                            {
                                //isComplete = true;
                                break;
                            }
                            float percentageComplete = (float)timeDiff / (float)waitTimeInMs;
                            if (percentageComplete >= 1)
                            {
                                isComplete = true;
                                break;
                            }

                            toTarget.Normalize();
                            Vector3 newPosition = startPostion + (toTarget * percentageComplete);
                            player.position = newPosition;
                            player.rotation = toTarget;
                            player.SendPositionInfo();
                        }
                        else //(true)
                        {
                            isComplete = true;
                        }
                    }
                    break;
                case Type.Wait:
                    {
                        int timeDiff = (int)(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - timeStampInMs;

                        if (timeDiff > waitTimeInMs)
                        {
                            isComplete = true;
                        }
                    }
                    break;
                case Type.Rotate:
                    {
                        float angleDiff = destRotation.y - player.rotation.y;
                        if (Math.Abs(angleDiff) < 1)
                        {
                            player.rotation = destRotation;
                            isComplete = true;
                        }
                        else
                        {
                            int timeDiff = (int)(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - timeStampInMs;
                            if (timeDiff <= 0)
                            {
                                //isComplete = true;
                                break;
                            }
                            float percentageComplete = (float)timeDiff / (float)waitTimeInMs;
                            if (percentageComplete >= 1)
                            {
                                isComplete = true;
                                break;
                            }
                            Vector3 newRotation = startRotation;
                            newRotation.y = (angleDiff * percentageComplete);
                            player.rotation = newRotation;
                            player.SendPositionInfo();
                        }
                    }
                    break;
            }
        }
    }
}


