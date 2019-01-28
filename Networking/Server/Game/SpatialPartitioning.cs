using System.Collections.Generic;
using UnityEngine;

public class SpatialPartitioning
{
    public const float INTEREST_RADIUS = 20f;

    // ============================================================================
    // Octree approach
    // ============================================================================
    // With 1000 players, 100m radius, approx 110ms
    private static PointOctree<ServerWorldEntity> octree = new PointOctree<ServerWorldEntity>(750, new Vector3(60, 0, 250), 1);
    private static Ray ray = new Ray();
    private static List<ServerWorldEntity> listResult = new List<ServerWorldEntity>();

    public static void Register(ServerWorldEntity entity)
    {
        lock (octree)
        {
            octree.Add(entity, entity.Position);
        }
    }

    public static void Update(ServerWorldEntity entity)
    {
        lock (octree)
        {
            octree.Remove(entity);
            Register(entity);
        }
    }

    public static void DrawOctree()
    {
        lock (octree)
        {
            octree.DrawAllBounds();
            octree.DrawAllObjects();
        }
    }

    public static int GetEntitiesInRadius<ENTITY_TYPE>(ref ENTITY_TYPE[] result, Vector3 position, float radius = INTEREST_RADIUS) where ENTITY_TYPE : ServerWorldEntity
    {
        ray.origin = position;
        ray.direction = Vector3.up;
        bool gotHits;
        lock (octree)
        {
            gotHits = octree.GetNearbyNonAlloc(ray, radius, listResult);
        }
        if (!gotHits)
        {
            return 0;
        }
        // Clear out all entities that don't match the requested class
        listResult.RemoveAll((entity) => { return !(entity is ENTITY_TYPE); });
        int hits = listResult.Count;
        if (result.Length < hits)
        {
            result = new ENTITY_TYPE[(int)Mathf.Round(hits * 1.1f)];
        }
        listResult.CopyTo(result);
        return hits;
    }

    public static ENTITY_TYPE[] GetEntitiesInRadius<ENTITY_TYPE>(Vector3 position, float radius = INTEREST_RADIUS) where ENTITY_TYPE : ServerWorldEntity
    {
        ray.origin = position;
        ray.direction = Vector3.up;
        bool gotHits;
        lock (octree)
        {
            gotHits = octree.GetNearbyNonAlloc(ray, radius, listResult);
        }
        if (!gotHits)
        {
            return new ENTITY_TYPE[0];
        }
        // Clear out all entities that don't match the requested class
        listResult.RemoveAll((entity) => { return !(entity is ENTITY_TYPE); });
        int hits = listResult.Count;
        var result = new ENTITY_TYPE[hits];
        listResult.CopyTo(result);
        return result;
    }
}
