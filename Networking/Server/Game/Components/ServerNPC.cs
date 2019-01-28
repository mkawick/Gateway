using System;
using Ego;
using UnityEngine;
using Packets;
using System.Collections.Generic;
using System.IO;
using System.Linq;


public class ServerNPC : ServerWorldEntity
{
    public ServerActor actor;
    private int agentID;
    private int configID;

    List<IBBVariable> dirtyVars = new List<IBBVariable>();
    NPC_BTState previousState = null;

    private void Awake()
    {
        var configurator = GetComponent<NPCConfigurator>();
        agentID = configurator.agentData.npcID;
        configID = configurator.configData.id;
        if (actor)
        {
            actor.OnBehaviorChange += BehaviorChanged;
            actor.OnMemoryChange += MemoryChanged;
        }
    }

    protected override void Start()
    {
        base.Start();
            
        OnInitData += OnSendingInitData;
        Position = transform.position;
        Rotation = transform.rotation.eulerAngles;
        RegisterNPC();
        SpatialPartitioning.Register(this);
    }

    private void MemoryChanged(IBBVariable variable)
    {
        if (dirtyVars.Contains(variable))
            return;

        dirtyVars.Add(variable);
    }

    private void BehaviorChanged(Behavior behavior)
    {
        ServerPlayer[] playerInRange = SpatialPartitioning.GetEntitiesInRadius<ServerPlayer>(transform.position);

        SendDirtyMemoryVar(playerInRange);
        SendChangeBehavior(playerInRange, behavior);
    }

    private void OnSendingInitData()
    {
        if (previousState != null)
        {
            ServerPlayer[] playerInRange = SpatialPartitioning.GetEntitiesInRadius<ServerPlayer>(transform.position);
            Server.Send(previousState, playerInRange.Select(e => e.EntityId));
        }
    }

    private void SendChangeBehavior(ServerPlayer[] playerInRange, Behavior behavior)
    {
        if (playerInRange.Length > 0)
        {
            NPC_BTState packet = IntrepidSerialize.TakeFromPool(PacketType.NPC_BTState) as NPC_BTState;
            packet.guid.Copy(behavior.id);

            if (packet == null)
                return;

            packet.entityId = EntityId;

            previousState = (NPC_BTState)IntrepidSerialize.ReplicatePacket(packet);

            Server.Send(packet, playerInRange.Select(e => e.EntityId));
        }
    }

    private void SendDirtyMemoryVar(ServerPlayer[] playerInRange)
    {
        if (playerInRange.Length > 0)
        {
            NPC_BlackBoard packet = IntrepidSerialize.TakeFromPool(PacketType.NPC_BlackBoard) as NPC_BlackBoard;

            MemoryStream memoryStream = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(memoryStream);

            bw.Write(dirtyVars.Count);


            for (int i = 0; i < dirtyVars.Count; i++)
            {
                BBWriterReader.Write(dirtyVars[i], bw);
            }

            packet.bbDelta = memoryStream.ToArray();

            packet.entityId = EntityId;

            Server.Send(packet, playerInRange.Select(e => e.EntityId));

            dirtyVars.Clear();
        }
    }
    public override void SendInitData(ServerPlayer destPlayer, EntityFullPacket packet = null)
    {
        NPCFullPacket npcPacket = packet as NPCFullPacket;
        if (npcPacket == null)
        {
            npcPacket = (NPCFullPacket)IntrepidSerialize.TakeFromPool(PacketType.NPCFull);
        }
        npcPacket.AgentID = agentID;
        npcPacket.ConfigID = configID;

        base.SendInitData(destPlayer, npcPacket);
    }

    public override void Tick()
    {
        Position = transform.position;
        Rotation = transform.rotation.eulerAngles;
        SpatialPartitioning.Update(this);
    }

    private void RegisterNPC()
    {
        if (GameServer.Instance.NPCManager != null)
        {
            GameServer.Instance.NPCManager.RegisterNPC(this);
        }
    }
}