using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Packets;

public class StringUtils
{
    public static string Sanitize(string dirtyString, bool allowSpaces = true)
    {
        HashSet<char> removeChars;
        if(allowSpaces == true)
            removeChars = new HashSet<char>(" ?&^$#@!()+-,:;<>’\'-_*");
        else
            removeChars = new HashSet<char>("?&^$#@!()+-,:;<>’\'-_*");

        StringBuilder result = new StringBuilder(dirtyString.Length);
        foreach (char c in dirtyString)
            if (!removeChars.Contains(c)) // prevent dirty chars
                result.Append(c);
        return result.ToString();
    }

    public static MemoryStream SerializeToStream(object o)
    {
        MemoryStream stream = new MemoryStream();
        IFormatter formatter = new BinaryFormatter();
        formatter.Serialize(stream, o);
        return stream;
    }

    public static object DeserializeFromStream(MemoryStream stream)
    {
        IFormatter formatter = new BinaryFormatter();
        stream.Seek(0, SeekOrigin.Begin);
        object o = formatter.Deserialize(stream);
        return o;
    }

    public static byte[] SerializeToBuffer(BasePacket bp)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            stream.Seek(0, SeekOrigin.Begin);
            BinaryWriter writer = new BinaryWriter(stream);
            {
                bp.Write(writer);
            }
            return stream.ToArray();
        }
    }

    public abstract class FixedLengthStringBase
    {
        protected Int16 size = 40;
        protected Int16 len = 0;
        protected char[] str;

        protected FixedLengthStringBase(int _size)
        {
            size = (Int16)_size;
            str = new char[size];
        }
        public Int16 Size { get { return size; } }
        public FixedLengthStringBase Copy(FixedLengthStringBase text)
        {
            len = text.len;
            Array.Copy(text.str, str, len);
            return this;
        }
        public FixedLengthStringBase Copy(char[] text)
        {
            len = (Int16)text.Length;
            if (len > size)
            {
                len = size;
            }
            Array.Copy(text, str, len);
            return this;
        }
        public FixedLengthStringBase Copy(string text)
        {
            len = (Int16)text.Length;
            if (len > size)
            {
                len = size;
            }
            Array.Copy(text.ToCharArray(0, len), str, len);
            return this;
        }

        public string MakeString()
        {
            return new string(str,0,len);
        }
        public char[] GetRaw()
        {
            return str;
        }

        public void Write(BinaryWriter writer)
        {
            int pos = Network.Utils.SetupWrite(writer);

            writer.Write(len);
            writer.Write(str, 0, size);// write the fixed len

            Network.Utils.FinishWrite(writer, pos);
        }
        public void Read(BinaryReader reader)
        {
            Network.Utils.SetupRead(reader);// todo: move outside of this function
            len = reader.ReadInt16();
            if (len > size)
            {
                len = size;
            }
            // read the fixed len
            Array.Copy(reader.ReadChars(size), str, len);
        }
    }

    public class FixedLengthString16 : FixedLengthStringBase
    {
        public FixedLengthString16() : base(16) { }
    }
    public class FixedLengthString32 : FixedLengthStringBase
    {
        public FixedLengthString32() : base(32) { }
    }
    public class FixedLengthString40 : FixedLengthStringBase
    {
        public FixedLengthString40() : base(40) { }
    }
    public class FixedLengthString60 : FixedLengthStringBase
    {
        public FixedLengthString60() : base(60) { }
    }
    public class FixedLengthString80 : FixedLengthStringBase
    {
        public FixedLengthString80() : base(80) { }
    }
}
