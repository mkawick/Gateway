using System.Net;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System;
using System.Collections;
using Vectors;
using System.Collections.Generic;
using System.Diagnostics;
using Packets;

namespace Network
{
    static public class Settings
    {
        static public class Rotation
        {
            static public int quantizationX = 512;
            static public int quantizationY = 512;
            static public int quantizationZ = 512;

            static public int shiftX;
            static public int shiftY;
            static public int maskZ, maskY, maskX;

            static Rotation()
            {
                shiftY = Network.Utils.GetBitPosition(quantizationZ);
                shiftX = Network.Utils.GetBitPosition(quantizationY);
                maskX = quantizationX - 1;
                maskY = quantizationY - 1;
                maskZ = quantizationZ - 1;
            }
        }

        static public class Position
        {
            public const int precision = 256;
            public const int xShift = 43;
            public const int yShift = 23;
            public const int zShift = 3;
            public const long xMask = 0x7FFFF0000000000;
            public const long yMask = 0x00000FFFFF00000;
            public const long zMask = 0x0000000000FFFF8;
        }
    }
    

    public interface IBinarySerializable
    {
        void Write(BinaryWriter writer);
        void Read(BinaryReader reader);
    }
    public class RotationPacker : IBinarySerializable
    {
        int rotation;

        public RotationPacker()
        {
            rotation = 0;
        }

        public void CopyFrom(RotationPacker ext)
        {
            rotation = ext.rotation;
        }
        int Pack(int x, int y, int z)
        {
            int result = (x << (Settings.Rotation.shiftX + Settings.Rotation.shiftY)) + (y << Settings.Rotation.shiftY) + z;
            return result;
        }
        void Unpack(int value, ref int x, ref int y, ref int z)
        {
            x = value & (Settings.Rotation.maskX << (Settings.Rotation.shiftY + Settings.Rotation.shiftX));
            value -= x;
            x >>= Settings.Rotation.shiftX + Settings.Rotation.shiftY;

            y = value & (Settings.Rotation.maskY << (Settings.Rotation.shiftY));
            value -= y;
            y >>= Settings.Rotation.shiftY;
            z = value;
        }

        public void Set(Vector3 eulerAngles)
        {
            int repAngleX = Network.Utils.ConvertDegToQuantitized(eulerAngles.x, Settings.Rotation.quantizationX);
            int repAngleY = Network.Utils.ConvertDegToQuantitized(eulerAngles.y, Settings.Rotation.quantizationY);
            int repAngleZ = Network.Utils.ConvertDegToQuantitized(eulerAngles.z, Settings.Rotation.quantizationZ);
            rotation = Pack(repAngleX, repAngleY, repAngleZ);
        }
        public Vector3 Get()
        {
            int x = 0, y = 0, z = 0;
            Unpack(rotation, ref x, ref y, ref z);
            return new Vector3(Network.Utils.ConvertToDeg(x, Settings.Rotation.quantizationX), Network.Utils.ConvertToDeg(y, Settings.Rotation.quantizationY), Network.Utils.ConvertToDeg(z, Settings.Rotation.quantizationZ));
        }

        public void Read(BinaryReader reader)
        {
            rotation = reader.ReadInt32();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(rotation);
        }
    }

    public class PositionCompressed : IBinarySerializable
    {
        public long position = 0;


        public void Set(Vector3 pos)
        {
            position = Pack(pos.x, pos.y, pos.z);
        }
        public void CopyFrom(PositionCompressed ext)
        {
            position = ext.position;
        }

        public Vector3 Get()
        {
            float x = 0, y = 0, z = 0;
            Unpack(position, ref x, ref y, ref z);
            return new Vector3(x, y, z);
        }

        private void Unpack(long position, ref float x, ref float y, ref float z)
        {
            float uX = (position & Settings.Position.xMask) >> Settings.Position.xShift;
            uX /= Settings.Position.precision;
            if ((position & 1 << 2) != 0)
                uX = -uX;

            x = (float)uX;

            float uY = (position & Settings.Position.yMask) >> Settings.Position.yShift;
            uY /= Settings.Position.precision;
            if ((position & 1 << 1) != 0)
                uY = -uY;

            y = (float)uY;

            float uZ = (position & Settings.Position.zMask) >> Settings.Position.zShift;
            uZ /= Settings.Position.precision;
            if ((position & 1 << 0) != 0)
                uZ = -uZ;

            z = (float)uZ;
        }

        private long Pack(float x, float y, float z)
        {
            bool xNeg = x < 0;
            bool yNeg = y < 0;
            bool zNeg = z < 0;

            if (xNeg) x = -x;
            if (yNeg) y = -y;
            if (zNeg) z = -z;

            long x0 = (long)(x * Settings.Position.precision);
            long y0 = (long)(y * Settings.Position.precision);
            long z0 = (long)(z * Settings.Position.precision);

            long xPacked = (x0 << Settings.Position.xShift) & Settings.Position.xMask;
            long yPacked = (y0 << Settings.Position.yShift) & Settings.Position.yMask;
            long zPacked = (z0 << Settings.Position.zShift) & Settings.Position.zMask;

            long signs = 0;
            if (xNeg) signs |= 1 << 2;
            if (yNeg) signs |= 1 << 1;
            if (zNeg) signs |= 1 << 0;

            long result = xPacked | yPacked | zPacked | signs;
            return result;
        }
        public void Read(BinaryReader reader)
        {
            position = reader.ReadInt64();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(position);
        }
    }

    static public class GenericExtensions
    {
        public static bool Implements<I>(this Type type, I @interface) where I : class
        {
            if (((@interface as Type) == null) || !(@interface as Type).IsInterface)
                throw new ArgumentException("Only interfaces can be 'implemented'.");

            return (@interface as Type).IsAssignableFrom(type);
        }
    }

    public class Utils
    {
        public static IPAddress ResolveIPAddress(string name)
        {
            // Bind to all interfaces
            if (name == null || name.Length == 0 || name.Trim().Equals("0.0.0.0"))
            {
                return IPAddress.Any;
            }

            // Bind to the interface for a single given address or ip
            IPHostEntry ipHostInfo = Dns.GetHostEntry(name.Trim());
            // TODO: probably want to allow IPv6 selection in the future
            IPAddress ip = ipHostInfo.AddressList.FirstOrDefault(e => e.AddressFamily == AddressFamily.InterNetwork);
            if (ip == null)
            {
                ip = ipHostInfo.AddressList.FirstOrDefault(e => e.AddressFamily == AddressFamily.InterNetworkV6);
            }
            if (ip == null)
            {
                ipHostInfo = Dns.GetHostEntry("localhost"); //  may want a flag here eventually
                ip = ipHostInfo.AddressList.FirstOrDefault(e => e.AddressFamily == AddressFamily.InterNetwork);
                if (ip == null)
                    ip = ipHostInfo.AddressList.FirstOrDefault(e => e.AddressFamily == AddressFamily.InterNetworkV6);
            }
            Debug.Assert(ip != null);
            return ip;
        }

        public static IPAddress GetOwnIPAddress()
        {
            return ResolveIPAddress(Dns.GetHostName());
        }

        public static Int64 GetIPBasedApplicationId()
        {
            //return GetOwnIPAddress().ToString().GetHashCode();
            string addr = GetOwnIPAddress().ToString();

            return Convert.ToInt64(addr.Replace(".", ""));
        }

        static public int SetupWrite(BinaryWriter writer)
        {
            long beginPos = writer.BaseStream.Position;
            writer.Seek(sizeof(ushort), SeekOrigin.Current);

            return (int)beginPos;
        }
        static public void FinishWrite(BinaryWriter writer, int beginPos)
        {
            long finalPos = writer.BaseStream.Position;
            ushort bytesWritten = (ushort)(finalPos - beginPos);

            writer.Seek(beginPos, SeekOrigin.Begin);
            writer.Write(bytesWritten);

            writer.Seek((int)finalPos, SeekOrigin.Begin);
        }

        static public ushort SetupRead(BinaryReader reader)
        {
            ushort size = reader.ReadUInt16();
            return size;
        }
        public static bool IsPowerOfTwo(int x)
        {
            return (x & (x - 1)) == 0;
        }
        public static float ConvertToDeg(int value, int range)
        {
            float angle = (float)value / (float)range;
            return angle * 360;
        }
        public static int ConvertRange(float value, int range)
        {
            int repAngle = (int)(value * range);
            repAngle %= range;
            if (repAngle < 0)
                repAngle += range;
            return repAngle;
        }
        public static int ConvertDegToQuantitized(float value, int quantitization)
        {
            float deg = value / 360;

            int repAngle = ConvertRange(deg, quantitization);
            return repAngle;
        }
        public static int GetBitPosition(int input)
        {
            for (int i = 0; i < 32; i++)
            {
                if ((input & (1 << i)) != 0)
                    return i;
            }
            return -1;
        }


        public class DatablobAccumulator
        {
            List<DataBlob> accumulator = new List<DataBlob>();

            public int BlobCount { get{return accumulator.Count;} }
            public bool Add( DataBlob blob )
            {
                Debug.Assert(accumulator.Count < blob.totalRawDataPacketCount);
                if (accumulator.Count > 1)
                {
                    //Debug.Assert(blob.packetIndex != accumulator[accumulator.Count - 1].packetIndex);
                    if (blob.packetIndex == accumulator[accumulator.Count - 1].packetIndex)
                        return false;
                }
                accumulator.Add(blob);
                if (accumulator.Count == blob.totalRawDataPacketCount)
                    return true;

                return false;
            }

            public void Clear()
            {
                // blobs should already be freed
                foreach (var blob in accumulator)
                {
                    IntrepidSerialize.ReturnToPool(blob);
                }
                
                accumulator = new List<DataBlob>();
            }
            public List<DataBlob> PrepToSendRawData(byte[] rawData, int size)
            {
                List<DataBlob> blobs = new List<DataBlob>();
                int offset = 0;
                int index = 0;
                while (offset < size)
                {
                    DataBlob blob = (DataBlob)IntrepidSerialize.CreatePacket(PacketType.DataBlob);
                    blob.totalRawDataPacketCount = 1;
                    blob.packetIndex = (short)index++;
                    int currentSize = size - offset;
                    if (currentSize > NetworkConstants.dataBlobMaxPacketSize)
                        currentSize = NetworkConstants.dataBlobMaxPacketSize;
                    //if(currentSize > size - offset)

                    blob.Prep(rawData, currentSize, offset);
                    offset += currentSize;
                    blobs.Add(blob);
                }
                foreach (var blob in blobs)
                {
                    blob.totalRawDataPacketCount = (short)index;
                }
                return blobs;
            }

            public  byte[] ConvertDatablobsIntoRawData(List<DataBlob> blobs = null)
            {
                int size = 0;
                if (blobs == null)
                    blobs = accumulator;

                foreach (var blob in blobs)
                {
                    size += blob.length;
                }
                byte[] rawData = new byte[size];

                int offset = 0;
                foreach (var blob in blobs)
                {
                    Buffer.BlockCopy(blob.rawData, 0, rawData, offset, blob.length);
                    offset += blob.length;
                }

                return rawData;

            }
            public int GetSizeOfAllBlobs(List<DataBlob> blobs = null)
            {
                if (blobs == null)
                    blobs = accumulator;
                int size = 0;
                foreach (var blob in blobs)
                {
                    size += blob.length;
                }
                return size;
            }
        }
    }

    
}