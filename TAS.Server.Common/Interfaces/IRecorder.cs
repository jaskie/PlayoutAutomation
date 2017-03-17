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
        void GoToTimecode(TimeSpan tc, TVideoFormat format);
        TimeSpan CurrentTc { get; }
        TDeckControl DeckControl { get; }
        TDeckState DeckState { get; }
        bool IsConnected { get; }
        IEnumerable<IPlayoutServerChannel> Channels { get; }
        IServerMedia RecordingMedia { get; }
        IServerDirectory RecordingDirectory { get; }
    }

    public interface IRecorderProperties
    {
        int Id { get; }
        string RecorderName { get; }
    }
}