
using System.IO;
using System.Collections.Generic;
using Network;
using System;
//using Linq;
//using StringUtils;

namespace Packets
{
  /*  public class SerializableIntList : List<int>, Packets.BinarySerializable
    { }

    public class SerializableIntList : List<int>, Packets.BinarySerializable
    {

    }*/


    public class SerializedList<T> where T : IBinarySerializable, new()
    {
        public List<T> listOfSerializableItems = null; // assign value before writing

        //public SerializedList<TestDataBlob> listOfBlobs;
        public SerializedList()
        {
            listOfSerializableItems = new List<T>();
        }

        public void Write(BinaryWriter writer)
        {
            if (listOfSerializableItems == null)
                throw new System.Exception("WTF");

            int num = listOfSerializableItems.Count;
            writer.Write(num);

            for (int i = 0; i < num; i++)
            {
                T item = listOfSerializableItems[i];
                item.Write(writer);
            }
        }

        public void Read(BinaryReader reader)
        {
            listOfSerializableItems = new List<T>();

            int num = reader.ReadInt32();
            for (int i = 0; i < num; i++)
            {
                T newItem = new T();
                newItem.Read(reader);
                listOfSerializableItems.Add(newItem);
            }
        }

        public void CopyFrom(SerializedList<T> externalList)
        {
            listOfSerializableItems = new List<T>(externalList.listOfSerializableItems);
        }
    }


    public class TestDataBlob : IBinarySerializable
    {
        int key;
        int value;

        public TestDataBlob()
        {
            key = 0; value = 0;
        }
        public TestDataBlob(int k, int v)
        {
            key = k; value = v;
        }
        public void Write(BinaryWriter writer)
        {
            writer.Write(key);
            writer.Write(value);
        }
        public void Read(BinaryReader reader)
        {
            key = reader.ReadInt32();
            value = reader.ReadInt32();
        }
    }

    public class TestPacket : BasePacket
    {
        public override PacketType PacketType { get { return PacketType.TestPacket; } }

        public SerializedList<TestDataBlob> listOfBlobs;

        
        public TestPacket() : base()
        {
            listOfBlobs = new SerializedList<TestDataBlob>();
        }
        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);

            listOfBlobs.Write(writer);
        }
        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            listOfBlobs.Read(reader);
        }
    }

    public class DataBlob : BasePacket
    {
        public override PacketType PacketType { get { return PacketType.DataBlob; } }

        public byte[] rawData;
        public short length;
        public short packetIndex;
        public short totalRawDataPacketCount;
        ///public short blobGroupId;

        public DataBlob() : base()
        {
            rawData = null;
            length = 0;
            packetIndex = 1;
            totalRawDataPacketCount = 1;
        }

        override public void Dispose() 
        {
            rawData = null;// cleanup
        }

        public void Prep(byte[] bytes, int size, int offset = 0)
        {
            if(size > NetworkConstants.dataBlobMaxPacketSize)
            {
                throw new Exception(string.Format("blob size too large: {0}", size));
            }
            length = (short)size;
            rawData = new byte[length];

            Buffer.BlockCopy(bytes, offset, rawData, 0, length);
        }
        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);

            writer.Write(totalRawDataPacketCount);
            writer.Write(packetIndex);
            
            writer.Write(length);
            writer.Write(rawData, 0, length);
        }
        public override void Read(BinaryReader reader)
        {
            base.Read(reader);

            totalRawDataPacketCount = reader.ReadInt16();
            packetIndex = reader.ReadInt16();
            
            
            length = reader.ReadInt16();
            long remainingBuffer = reader.BaseStream.Length - reader.BaseStream.Position;
            if (remainingBuffer < length)
            {
                throw new EndOfStreamException(string.Format("not enough buffer left: {0} needed, {1} remaining", length, remainingBuffer));
            }
            rawData = null;
            rawData = new byte[length];
            reader.Read(rawData, 0, length);
        }
    }


}