using System.Collections.Generic;
using UnityEngine;
using System;
using CommonLibrary;
using Packets;
using Server;

//Initialises the networking layer and manages player objects
public class PlayerManager 
{
    GameObject playerPrefab;
    IServerNetworking networking;
    Dictionary<int, ServerPlayer> createdPlayers;
    string defaultSaveState;

    public PlayerManager(IServerNetworking unityServer)
    {
        createdPlayers = new Dictionary<int, ServerPlayer>();

        ServerSettings settings = Resources.Load<ServerSettings>("ServerSettings");

        defaultSaveState = settings.saveState.text;
        playerPrefab = settings.playerPrefab;

        networking = unityServer;
        networking.OnClientConnected += NetworkInterface_OnClientConnected;
        networking.OnClientDisconnected += GameServer_OnClientDisconnected;
    }

    public void Tick()
    {
        TickAllPlayers();
    }

    private void GameServer_OnClientDisconnected(int entityID)
    {
        MainThreadQueuer.Instance.AddMessage(() =>
        {
            GameObject.Destroy(createdPlayers[entityID].gameObject);
            createdPlayers.Remove(entityID);
        });
    }

    private void NetworkInterface_OnClientConnected(int entityID, PlayerSaveState saveState)
    {
        MainThreadQueuer.Instance.AddMessage(() =>
        {
            var go = GameObject.Instantiate(playerPrefab);

            var playerComp = go.GetComponent<ServerPlayer>();

            //Init the player to itself and others
            playerComp.Init(entityID, networking);
            playerComp.SendPlayerSaveState(defaultSaveState, saveState);
            playerComp.SendInitData(playerComp);

            Debug.Log("Player connected: " + playerComp.EntityId);

            createdPlayers.Add(playerComp.EntityId, playerComp);
        });
    }

    public void SendWorldStateToPlayers()
    {
#if DEBUG_SENDING_WORLD_STATE
        Stopwatch sw = new Stopwatch();
        sw.Start();
#endif
        foreach (var player in createdPlayers)
        {
            player.Value.UpdateNearbyEntities();
        }
#if DEBUG_SENDING_WORLD_STATE
        sw.Stop();
        Console.WriteLine("Sending world state took {0} ms", sw.ElapsedMilliseconds);
#endif
        // We've sent everything so clear the flags
        ClearPlayerDirtyFlags();
    }

    void ClearPlayerDirtyFlags()
    {
        foreach (var player in createdPlayers)
        {
            player.Value.IsDirty = false;
        }
    }

    void TickAllPlayers()
    {
        if (createdPlayers.Count > 0)
        {
            foreach (var entry in createdPlayers)
            {
                entry.Value.Tick();
            }
        }
    }


}