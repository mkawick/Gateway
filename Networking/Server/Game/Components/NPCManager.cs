using Server;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Manages network updates for NPC's on the server.
//Thread safe, so the allNPC and players lists can be accessed freely inside Tick())
public class NPCManager
{
    IServerNetworking networking;

    private List<ServerNPC> allNPCs;
    //Temporary store to prevent locking on allNPCs
    private List<ServerNPC> newNPCs;
    private object newNPCLock;

    public NPCManager(IServerNetworking unityServer)
    {
        allNPCs = new List<ServerNPC>();
        newNPCs = new List<ServerNPC>();
        newNPCLock = new object();
        this.networking = unityServer;
    }

    public void Tick()
    {
        SetupNewNPCs();
    }

    void SetupNewNPCs()
    {
        lock (newNPCLock)
        {
            if (newNPCs.Count > 0)
            {
                foreach(var npc in newNPCs)
                {
                    allNPCs.Add(npc);
                }
                newNPCs.Clear();
            }
        }
    }

    public void ClearDirtyFlags()
    {
        foreach (var npc in allNPCs)
        {
            npc.IsDirty = false;
        }
    }

    public void RegisterNPC(ServerNPC npc)
    {
        npc.Init(networking.GetNextEntityID(), networking);
        lock (newNPCLock)
        {
            newNPCs.Add(npc);
        }
    }
}