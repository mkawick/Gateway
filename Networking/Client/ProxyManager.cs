using Packets;
using UnityEngine;

public class ProxyManager
{
    private IClientNetworking client;
    private WorldEntityManager worldEntityManager;
    private ClientSettings clientSettings;

    public ProxyManager(IClientNetworking client, WorldEntityManager worldEntityManager)
    {
        this.client = client;
        this.worldEntityManager = worldEntityManager;

        clientSettings = Resources.Load<ClientSettings>("ClientSettings");

        client.AddListener<PlayerFullPacket>(OnPlayerFullPacket);
    }

    private void OnPlayerFullPacket(PlayerFullPacket packet)
    {
        var go = GameObject.Instantiate(clientSettings.proxyPrefab, packet.position.Get(), Quaternion.Euler(packet.rotation.Get()));
        var playerObj = go.GetComponent<ProxyPlayer>();
        if (playerObj == null)
        {
            Debug.LogWarning("Unable to find ProxyPlayer component on proxy player prefab.  Adding.");
            playerObj = go.AddComponent<ProxyPlayer>();
        }
        playerObj.Init(client, packet.entityId);
        worldEntityManager.AddWorldEntity(playerObj);
    }
}
