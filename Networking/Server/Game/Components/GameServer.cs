using CommonLibrary;
using Server;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameServer : MonoBehaviour
{
    #region Singleton
    private static GameServer instance;
    public static GameServer Instance
    {
        get
        {
            if (instance == null)
            {
                var instances = FindObjectsOfType<GameServer>();
                if (instances.Length > 0)
                {
                    instance = instances[0];
                    if (instances.Length > 1)
                    {
                        for (int i = 1; i < instances.Length; i++)
                            Destroy(instances[i]);
                    }
                }
                else
                {
                    instance = new GameObject("GameServer (Singleton)").AddComponent<GameServer>();
                }
            }
            return instance;
        }
    }
    #endregion

    [SerializeField]
    [Tooltip("Id for the server - leave as -1 to use an IP-based hash")]
    private int applicationId = -1;

    [SerializeField]
    private bool connectToProfileServer = false;
    [SerializeField]
    private SocketWrapperSettings profileServerSettings
        = new SocketWrapperSettings(NetworkConstants.defaultServerIp, 11002);

    [SerializeField]
    private SocketWrapperSettings gatewayServerSettings
        = new SocketWrapperSettings(
            NetworkConstants.defaultServerIp,
            NetworkConstants.defaultGatewayToServerPort);

    public IServerNetworking NetworkInterface { get; private set; }

    public Action OnTick;

    public NPCManager NPCManager { get; private set; }
    public PlayerManager PlayerManager { get; private set; }


    private void Awake()
    {
        Application.targetFrameRate = 60;

        ConsoleWriteRedirecter.Redirect();

        if (applicationId == -1)
        {
            applicationId = Network.Utils.GetIPBasedApplicationId();
        }


        NetworkInterface = new ServerNetworking(connectToProfileServer ? profileServerSettings : null, gatewayServerSettings, applicationId);
        NetworkInterface.OnTick += () => { MainThreadQueuer.Instance.AddMessage(FireOnTick); };
        NPCManager = new NPCManager(NetworkInterface);
        PlayerManager = new PlayerManager(NetworkInterface);

        NetworkInterface.StartService();
    }

    private void FireOnTick()
    {
        OnTick?.Invoke();
        NPCManager.Tick();
        PlayerManager.Tick();

        PlayerManager.SendWorldStateToPlayers();
    }

    private void OnDrawGizmos()
    {
        SpatialPartitioning.DrawOctree();
    }

    void OnApplicationQuit()
    {
        NetworkInterface.StopService();
    }
}
