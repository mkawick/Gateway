using System;
using System.IO;
using Network;

namespace Vectors
{
    public struct Vector3 : IBinarySerializable
    {
        public float x;
        public float y;
        public float z;

        public Vector3(float x, float y, float z)
        {
            this.x = x; this.y = y; this.z = z;
        }

#if UNITY_ || UNITY_5_3_OR_NEWER
        public Vector3(UnityEngine.Vector3 v)
        {
            x = v.x; y = v.y; z = v.z;
        }

        public UnityEngine.Vector3 ToUnityVec()
        {
            return new UnityEngine.Vector3(x, y, z);
        }

        // Support implicit conversion between UnityEngine.Vector3 and Packets.Vector3

        public static implicit operator UnityEngine.Vector3(Vector3 v)
        {
            return v.ToUnityVec();
        }

        public static implicit operator Vector3(UnityEngine.Vector3 v)
        {
            return new Vector3(v);
        }
#else
        public void Normalize()
        {
            float len = (float)Math.Sqrt(x * x + y * y + z * z);
            if (len == 0)
                len = 1;
            x = x / len;
            y = y / len;
            z = z / len;
        }

        static public Vector3 operator * (Vector3 v, float mult)
        {
            return new Vector3(v.x * mult, v.y * mult, v.z * mult);
        }
#endif

        #region Operators

        public static bool operator ==(Vector3 v1, Vector3 v2)
        {
            return Vector3.DistanceSquared(v1, v2) <= float.Epsilon;
        }

        public static bool operator !=(Vector3 v1, Vector3 v2)
        {
            return Vector3.DistanceSquared(v1, v2) > float.Epsilon;
        }

        public static Vector3 operator -(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        }

        public static Vector3 operator +(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        }

        public override int GetHashCode()
        {
            //Thank you Jon Skeet...
            unchecked
            {
                int hash = 17;

                hash = hash * 29 + x.GetHashCode();
                hash = hash * 29 + y.GetHashCode();
                hash = hash * 29 + z.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            Vector3 v2 = (Vector3)obj;
            return v2 == this;
        }

        #endregion

        public static float DistanceSquared(Vector3 v1, Vector3 v2)
        {
            Vector3 dir = new Vector3();
            dir.x = v1.x - v2.x;
            dir.y = v1.y - v2.y;
            dir.z = v1.z - v2.z;

            return dir.x * dir.x + dir.y * dir.y + dir.z * dir.z;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(x);
            writer.Write(y);
            writer.Write(z);
        }
        public void Read(BinaryReader reader)
        {
            x = reader.ReadSingle();
            y = reader.ReadSingle();
            z = reader.ReadSingle();
        }

        public void Print()
        {
            Console.Write(ToString());
        }

        public override string ToString()
        {
            return string.Format("({0}, {1}, {2})", x, y, z);
        }

    }
}
