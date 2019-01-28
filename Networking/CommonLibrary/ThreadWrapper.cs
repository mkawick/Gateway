using System;
using System.Threading;

namespace CommonLibrary
{
    public abstract class ThreadWrapper
    {
        public int configuredSleep = 10;
        public void SetMaxFPS(int fps)
        {
            float sleepTime = 1000.0f / fps;
            configuredSleep = (int)sleepTime;// ignore fractional frames for now
        }
        protected bool hasTerminated = false;
        
        public bool IsRunning
        {
            get
            {
                return myThread != null && myThread.IsAlive;
            }
        }
           

        Thread myThread;

        public event Action<Exception> OnExceptionFromThread;

        //-----------------------------------------------------
        public virtual void StartService()
        {
            if (IsRunning) return;
            myThread = new Thread(RunThread);
            myThread.Start();
        }
        public virtual void EndService()
        {
            hasTerminated = true;
        }

        public void RunThread()
        {
            try
            {
                while (hasTerminated == false)
                {
                    this.ThreadTick();
                    Thread.Sleep(configuredSleep);
                }
            }
            catch (Exception e)
            {
                if (OnExceptionFromThread != null)
                    OnExceptionFromThread(e);
            }
        }
        public virtual void Cleanup()
        {
            EndService();
        }

        protected abstract void ThreadTick();
    }
}
