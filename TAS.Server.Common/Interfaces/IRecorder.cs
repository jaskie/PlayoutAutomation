using System;
using System.Collections.Generic;
using System.ComponentModel;
using TAS.Common;

namespace TAS.Server.Interfaces
{
    public interface IRecorder :IRecorderProperties, INotifyPropertyChanged
    {
        void DeckPlay();
        void DeckStop();
        void Abort();
        void DeckFastForward();
        void DeckRewind();
        IMedia Capture(IPlayoutServerChannel channel, TimeSpan tcIn, TimeSpan tcOut, string fileName);
        IMedia Capture(IPlayoutServerChannel channel, TimeSpan timeLimit, string fileName);
        void GoToTimecode(TimeSpan tc, TVideoFormat format);
        void SetTimeLimit(TimeSpan limit);
        TimeSpan CurrentTc { get; }
        TimeSpan TimeLimit { get; }
        TDeckControl DeckControl { get; }
        TDeckState DeckState { get; }
        bool IsDeckConnected { get; }
        IEnumerable<IPlayoutServerChannel> Channels { get; }
        IMedia RecordingMedia { get; }
        IMediaDirectory RecordingDirectory { get; }
    }

    public interface IRecorderProperties
    {
        int Id { get; }
        string RecorderName { get; }
    }
}