using Packets;
using System;
using System.Collections.Generic;
using System.Text;
using Vectors;

namespace Testing
{
    public class PlayerState
    {
        public int connectionId;
        public int characterId;
        public int accountId;
        public int entityId;
        public bool requestedRenderFrame = false;
        public Vector3 position = new Vector3();
        public Vector3 rotation = new Vector3();
        public bool isDirty = false;
        public RenderSettings settings = new RenderSettings();
        long timeStampOfLastRenderFrame = 0;


        public byte[] renderBuffer = null;
        public void Set(Vector3 p, Vector3 r)
        {
            position = p;
            rotation = r;
            isDirty = true;
        }
        public void Clear()
        {
            isDirty = false;
            requestedRenderFrame = false;
        }
        public int SetupRenderBuffer(int w, int h, int bytesPerPixel)
        {
            int size = w * h * bytesPerPixel;
            if (renderBuffer != null)
                return size;

            renderBuffer = new byte[size];
            return size;
        }

        public void TimeStampForFPS(long milliseconds)
        {
            timeStampOfLastRenderFrame = milliseconds;
        }
        public void UpdateForFPS(long milliseconds)
        {

            if (settings.maxFPS == -1)
                return;

            long fps = settings.maxFPS;
            if (fps > 90)
                fps = 90;
            if (fps < 5)
                fps = 5;
            long timeout = 1000 / fps;
            long timeElapsed = milliseconds - timeStampOfLastRenderFrame;
            if (timeElapsed > timeout)
            {
                timeStampOfLastRenderFrame = milliseconds;
                requestedRenderFrame = true;
            }

        }
    }
}
