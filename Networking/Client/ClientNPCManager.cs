using Packets;
using UnityEngine;

/// <summary>
/// Creates new NPCs on receipt of an NPCFullPacket, and adds them to the WorldEntityManager
/// </summary>
public class ClientNPCManager
{
    private IClientNetworking client;
    private WorldEntityManager worldEntityManager;

    public ClientNPCManager(IClientNetworking client, WorldEntityManager worldEntityManager)
    {
        this.client = client;
        this.worldEntityManager = worldEntityManager;
        client.AddListener<NPCFullPacket>(OnNPCFullPacket);
    }

    private void OnNPCFullPacket(NPCFullPacket packet)
    {
        var agentData = DataLookup.GetData<NPCAgentData>(packet.AgentID);
        var configData = DataLookup.GetData<NPCConfigData>(packet.ConfigID);
        if (agentData == null)
        {
            Debug.LogErrorFormat("Unable to spawn NPC as we can't find the agentData from id: {0}", packet.AgentID);
            return;
        }

        var go = GameObject.Instantiate(agentData.clientPrefab, packet.position.Get(), Quaternion.Euler(packet.rotation.Get()));

        if (configData != null)
        {
            var configurator = go.GetComponent<NPCConfigurator>();
            if (configurator != null)
            {
                configurator.Configure(agentData, configData);
            }
            else
            {
                Debug.LogWarningFormat("Unable to find NPCConfigurator component on {0}", agentData.name);
            }
        }
        else
        {
            Debug.LogErrorFormat("Unable to find config data, id: {0} on {1}", packet.ConfigID, agentData.name);
        }
        
        var playerObj = go.GetComponent<ClientNPC>();
        if (playerObj == null)
        {
            playerObj = go.AddComponent<ClientNPC>();
            Debug.LogWarningFormat("Adding ClientNPC component to {0}", agentData.name);
        }
        playerObj.Init(client, packet.entityId);
        worldEntityManager.AddWorldEntity(playerObj);
    }
}
