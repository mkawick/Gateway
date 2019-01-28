using Packets;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

struct PositionalTracker
{
    const float distSquaredThreshold = 0.05f;
    const float rotationThreshold = 0.8f;
    Vector3 lastSendPosition;
    Quaternion lastSendRotation;

    public bool Passes(Vector3 position, Quaternion rotation)
    {
        float diffMagnitude = (position - lastSendPosition).sqrMagnitude;
        float diffRotation = Quaternion.Angle(rotation, lastSendRotation);

        if (diffMagnitude > distSquaredThreshold || diffRotation > rotationThreshold)
        {
            return true;
        }
        return false;
    }

    public void Set(Vector3 position, Quaternion rotation)
    {
        lastSendPosition = position;
        lastSendRotation = rotation;
    }
}

public class ClientPlayer : ClientWorldEntity
{
    PositionalTracker positionalTracker = new PositionalTracker();

    public override void Init(IClientNetworking client, int entityId)
    {
        base.Init(client, entityId);

        client.AddListener<WorldEntityPacket>(entityId, OnWorldEntityPacket);
        client.OnTick += OnTick;
        positionalTracker.Set(transform.position, transform.rotation);
    }

    private void OnWorldEntityPacket(WorldEntityPacket packet)
    {
        transform.position = packet.position.Get();
        transform.rotation = Quaternion.Euler(packet.rotation.Get());
    }

    void OnTick()
    {
        if (positionalTracker.Passes(transform.position, transform.rotation) == true)
        {
            WorldEntityPacket packet = (WorldEntityPacket)IntrepidSerialize.TakeFromPool(PacketType.WorldEntity);

            packet.entityId = EntityId;
            packet.position.Set(transform.position);
            packet.rotation.Set(transform.rotation.eulerAngles);

            Client.Send(packet);
            positionalTracker.Set(transform.position, transform.rotation);
        }
    }

    public void RequestAttack(int abilityId, int targetId, Vector3 position)
    {
        Combat_AttackRequest packet = (Combat_AttackRequest)IntrepidSerialize.TakeFromPool(PacketType.Combat_AttackRequest);
        packet.frameId = Client.FrameID;
        packet.abilityId = abilityId;
        packet.targetId = targetId;
        packet.position = position;
        Client.Send(packet);
    }
    
}
