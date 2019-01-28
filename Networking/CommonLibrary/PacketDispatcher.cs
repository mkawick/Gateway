using Packets;
using System;
using System.Collections.Generic;

public class PacketDispatcher : IPacketSource
{
    struct PacketListener
    {
        public long handle;
        public Action<BasePacket> action;
    }

    // Tuple(entityId, packet type) => List of packet listeners to fire
    Dictionary<Tuple<int, Type>, List<PacketListener>> entityPacketListeners;
    // entityId => List of packet listeners to fire
    Dictionary<Type, List<PacketListener>> packetListeners;
    // entityId => List of Tuple(entityId, packet type) that have listeners
    Dictionary<int, HashSet<Type>> entityListenerTypesByEntityId;

    private long nextHandle = 1024;

    public PacketDispatcher()
    {
        entityPacketListeners = new Dictionary<Tuple<int, Type>, List<PacketListener>>();
        packetListeners = new Dictionary<Type, List<PacketListener>>();
        entityListenerTypesByEntityId = new Dictionary<int, HashSet<Type>>();
    }

    public void SignalListener(int entityId, BasePacket packet)
    {
        List<PacketListener> actions = null;
        if (entityPacketListeners.TryGetValue(new Tuple<int, Type>(entityId, packet.GetType()), out actions))
        {
            // Iterate by index, as the action may add new listeners
            for (int i = actions.Count - 1; i >= 0; i--)
            {
                actions[i].action(packet);
            }
        }
    }

    public void SignalListener(BasePacket packet)
    {
        List<PacketListener> actions = null;
        if (packetListeners.TryGetValue(packet.GetType(), out actions))
        {
            // Iterate by index, as the action may add new listeners
            for (int i = actions.Count - 1; i >= 0; i--)
            {
                actions[i].action(packet);
            }
        }
    }

    public long AddListener<PACKET_TYPE>(int entityID, Action<PACKET_TYPE> action) where PACKET_TYPE : BasePacket
    {
        // Add the listener to entityPacketListeners
        var key = new Tuple<int, Type>(entityID, typeof(PACKET_TYPE));
        List<PacketListener> actions = null;
        if (!entityPacketListeners.TryGetValue(key, out actions))
        {
            actions = new List<PacketListener>();
            entityPacketListeners.Add(key, actions);
        }
        long result = nextHandle++;
        actions.Add(new PacketListener {
            handle = result,
            action = e => action((PACKET_TYPE)e)
        });

        // Add the type to the listenerTypes
        HashSet<Type> listenerTypes;
        if (!entityListenerTypesByEntityId.TryGetValue(entityID, out listenerTypes))
        {
            listenerTypes = new HashSet<Type>();
            entityListenerTypesByEntityId.Add(entityID, listenerTypes);
        }
        listenerTypes.Add(typeof(PACKET_TYPE));

        return result;
    }

    public long AddListener<PACKET_TYPE>(Action<PACKET_TYPE> action) where PACKET_TYPE : BasePacket
    {
        var key = typeof(PACKET_TYPE);
        List<PacketListener> actions = null;
        if (!packetListeners.TryGetValue(key, out actions))
        {
            actions = new List<PacketListener>();
            packetListeners.Add(key, actions);
        }
        long result = nextHandle++;
        actions.Add(new PacketListener {
            handle = result,
            action = e => action((PACKET_TYPE)e)
        });
        return result;
    }

    public void RemoveListener<PACKET_TYPE>(int entityId, long listenerHandle) where PACKET_TYPE : BasePacket
    {
        List<PacketListener> actions = null;
        if (entityPacketListeners.TryGetValue(new Tuple<int, Type>(entityId, typeof(PACKET_TYPE)), out actions))
        {
            // Iterate by index, as the action may add new listeners
            for (int i = actions.Count - 1; i >= 0; i--)
            {
                if (actions[i].handle == listenerHandle)
                {
                    actions.RemoveAt(i);
                    break;
                }
            }
        }
    }

    public void RemoveListener<PACKET_TYPE>(long listenerHandle) where PACKET_TYPE : BasePacket
    {
        List<PacketListener> actions = null;
        if (packetListeners.TryGetValue(typeof(PACKET_TYPE), out actions))
        {
            // Iterate by index, as the action may add new listeners
            for (int i = actions.Count - 1; i >= 0; i--)
            {
                if (actions[i].handle == listenerHandle)
                {
                    actions.RemoveAt(i);
                    break;
                }
            }
        }
    }

    public void RemoveListenersForEntity(int entityId)
    {
        HashSet<Type> listenerTypes;
        if (entityListenerTypesByEntityId.TryGetValue(entityId, out listenerTypes))
        {
            foreach (var type in listenerTypes)
            {
                entityPacketListeners.Remove(new Tuple<int, Type>(entityId, type));
            }
            entityListenerTypesByEntityId.Remove(entityId);
        }
    }
}
