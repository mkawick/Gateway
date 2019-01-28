using CommonLibrary;
using Packets;
using PacketTypes;
using System;
using System.Collections.Generic;

namespace Test_client_login
{
    class TestLoginController
    {
        public string playerName = "mickey";
        public string password = "password";
        
        SocketWrapper socket;
        
        public TestLoginController(string ipAddr, ushort port)
        {
            socket = new SocketWrapper(ipAddr, port);
            socket.OnPacketsReceived += ProcessReceiveBuffer;
            socket.Connect();
        }

        public void SendLoginRequest(string username, string password, string productname)
        {
            Console.WriteLine("send request: username: {0}\npassword: {1}\nproductname: {2}\n", username, password, productname);
            UserAccountRequest loginCredentials = (UserAccountRequest)IntrepidSerialize.TakeFromPool(PacketType.UserAccountRequest);
            loginCredentials.password.Copy(password);
            loginCredentials.username.Copy(username);
            loginCredentials.connectionId = 0;
            loginCredentials.product_name.Copy(productname);
            socket.Send(loginCredentials);
        }

        //UserAccountRequest
        public bool ReceiveLogin(UserAccountResponse response)
        {
            Console.Write("response id: {0}\nis valid account: {1}\n state: {2}\n", response.connectionId, response.isValidAccount, response.state);
            return true;
        }

        public void SendCreateCharacter(int accountId, string productName, string characterName, PlayerSaveStateData state)
        {
            Console.WriteLine("send create char: accountId: {0}, productName: {1}, charName: {2}, state: {3}", accountId, productName, characterName, state);
            ProfileCreateCharacterRequest packet = (ProfileCreateCharacterRequest)IntrepidSerialize.TakeFromPool(PacketType.ProfileCreateCharacterRequest);
            packet.accountId = accountId;
            packet.productName.Copy(productName);
            packet.characterName.Copy(characterName);
            packet.state = state;
            socket.Send(packet);
        }

        public void SendUpdateCharacter(int characterId, PlayerSaveStateData state)
        {
            Console.WriteLine("send update char: characterId: {0}, state: {1}", characterId, state);
            ProfileUpdateCharacter packet = (ProfileUpdateCharacter)IntrepidSerialize.TakeFromPool(PacketType.ProfileUpdateCharacter);
            packet.characterId = characterId;
            packet.state = state;
            socket.Send(packet);
        }

        public void Send(BasePacket packet)
        {
            Console.WriteLine("send packet: type: {0}", packet.PacketType);
            socket.Send(packet);
        }

        public void ProcessReceiveBuffer(IPacketSend socket, Queue<BasePacket> deserializedPackets)
        {
            if (deserializedPackets.Count < 1)
                return;

            while (deserializedPackets.Count > 0)
            {
                BasePacket packet = deserializedPackets.Dequeue();
                PacketType packetType = packet.PacketType;
                Console.WriteLine("packetType: {0}\n", packetType);

                if(packetType == PacketType.UserAccountResponse)
                {
                    ReceiveLogin(packet as UserAccountResponse);
                }
            }
            return;
        }

        public void Disconnect()
        {
            socket.Disconnect();
        }
    }
}
