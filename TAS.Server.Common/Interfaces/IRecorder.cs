using System;

namespace TAS.Server.Interfaces
{
    public interface IRecorder :IRecorderProperties
    {
        void Play();
        void Stop();
        void Abort();
        void FastForward();
        void Rewind();
        void Capture(IPlayoutServerChannel channel, TimeSpan tcIn, TimeSpan tcOut, string fileName);
    }

    public interface IRecorderProperties
    {
        int Id { get; }
        string RecorderName { get; }
    }
}