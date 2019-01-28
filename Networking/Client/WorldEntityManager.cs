using Packets;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a map of entity id to ClientWorldEntity
/// Will destroy the game object associated with an entity on receipt of an EntityDestroyPacket.
/// </summary>
public class WorldEntityManager
{
    // Keeps track of entityId -> world entity relationship
    Dictionary<int, ClientWorldEntity> worldEntities;

    IClientNetworking client;

    public WorldEntityManager(IClientNetworking client)
    {
        this.client = client;
        worldEntities = new Dictionary<int, ClientWorldEntity>();
        client.AddListener<EntityDestroyPacket>(OnEntityDestroy);
    }

    private void OnEntityDestroy(EntityDestroyPacket packet)
    {
        var entity = RemoveEntity(packet.entityId);
        if (entity != null)
        {
            client.RemoveListenersForEntity(packet.entityId);
            GameObject.Destroy(entity.gameObject);
        }
    }

    /// <summary>
    /// Add the entity to the manager.
    /// </summary>
    /// <param name="worldEntity"></param>
    /// <returns></returns>
    public bool AddWorldEntity(ClientWorldEntity worldEntity)
    {
        if (worldEntities.ContainsKey(worldEntity.EntityId))
        {
            Debug.LogWarningFormat("Found duplicate instance id: {0}, {1}", worldEntity.EntityId, worldEntity);
            return false;
        }
        worldEntities.Add(worldEntity.EntityId, worldEntity);
        return true;
    }

    /// <summary>
    /// Removes and returns the entity with the given ID.
    /// </summary>
    /// <param name="entityID"></param>
    /// <returns></returns>
    private ClientWorldEntity RemoveEntity(int entityID)
    {
        ClientWorldEntity entity = null;
        if (worldEntities.TryGetValue(entityID, out entity))
        {
            worldEntities.Remove(entityID);
        }
        return entity;
    }
}
