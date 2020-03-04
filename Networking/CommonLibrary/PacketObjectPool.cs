using System;
using System.Collections.Generic;
using CommonLibrary;

namespace Packets
{
    internal class PacketPool
    {
        private const int initialSize = 2048;
        private readonly Queue<BasePacket> pool;
        private readonly PacketType type;

        public PacketPool(PacketType _type)
        {
            type = _type;

            pool = new Queue<BasePacket>(initialSize);

            for (int i = 0; i < initialSize; i++)
            {
                // No need to lock on pool for this, as it's not yet
                // externally accessible
                CreateAndEnqueuePacket();
            }
        }

        // Not thread-safe: ensure you have a lock on the pool,
        // or that the pool can only be accessed from one thread
        // before calling this.
        private void CreateAndEnqueuePacket()
        {
            BasePacket packet;
            // Stop the BasePacket constructor from erroring
            BasePacket.AllowConstruction = true;
            try
            {
                packet = IntrepidSerialize.CreatePacket(type);
            }
            finally
            {
                BasePacket.AllowConstruction = false;
            }
            packet.IsInPool = true;
            pool.Enqueue(packet);
        }

        public BasePacket GetNew()
        {
            BasePacket packet;
            lock (pool)
            {
                if (pool.Count == 0)
                {
                    CreateAndEnqueuePacket();
                }
                packet = pool.Dequeue();
            }
            if (!packet.IsInPool)
            {
                Console.WriteLine("Packet wasn't marked as being in the pool");
                //throw new InvalidOperationException("Packet wasn't marked as being in the pool");
            }
            packet.IsInPool = false;
            return packet;
        }
        public void Free(BasePacket bp)
        {
            if (bp.PacketType != type)
            {
                Console.WriteLine("Attempted to free an invalid packet type");
                //throw new InvalidOperationException("object pool problem");
            }
            if (bp.IsInPool)
            {
                Console.WriteLine("Packet already in the pool");
                //throw new InvalidOperationException("Packet already in the pool");
            }
            bp.IsInPool = true;
            bp.Dispose();
            lock (pool)
            {
                pool.Enqueue(bp);
            }
        }
    }

    internal class PacketPoolManager
    {
        private readonly Dictionary<PacketType, PacketPool> pools;

        public PacketPoolManager()
        {
            pools = new Dictionary<PacketType, PacketPool>();
            Array packetTypeValues = Enum.GetValues(typeof(PacketType));
            foreach (PacketType type in packetTypeValues)
            {
                if (type == PacketType.None)
                    continue;
                pools.Add(type, new PacketPool(type));
            }
        }

        public BasePacket Allocate(PacketType type)
        {
            return pools[type].GetNew();
        }
        public bool Deallocate(BasePacket packet)
        {
            try
            {
                pools[packet.PacketType].Free(packet);
                return true;
            }
            catch
            {
                Console.WriteLine("memory pool problem");
            }
            return false;
        }
    }

    internal class BufferPoolManager
    {
        byte[] buffer;
        BufferPoolManager()
        {
            buffer = new byte[32 * 1024 * 1024];// 32 megs
        }
    }
}