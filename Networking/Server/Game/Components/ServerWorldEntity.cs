using Packets;
using Server;
using System;
using Vectors;
using UnityEngine;

public class ServerWorldEntity : ServerEntity
{
    protected Vectors.Vector3 position;
    protected Vectors.Vector3 rotation;

    public Action OnInitData;

    public virtual Vectors.Vector3 Position
    {
        get
        {
            return position;
        }

        set
        {
            { if (value != position) { IsDirty = true; position = value; } }
        }
    }

    public virtual Vectors.Vector3 Rotation
    {
        get
        {
            return rotation;
        }

        set
        {
            { if (value != rotation) { IsDirty = true; rotation = value; } }
        }
    }

    public override void Init(int entityId, IServerNetworking unityServer)
    {
        base.Init(entityId, unityServer);

        //HACK: Remove once we have everything in states
        OnSendState += (server, destPlayer, frameIdLastUpdated) =>
        {
            if (frameIdLastUpdated != NetworkConstants.BeforeStartOfGameFrameId
                && IsDirty)
            {
                SendPositionAndRotationData(destPlayer);
            }
        };
    }

    public virtual void SendPositionAndRotationData(ServerPlayer destPlayer)
    {
        WorldEntityPacket packet = (WorldEntityPacket)IntrepidSerialize.TakeFromPool(PacketType.WorldEntity);
        packet.entityId = EntityId;
        packet.position.Set(Position);
        packet.rotation.Set(Rotation);

        Server.Send(packet, destPlayer.EntityId.SingleItemAsEnumerable());
    }

    public override void SendInitData(ServerPlayer destPlayer, EntityFullPacket packet = null)
    {
        if (packet == null)
        {
            packet = (EntityFullPacket)IntrepidSerialize.TakeFromPool(PacketType.EntityFull);
        }
        packet.position.Set(Position);
        packet.rotation.Set(Rotation);
        base.SendInitData(destPlayer, packet);
        OnInitData?.Invoke();
    }

    public override void Tick()
    {
    }
}
