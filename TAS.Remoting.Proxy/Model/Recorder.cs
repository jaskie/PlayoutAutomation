using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using TAS.Remoting.Client;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class Recorder : ProxyBase, IRecorder
    {
        #region IRecorder

        #pragma warning disable CS0649

        [JsonProperty(nameof(IRecorder.Channels))]
        private List<PlayoutServerChannel> _channels;

        [JsonProperty(nameof(IRecorder.RecordingDirectory))]
        private ServerDirectory _recordingDirectory;

        [JsonProperty(nameof(IRecorder.CurrentTc))]
        private TimeSpan _currentTc;

        [JsonProperty(nameof(IRecorder.DeckControl))]
        private TDeckControl _deckControl;

        [JsonProperty(nameof(IRecorder.DeckState))]
        private TDeckState _deckState;

        [JsonProperty(nameof(IRecorder.Id))]
        private int _id;

        [JsonProperty(nameof(IRecorder.IsDeckConnected))]
        private bool _isDeckConnected;

        [JsonProperty(nameof(IRecorder.IsServerConnected))]
        private bool _isServerConnected;

        [JsonProperty(nameof(IRecorder.RecorderName))]
        private string _recorderName;

        [JsonProperty(nameof(IRecorder.RecordingMedia))]
        private IMedia _recordingMedia;

        [JsonProperty(nameof(IRecorder.TimeLimit))]
        private TimeSpan _timeLimit;

        [JsonProperty(nameof(IRecorder.CaptureTimeLimit))]
        private TimeSpan _captureTimeLimit;

        [JsonProperty(nameof(IRecorder.CaptureTcIn))]
        private TimeSpan _captureTcIn;

        [JsonProperty(nameof(IRecorder.CaptureTcOut))]
        private TimeSpan _captureTcOut;

        [JsonProperty(nameof(IRecorder.CaptureNarrowMode))]
        private bool _captureNarrowMode;

        [JsonProperty(nameof(IRecorder.CaptureChannel))]
        private IPlayoutServerChannel _captureChannel;

        [JsonProperty(nameof(IRecorder.CaptureFileName))]
        private string _captureFileName;

        #pragma warning restore

        public IEnumerable<IPlayoutServerChannel> Channels => _channels;

        public TimeSpan CurrentTc => _currentTc;

        public TDeckControl DeckControl => _deckControl;

        public TDeckState DeckState => _deckState;

        public int Id => _id;

        public bool IsDeckConnected => _isDeckConnected;

        public bool IsServerConnected => _isServerConnected;

        public string RecorderName => _recorderName;

        public IMediaDirectory RecordingDirectory => _recordingDirectory;

        public IMedia RecordingMedia => _recordingMedia;

        public TimeSpan TimeLimit => _timeLimit;

        public TimeSpan CaptureTimeLimit
        {
            get { return _captureTimeLimit; }
            set { Set(value); }
        }

        public TimeSpan CaptureTcIn => _captureTcIn;

        public TimeSpan CaptureTcOut => _captureTcOut;

        public bool CaptureNarrowMode => _captureNarrowMode;

        public IPlayoutServerChannel CaptureChannel => _captureChannel;

        public string CaptureFileName => _captureFileName;

        public void Abort() { Invoke(); }

        public IMedia Capture(IPlayoutServerChannel channel, TimeSpan tcIn, TimeSpan tcOut, bool narrowMode, string mediaName, string fileName)
        {
            return Query<IMedia>(parameters: new object[] { channel, tcIn, tcOut, narrowMode, mediaName, fileName });
        }

        public IMedia Capture(IPlayoutServerChannel channel, TimeSpan timeLimit, bool narrowMode, string mediaName, string fileName)
        {
            return Query<IMedia>(parameters: new object[] { channel, timeLimit, narrowMode, mediaName, fileName });
        }

        public void DeckFastForward() { Invoke(); }

        public void GoToTimecode(TimeSpan tc, TVideoFormat format) { Invoke(parameters: new { tc, format }); }
        
        public void DeckRewind() { Invoke(); }

        public void DeckStop() { Invoke(); }

        public void DeckPlay() { Invoke(); }

        public void Finish() { Invoke(); }

        public IMedia Capture(IPlayoutServerChannel channel, TimeSpan timeLimit, string fileName)
        {
            return Query<IMedia>(parameters: new object[] { channel, timeLimit, fileName });
        }

        #endregion IRecorder

        protected override void OnEventNotification(WebSocketMessage e) { }

    }
}
