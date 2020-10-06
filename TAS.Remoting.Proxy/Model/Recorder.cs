using System;
using System.Collections.Generic;
using jNet.RPC;
using jNet.RPC.Client;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Remoting.Model.Media;

namespace TAS.Remoting.Model
{
    [DtoType(typeof(IRecorder))]
    public class Recorder : ProxyObjectBase, IRecorder
    {
        #region IRecorder

        #pragma warning disable CS0649

        [DtoMember(nameof(IRecorder.Channels))]
        private List<PlayoutServerChannel> _channels;

        [DtoMember(nameof(IRecorder.RecordingDirectory))]
        private ServerDirectory _recordingDirectory;

        [DtoMember(nameof(IRecorder.CurrentTc))]
        private TimeSpan _currentTc;

        [DtoMember(nameof(IRecorder.DeckControl))]
        private TDeckControl _deckControl;

        [DtoMember(nameof(IRecorder.DeckState))]
        private TDeckState _deckState;

        [DtoMember(nameof(IRecorder.Id))]
        private int _id;

        [DtoMember(nameof(IRecorder.ServerId))]
        private int _serverId;

        [DtoMember(nameof(IRecorder.IsDeckConnected))]
        private bool _isDeckConnected;

        [DtoMember(nameof(IRecorder.IsServerConnected))]
        private bool _isServerConnected;

        [DtoMember(nameof(IRecorder.RecorderName))]
        private string _recorderName;

        [DtoMember(nameof(IRecorder.RecordingMedia))]
        private IMedia _recordingMedia;

        [DtoMember(nameof(IRecorder.TimeLimit))]
        private TimeSpan _timeLimit;

        [DtoMember(nameof(IRecorder.DefaultChannel))]
        private readonly int _defaultChannel;

#pragma warning restore

        public IEnumerable<IPlayoutServerChannel> Channels => _channels;

        public TimeSpan CurrentTc => _currentTc;

        public TDeckControl DeckControl => _deckControl;

        public TDeckState DeckState => _deckState;

        public int Id => _id;

        public bool IsDeckConnected => _isDeckConnected;

        public bool IsServerConnected => _isServerConnected;

        public string RecorderName => _recorderName;

        public int DefaultChannel => _defaultChannel;

        public IWatcherDirectory RecordingDirectory => _recordingDirectory;

        public IMedia RecordingMedia => _recordingMedia;

        public TimeSpan TimeLimit => _timeLimit;

        public int ServerId => _serverId;

        public void Abort() { Invoke(); }

        public IMedia Capture(IPlayoutServerChannel channel, TimeSpan tcIn, TimeSpan tcOut, bool narrowMode, string mediaName, string fileName, int[] channelMap)
        {
            return Query<IMedia>(parameters: new object[] { channel, tcIn, tcOut, narrowMode, mediaName, fileName, channelMap });
        }

        public IMedia Capture(IPlayoutServerChannel channel, TimeSpan timeLimit, bool narrowMode, string mediaName, string fileName, int[] channelMap)
        {
            return Query<IMedia>(parameters: new object[] { channel, timeLimit, narrowMode, mediaName, fileName, channelMap });
        }

        public void DeckFastForward() { Invoke(); }

        public void GoToTimecode(TimeSpan tc, TVideoFormat format) { Invoke(parameters: new { tc, format }); }
        
        public void DeckRewind() { Invoke(); }

        public void DeckStop() { Invoke(); }

        public void DeckPlay() { Invoke(); }

        public void SetTimeLimit(TimeSpan value)
        {
            Invoke(parameters: new object[] {value});
        }

        public void Finish() { Invoke(); }

        public IMedia Capture(IPlayoutServerChannel channel, TimeSpan timeLimit, string fileName)
        {
            return Query<IMedia>(parameters: new object[] { channel, timeLimit, fileName });
        }

        #endregion IRecorder

        protected override void OnEventNotification(SocketMessage message) { }

    }
}
