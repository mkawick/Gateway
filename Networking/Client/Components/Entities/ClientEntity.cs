using Packets;
using UnityEngine;

/// <summary>
/// An entity that the client is aware of
/// </summary>
public class ClientEntity : MonoBehaviour
{
    public int EntityId { get; private set; }

    public IClientNetworking Client { get; private set; }

    // Used to signal to the main thread that we should destroy ourselves
    private bool shouldDestroy = false;

    public virtual void Init(IClientNetworking client, int entityId)
    {
        Client = client;
        EntityId = entityId;
        // TODO: Managers?
        client.AddListener<EntityDestroyPacket>(entityId, OnEntityDestroyPacket);
    }

    private void OnEntityDestroyPacket(EntityDestroyPacket obj)
    {
        // Signal the main thread to destroy this entity
        shouldDestroy = true;
    }

    protected virtual void Update()
    {
        if (shouldDestroy)
        {
            Destroy(gameObject);
            return;
        }
    }
}
