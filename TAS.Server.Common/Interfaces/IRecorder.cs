using System;
using System.Collections.Generic;
using System.ComponentModel;
using TAS.Common;

namespace TAS.Server.Interfaces
{
    public interface IRecorder :IRecorderProperties, INotifyPropertyChanged
    {
        void Play();
        void Stop();
        void Abort();
        void FastForward();
        void Rewind();
        void Capture(IPlayoutServerChannel channel, TimeSpan tcIn, TimeSpan tcOut, string fileName);
        TimeSpan CurrentTc { get; }
        TDeckControl DeckControl { get; }
        TDeckState DeckState { get; }
        IEnumerable<IPlayoutServerChannel> Channels { get; }
    }

    public interface IRecorderProperties
    {
        int Id { get; }
        string RecorderName { get; }
    }
}