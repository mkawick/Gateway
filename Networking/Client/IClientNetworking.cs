using Packets;
using System;

public class LoginResponse
{
    public bool Success { get; private set; }
    public LoginResponse(bool success)
    {
        this.Success = success;
    }
}

public interface IClientNetworking : IPacketSource, IFrameTicker
{
    /// <summary>
    /// Attempt to connect to the gateway.
    /// </summary>
    void Connect();

    /// <summary>
    /// Fired once we're connected.
    /// Is fired by an internal thread.
    /// </summary>
    event Action OnConnect;

    /// <summary>
    /// Returns true if we're connected
    /// </summary>
    bool IsConnected();

    /// <summary>
    /// Disconnect from the gateway.
    /// </summary>
    void Disconnect();

    /// <summary>
    /// Fired once we've been disconnected, with the parameter indicating if we
    /// will attempt to reconnect.
    /// Is fired by an internal thread.
    /// </summary>
    event Action<bool> OnDisconnect;

    /// <summary>
    /// Sends the given login credentials.
    /// Requires us to be connected to the gateway, and for any previous
    /// SentLogin requests to have received a login response.
    /// </summary>
    /// <param name="username"></param>
    /// <param name="password"></param>
    void SendLogin(string username, string password);
    
    /// <summary>
    /// Fired when we receive a response to a login request.
    /// Is fired by an internal thread.
    /// </summary>
    event Action<LoginResponse> OnLoginResponse;

    bool IsLoggedIn { get; }

    void Send(BasePacket packet);

    /// <summary>
    /// Call this frequently to process received packets.
    /// Will trigger any listeners registered via IPacketSource, and will fire
    /// IFrameTicker.OnTick for every new frame since the last call.
    /// </summary>
    void ProcessReceivedPackets();
}
