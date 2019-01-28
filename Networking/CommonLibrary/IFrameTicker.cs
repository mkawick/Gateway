using System;

public interface IFrameTicker
{
    int FrameID { get; }

    /// <summary>
    /// Networking Tick
    /// </summary>
    event Action OnTick;
}
