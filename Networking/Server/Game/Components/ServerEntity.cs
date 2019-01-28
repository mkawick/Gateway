using Packets;
using Server;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ServerEntity : MonoBehaviour
{
    public int EntityId { get; protected set; }
    public bool IsDirty { get; set; }

    protected IServerNetworking Server { get; private set; }

    public Action<IServerNetworking, ServerPlayer, int> OnSendState;

    public void SendState(ServerPlayer destPlayer, int frameIdLastUpdated)
    {
        OnSendState?.Invoke(Server, destPlayer, frameIdLastUpdated);
    }

    virtual public void Update()
    {
    }

    protected virtual void Start()
    {
    }

    /// <summary>
    /// Replacement constructor.
    /// You MUST call base.Init when overriding.
    /// </summary>
    public virtual void Init(int entityId, IServerNetworking unityServer)
    {
        this.EntityId = entityId;
        this.Server = unityServer;

        // HACK: Remove this when we refactor everything to use states
        OnSendState += (server, destPlayer, frameIdLastUpdated) =>
        {
            if (frameIdLastUpdated == NetworkConstants.BeforeStartOfGameFrameId)
            {
                SendInitData(destPlayer);
            }
        };
    }

    public virtual void SendInitData(ServerPlayer destPlayer, EntityFullPacket packet = null)
    {
        if (packet == null)
        {
            packet = new EntityFullPacket();
        }
        packet.entityId = EntityId;

        Server.Send(packet, destPlayer.EntityId.SingleItemAsEnumerable());
    }

    public virtual void SendDestroyMessage(ServerPlayer destPlayer)
    {
        var packet = new EntityDestroyPacket();
        packet.entityId = EntityId;

        Server.Send(packet, destPlayer.EntityId.SingleItemAsEnumerable());
    }

    /// <summary>
    /// Main network tick.
    /// </summary>
    public abstract void Tick();
}
