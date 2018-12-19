using System;
using System.Collections.Generic;
using System.ComponentModel;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Common.Interfaces
{
    public interface IRecorder :IRecorderProperties, INotifyPropertyChanged
    {
        void DeckPlay();
        void DeckStop();
        void Abort();
        void DeckFastForward();
        void DeckRewind();
        IMedia Capture(IPlayoutServerChannel channel, TimeSpan tcIn, TimeSpan tcOut, bool narrowMode, string mediaName, string fileName);
        IMedia Capture(IPlayoutServerChannel channel, TimeSpan timeLimit, bool narrowMode, string mediaName, string fileName);
        TimeSpan TimeLimit { get; }
        void SetTimeLimit(TimeSpan value);
        void Finish();
        void GoToTimecode(TimeSpan tc, TVideoFormat format);
        TimeSpan CurrentTc { get; }
        TDeckControl DeckControl { get; }
        TDeckState DeckState { get; }
        bool IsDeckConnected { get; }
        bool IsServerConnected { get; }
        IEnumerable<IPlayoutServerChannel> Channels { get; }
        IMedia RecordingMedia { get; }
        IWatcherDirectory RecordingDirectory { get; }
    }

    public interface IRecorderProperties
    {
        int Id { get; }
        string RecorderName { get; }
        int DefaultChannel { get; }
    }
}