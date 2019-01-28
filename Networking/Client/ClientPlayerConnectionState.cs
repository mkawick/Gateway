using CommonLibrary;
using Packets;
using System;
using System.Collections.Generic;

namespace Client
{
    public class ClientPlayerConnectionState
    {
        public int ApplicationId { get; private set; }

        private IPacketSend socket;
        private bool hasSentCredentials = false;
        
        public bool IsLoggedIn { get; private set; }

        int numPacketsReceived = 0;
        bool isBoundToGateway = false;
        
        //----------------------------------
        
        public event Action OnConnect;
        public event Action<bool> OnDisconnect;
        public event Action<LoginResponse> OnLoginResponse;

        private List<BasePacket> unprocessedPackets;

        public ClientPlayerConnectionState(IPacketSend socket, int applicationId)
        {
            ApplicationId = applicationId;
            IsLoggedIn = false;
            unprocessedPackets = new List<BasePacket>();

            this.socket = socket;
            socket.OnPacketsReceived += Socket_OnPacketsReceived;
            socket.OnConnect += Socket_OnConnect;
            socket.OnDisconnect += Socket_OnDisconnect;
        }

        private void Socket_OnConnect(IPacketSend socket)
        {
            // Tell the gateway who we are
            ClientIdPacket clientId = (ClientIdPacket)IntrepidSerialize.TakeFromPool(PacketType.ClientIdPacket);
            clientId.Id = ApplicationId;
            Send(clientId);
        }

        private void Socket_OnPacketsReceived(IPacketSend socket, Queue<BasePacket> receivedPackets)
        {
            numPacketsReceived += receivedPackets.Count;

            // Look for a server id packet if we've only just connected
            if (!isBoundToGateway)
            {
                var packet = receivedPackets.Dequeue();
                if (packet is ServerIdPacket)
                {
                    ServerIdPacket id = packet as ServerIdPacket;
                    if (id != null && id.Type == ServerIdPacket.ServerType.Gateway)
                    {
                        isBoundToGateway = true;
                        OnConnect?.Invoke();
                    }
                }
                else
                {
                    socket.Disconnect();
                    Console.Error.WriteLine("Unexpected packet type, disconnecting: {0}", packet.PacketType);
                    return;
                }
            }
            // Look for a login credentials valid packet if we've tried to log in
            else if (!IsLoggedIn && hasSentCredentials)
            {
                var packet = receivedPackets.Dequeue();
                if (packet is LoginCredentialValid)
                {
                    HandleLoginCredentialsValidPacket(packet as LoginCredentialValid);
                }
                else
                {
                    socket.Disconnect();
                    Console.Error.WriteLine("Unexpected packet type, disconnecting: {0}", packet.PacketType);
                    return;
                }
            }
            lock (unprocessedPackets)
            {
                // Store the other packets for retrieval later
                unprocessedPackets.AddRange(receivedPackets);
            }
        }

        private void HandleLoginCredentialsValidPacket(LoginCredentialValid packet)
        {
            IsLoggedIn = packet.isValid;
            if (IsLoggedIn)
            {
                // Tell the server that we're ready for moar data
                LoginClientReady temp = (LoginClientReady)IntrepidSerialize.TakeFromPool(PacketType.LoginClientReady);
                Send(temp);
                ClientGameInfoResponse cgir = (ClientGameInfoResponse)IntrepidSerialize.TakeFromPool(PacketType.ClientGameInfoResponse);
                cgir.GameId = ApplicationId;
                Send(cgir);
            }
            else
            {
                // Allows us to try to login again
                hasSentCredentials = false;
            }
            
            OnLoginResponse?.Invoke(new LoginResponse(packet.isValid));
        }

        /// <summary>
        /// Gets all the unprocessed packets, and clears the unprocessed packet list.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<BasePacket> TakeUnprocessedPackets()
        {
            IEnumerable<BasePacket> result;
            lock (unprocessedPackets)
            {
                result = unprocessedPackets;
                unprocessedPackets = new List<BasePacket>();
            }
            return result;
        }

        public void Login(string username, string password)
        {
            if (hasSentCredentials || IsLoggedIn)
                return;

            LoginCredentials loginCredentials = (LoginCredentials)IntrepidSerialize.TakeFromPool(PacketType.LoginCredentials);
            loginCredentials.playerName.Copy(username);
            loginCredentials.password.Copy(password);
            Send(loginCredentials);

            hasSentCredentials = true;
        }

        private void Socket_OnDisconnect(IPacketSend socket, bool attemptingReconnect)
        {
            // We're no longer connected, so we'll need to log in again
            hasSentCredentials = false;
            IsLoggedIn = false;
            OnDisconnect?.Invoke(attemptingReconnect);
        }

        //--------------------------------------------------------------------------------

        public bool IsConnected()
        {
            return socket.IsConnected;
        }
        
        public void ConnectClient()
        {
            socket.Connect();
        }

        public void EndClient()
        {
            socket.Disconnect();
        }
        
        public void Send(BasePacket packet)
        {
            socket.Send(packet);
        }
    }
}