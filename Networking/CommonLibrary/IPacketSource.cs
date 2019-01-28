using Packets;
using System;

public interface IPacketSource
{
    /// <summary>
    /// Subscribe to receive packets of this type, intended for this entity ID
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entityID"></param>
    /// <param name="action"></param>
    /// <returns>A listener handle, which can be used to remove the listener</returns>
    long AddListener<T>(int entityID, Action<T> action) where T : BasePacket;

    /// <summary>
    /// Subscribe to receive packets of this type, regardless of entity ID
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="action"></param>
    /// <returns>A listener handle, which can be used to remove the listener</returns>
    long AddListener<T>(Action<T> action) where T : BasePacket;

    /// <summary>
    /// Removes the listener against the given entityId with the given handle.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entityId"></param>
    /// <param name="listenerHandle"></param>
    void RemoveListener<T>(int entityId, long listenerHandle) where T : BasePacket;

    /// <summary>
    /// Removes the non-entity listener with the given handle
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="listenerHandle"></param>
    void RemoveListener<T>(long listenerHandle) where T : BasePacket;

    /// <summary>
    /// Removes all entity listeners for a given entity.
    /// </summary>
    /// <param name="entityId"></param>
    void RemoveListenersForEntity(int entityId);
}
