using UnityEngine;
using Client.Game;
using CommonLibrary;
using Packets;
using System;

public class GameClient : MonoBehaviour
{
    public static GameClient Instance { get; private set; }
    
    [SerializeField]
    [Tooltip("Id of the server to connect to - leave as -1 to use an IP-based hash")]
    private int applicationId = -1;

    [SerializeField]
    private SocketWrapperSettings socketSettings
        = new SocketWrapperSettings(
            NetworkConstants.defaultServerIp,
            NetworkConstants.defaultGatewayToClientPort);

    [SerializeField]
    private bool autoLogin = true;

    [SerializeField]
    private string username = "mickey";

    [SerializeField]
    private string password = "password";

    public IClientNetworking Client { get; private set; }

    public ClientPlayer Player { get; private set; }
    public bool IsLoggedIn { get { return Client.IsLoggedIn; } }

    // Fired when the player has been loaded
    public event Action OnPlayerLoaded;

    private WorldEntityManager worldEntityManager;
#pragma warning disable 414
    private ProxyManager proxyManager;
    private ClientNPCManager npcManager;
#pragma warning restore 414

    private long onPlayerFullPacketListenerHandle;

    // Use this for initialization
    void Awake ()
    {
        // We are a singleton
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
        }

        if (applicationId == -1)
        {
            applicationId = Network.Utils.GetIPBasedApplicationId();
        }

        Client = new ClientNetworking(socketSettings, applicationId);
        Client.OnConnect += OnConnect;
        onPlayerFullPacketListenerHandle = Client.AddListener<PlayerFullPacket>(OnPlayerFullPacket);

        worldEntityManager = new WorldEntityManager(Client);
        npcManager = new ClientNPCManager(Client, worldEntityManager);
        // don't set proxy manager up yet, as we want to wait for first player packet
        // as that is for *us* rather than proxy players

        Client.Connect();
    }

    private void OnConnect()
    {
        // Try auto-login if requested
        if (autoLogin)
        {
            Client.SendLogin(username, password);
        }
    }

    private void OnPlayerFullPacket(PlayerFullPacket packet)
    {
        // We don't spawn the player here immediately, as we want the start screen
        // to ask the user to press space first.

        // All subsequent packets are proxy players, so create the proxy manager
        // to deal with them
        proxyManager = new ProxyManager(Client, worldEntityManager);

        // We don't want this to be called any more
        Client.RemoveListener<PlayerFullPacket>(onPlayerFullPacketListenerHandle);

        OnPlayerLoaded?.Invoke();
    }

    public void SpawnPlayer(PlayerFullPacket packet)
    {
        if (Player != null)
        {
            Debug.LogWarning("SpawnPlayer called but we already have a player");
            return;
        }

        // The first player full packet we receive is for us, after that
        // it's for proxy players.  This is the first one.
        var clientSettings = Resources.Load<ClientSettings>("ClientSettings");
        var go = Instantiate(clientSettings.playerPrefab, packet.position.Get(), Quaternion.Euler(packet.rotation.Get()));
        var clientPlayer = go.GetComponent<ClientPlayer>();
        if (clientPlayer == null)
        {
            clientPlayer = go.AddComponent<ClientPlayer>();
            Debug.LogWarning("Didn't find ClientPlayer component on player prefab, adding..");
        }
        clientPlayer.Init(Client, packet.entityId);
        worldEntityManager.AddWorldEntity(clientPlayer);
        Player = clientPlayer;
    }

    void Update ()
    {
        // May cause packet handlers and the OnTick to fire
        Client.ProcessReceivedPackets();
    }

    void OnApplicationQuit()
    {
        // We can't guarantee the order in which OnApplicationQuit will be called, so make sure
        // that we save the game state (if requested) before we shut down the socket.
        // GameState may have done this already, but it shouldn't trigger another save.
        GameState.Instance.AttemptToSaveOnExit();
        Client.Disconnect();
    }
}
