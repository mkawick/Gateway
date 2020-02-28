using CommonLibrary;
using Packets;
using System;
using System.Collections.Generic;
using static Network.Utils;

namespace Test_Direct_ClientToServer
{
    class ClientController : INeedsExternalUpdate
    {
        Int64 applicationId;
        int numPacketsReceived = 0;
        bool isBoundToGateway = false;
        int entityId = 0;
        DatablobAccumulator accumulator = new DatablobAccumulator();

        Queue<BasePacket> receivedPackets = new Queue<BasePacket>();

        public event Action<byte[], int> OnImageReceived;

        public bool isLoggedIn = false;
        SocketWrapper socket;

        public ClientController(string serverRemoteAddr, ushort serverPort, Int64 appId = 15)
        {
            socket = new SocketWrapper(serverRemoteAddr, 11000);

            applicationId = appId;
            socket.OnPacketsReceived += Sock_OnPacketsReceived;
            socket.OnConnect += Sock_OnConnect;
            socket.OnDisconnect += Sock_OnDisconnect;
            socket.Connect();
        }

        public void Close()
        {
            if (socket != null && socket.IsConnected == true)
            {
                socket.Disconnect();
                socket = null;
            }
        }

        private void Sock_OnConnect(IPacketSend sender)
        {
            if (socket != sender)
                return;

            ClientIdPacket clientId = (ClientIdPacket)IntrepidSerialize.TakeFromPool(PacketType.ClientIdPacket);
            clientId.Id = (int) applicationId;
            sender.Send(clientId);
        }
        public void Sock_OnDisconnect(IPacketSend sender, bool willRetry)
        {
            socket.OnConnect -= Sock_OnConnect;
            socket.OnDisconnect -= Sock_OnDisconnect;
            socket.OnConnect -= Sock_OnConnect;
            socket = null;
        }

        public void Update()
        {
            Queue<BasePacket> workingPackets;
            lock (receivedPackets)
            {
                workingPackets = receivedPackets;
                receivedPackets = new Queue<BasePacket>();
            }

            HandleNormalPackets(workingPackets);
        }
        void HandleBlobData(DataBlob packet)
        {
            if (accumulator.Add(packet as DataBlob) == true)
            {
                int sizeOfBlobs = accumulator.GetSizeOfAllBlobs();
                if (accumulator.BlobCount == 307)
                {
                    Console.Write("looking for extra processes\n");
                }
                if (sizeOfBlobs!= 3686400 || accumulator.BlobCount == 307)
                {
                    Console.Write("wtf\n");
                }
                int numBlobs = accumulator.BlobCount;


                byte[] bytes = accumulator.ConvertDatablobsIntoRawData();

                int len = bytes.Length;
                if(len != sizeOfBlobs || len != 3686400)
                {
                    Console.Write("wtf\n");
                }
                OnImageReceived?.Invoke(bytes, bytes.Length);
                    
                accumulator.Clear();
                Console.Write("Blobs received in acc {0}\n", numBlobs);
                Console.Write("Bytes received in blob {0}\n", len);
            }
            return;
        }
        void HandleNormalPackets(Queue<BasePacket> listOfPackets)
        {
            foreach (var packet in listOfPackets)
            {
                numPacketsReceived++;
                // normal processing
                EntityPacket ep = packet as EntityPacket;
                if (ep != null)
                {
                    //entityId = ep.entityId;
                    int tempEntityId = ep.entityId;
                    if (entityId == tempEntityId)
                    {
                        Console.Write("This entity packet updated {0}\n", tempEntityId);
                    }
                    else
                    {
                        Console.Write("Entity update packet {0}\n", tempEntityId);
                    }
                    continue;
                }
                KeepAlive ka = packet as KeepAlive;
                if (ka != null)
                {
                    KeepAliveResponse kar = (KeepAliveResponse)IntrepidSerialize.TakeFromPool(PacketType.KeepAliveResponse);
                    socket.Send(kar);
                }

                if (packet is ServerPingHopperPacket)
                {
                    ServerPingHopperPacket hopper = packet as ServerPingHopperPacket;
                    hopper.Stamp("client end");
                    hopper.PrintList();
                }
                if (packet.PacketType == PacketType.DataBlob)
                {
                    HandleBlobData(packet as DataBlob);
                }
                IntrepidSerialize.ReturnToPool(packet);
            }
        }
        private void Sock_OnPacketsReceived(IPacketSend arg1, Queue<BasePacket> listOfPackets)
        {
            // all of these boolean checks should be replaced by a Strategy
            if (isBoundToGateway == true)
            {
                if (isLoggedIn == true)
                {
                    //HandleNormalPackets(listOfPackets);
                    lock(receivedPackets)
                    {
                        foreach (var packet in listOfPackets)
                        {
                            receivedPackets.Enqueue(packet);
                        }
                    }
                }
                else
                {
                    foreach (var packet in listOfPackets)
                    {
                        LoginCredentialValid lcr = packet as LoginCredentialValid;
                        if (lcr != null)
                        {
                            LoginClientReady temp = (LoginClientReady)IntrepidSerialize.TakeFromPool(PacketType.LoginClientReady);
                            Send(temp);

                            ClientGameInfoResponse cgir = (ClientGameInfoResponse)IntrepidSerialize.TakeFromPool(PacketType.ClientGameInfoResponse);
                            cgir.GameId = (int) applicationId;
                            Send(cgir);

                            isLoggedIn = lcr.isValid;
                        }
                        if (entityId == 0)// until we are assigned an entity id, we can't do much
                        {
                            EntityPacket ep = packet as EntityPacket;
                            if (ep != null)
                            {
                                entityId = ep.entityId;
                            }
                        }
                        numPacketsReceived++;
                    }
                }
            }
            else
            {
                foreach (var packet in listOfPackets)
                {
                    numPacketsReceived++;
                    if (packet is ServerIdPacket)
                    {
                        ServerIdPacket id = packet as ServerIdPacket;
                        if (id != null && id.Type == ServerIdPacket.ServerType.Gateway)
                        {
                            isBoundToGateway = true;
                            break;
                        }
                    }
                }
            }
        }

        public void Send(BasePacket bp)
        {
            socket.Send(bp);
        }
    }
}
