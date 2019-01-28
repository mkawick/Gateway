#if UNITY_5_3_OR_NEWER
using System;
using System.IO;
using System.Text;
using UnityEngine;

//Inspiration: https://jacksondunstan.com/articles/2986
public class ConsoleWriteRedirecter : MonoBehaviour
{
    private void Awake()
    {
        Redirect();
    }

    private class UnityTextWriter : TextWriter
    {
        private StringBuilder buffer = new StringBuilder();
        // StringBuilder isn't thread-safe, so we need to serialize access to it via this lock
        private System.Object bufferLock = new System.Object();

        public override void Flush()
        {
            lock (bufferLock)
            {
#if UNITY_EDITOR
                Debug.Log(buffer.ToString());
#else
                Debug.LogError(buffer.ToString());
#endif
                buffer.Length = 0;
            }
        }

        public override void Write(string value)
        {
            lock (bufferLock)
            {
                buffer.Append(value);
                if (value != null)
                {
                    var len = value.Length;
                    if (len > 0)
                    {
                        var lastChar = value[len - 1];
                        if (lastChar == '\n')
                        {
                            Flush();
                        }
                    }
                }
            }
        }

        public override void Write(char value)
        {
            lock (bufferLock)
            {
                buffer.Append(value);
                if (value == '\n')
                {
                    Flush();
                }
            }
        }

        public override void Write(char[] value, int index, int count)
        {
            Write(new string(value, index, count));
        }

        public override Encoding Encoding
        {
            get { return Encoding.Default; }
        }
    }

    public static void Redirect()
    {
        var writer = new UnityTextWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
    }
}
#endif