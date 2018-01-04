using Newtonsoft.Json;
using Svt.Caspar;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TAS.Remoting.Server;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Server.Media;

namespace TAS.Server
{

    public class CasparRecorder: DtoBase, IRecorder
    {
        private TVideoFormat _tcFormat = TVideoFormat.PAL;
        private Recorder _recorder;
        private IMedia _recordingMedia;
        internal IArchiveDirectory ArchiveDirectory;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(CasparRecorder));
        private CasparServer _ownerServer;

        private TimeSpan _currentTc;
        private TimeSpan _timeLimit;
        private TDeckState _deckState;
        private TDeckControl _deckControl;
        private bool _isDeckConnected;
        private bool _isServerConnected;
        private string _recorderName;

        #region Deserialized properties
        public int RecorderNumber { get; set; }
        public int Id { get; set; }

        [JsonProperty]
        public string RecorderName
        {
            get { return _recorderName; }
            set { SetField(ref _recorderName, value); }
        }

        public int DefaultChannel { get; set; }

        #endregion Deserialized properties

        #region IRecorder
        [JsonProperty, XmlIgnore]
        public TimeSpan CurrentTc { get { return _currentTc; }  private set { SetField(ref _currentTc, value); } }

        [JsonProperty, XmlIgnore]
        public TimeSpan TimeLimit { get { return _timeLimit; }  private set { SetField(ref _timeLimit, value); } }

        [JsonProperty, XmlIgnore]
        public TDeckState DeckState { get { return _deckState; } private set { SetField(ref _deckState, value); } }

        [JsonProperty, XmlIgnore]
        public TDeckControl DeckControl { get { return _deckControl; }  private set { SetField(ref _deckControl, value); } }

        [JsonProperty, XmlIgnore]
        public bool IsDeckConnected { get { return _isDeckConnected; } private set { SetField(ref _isDeckConnected, value); } }

        [JsonProperty, XmlIgnore]
        public bool IsServerConnected { get { return _isServerConnected; } internal set { SetField(ref _isServerConnected, value); } }

        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.Objects), XmlIgnore]
        public IEnumerable<IPlayoutServerChannel> Channels => _ownerServer.Channels;

        [JsonProperty, XmlIgnore]
        public IMedia RecordingMedia { get { return _recordingMedia; } private set { SetField(ref _recordingMedia, value); } }

        [JsonProperty, XmlIgnore]
        public IMediaDirectory RecordingDirectory => _ownerServer.MediaDirectory;
        
        public IMedia Capture(IPlayoutServerChannel channel, TimeSpan tcIn, TimeSpan tcOut, bool narrowMode, string mediaName, string fileName)
        {
            _tcFormat = channel.VideoFormat;
            var directory = (ServerDirectory)_ownerServer.MediaDirectory;
            var newMedia = new ServerMedia(directory, Guid.NewGuid(), 0, ArchiveDirectory)
            {
                FileName = fileName,
                MediaName = mediaName,
                TcStart = tcIn,
                TcPlay = tcIn,
                Duration = tcOut - tcIn,
                MediaStatus = TMediaStatus.Copying,
                LastUpdated = DateTime.UtcNow,
                MediaType = TMediaType.Movie
            };
            if (_recorder?.Capture(channel.Id, tcIn.ToSMPTETimecodeString(channel.VideoFormat), tcOut.ToSMPTETimecodeString(channel.VideoFormat), narrowMode, fileName) == true)
            {
                RecordingMedia = newMedia;
                Logger.Debug("Started recording from {0} file {1} TcIn {2} TcOut {3}", channel.ChannelName, fileName, tcIn, tcOut);
                return newMedia;
            }
            Logger.Error("Unsuccessfull recording from {0} file {1} TcIn {2} TcOut {3}", channel.ChannelName, fileName, tcIn, tcOut);
            return null;
        }

        public IMedia Capture(IPlayoutServerChannel channel, TimeSpan timeLimit, bool narrowMode, string mediaName, string fileName)
        {
            _tcFormat = channel.VideoFormat;
            var directory = (ServerDirectory)_ownerServer.MediaDirectory;
            var newMedia = new ServerMedia(directory, Guid.NewGuid(), 0, ArchiveDirectory)
            {
                FileName = fileName,
                MediaName = mediaName,
                TcStart = TimeSpan.Zero,
                TcPlay =TimeSpan.Zero,
                Duration = timeLimit,
                MediaStatus = TMediaStatus.Copying,
                LastUpdated = DateTime.UtcNow,
                MediaType = TMediaType.Movie
            };
            if (_recorder?.Capture(channel.Id,  timeLimit.ToSMPTEFrames(channel.VideoFormat), narrowMode, fileName) == true)
            {
                RecordingMedia = newMedia;
                Logger.Debug("Started recording from {0} file {1} with time limit {2} ", channel.ChannelName, fileName, timeLimit);
                return newMedia;
            }
            Logger.Error("Unsuccessfull recording from {0} file {1} with time limit {2}", channel.ChannelName, fileName, timeLimit);
            return null;
        }

        public void SetTimeLimit(TimeSpan value)
        {
            var media = RecordingMedia;
            if (media != null)
                _recorder?.SetTimeLimit(value.ToSMPTEFrames(media.VideoFormat));
        }

        public void Finish()
        {
            _recorder?.Finish();
        }
        
        public void Abort()
        {
            _recorder?.Abort();
        }

        public void DeckPlay()
        {
            _recorder?.Play();
        }
        public void DeckStop()
        {
            _recorder?.Stop();
        }

        public void DeckFastForward()
        {
            _recorder.FastForward();
        }

        public void DeckRewind()
        {
            _recorder.Rewind();
        }

        public void GoToTimecode(TimeSpan tc, TVideoFormat format)
        {
            _recorder?.GotoTimecode(tc.ToSMPTETimecodeString(format));
        }

        #endregion IRecorder

        internal void SetRecorder(Recorder value)
        {
            var oldRecorder = _recorder;
            if (_recorder != value)
            {
                if (oldRecorder != null)
                {
                    oldRecorder.Tc -= _recorder_Tc;
                    oldRecorder.FramesLeft -= _recorder_FramesLeft;
                    oldRecorder.DeckConnected -= _recorder_DeckConnected;
                    oldRecorder.DeckControl -= _recorder_DeckControl;
                    oldRecorder.DeckState -= _recorder_DeckState;
                }
                _recorder = value;
                if (value != null)
                {
                    value.Tc += _recorder_Tc;
                    value.DeckConnected += _recorder_DeckConnected;
                    value.DeckControl += _recorder_DeckControl;
                    value.DeckState += _recorder_DeckState;
                    value.FramesLeft += _recorder_FramesLeft;
                    IsDeckConnected = value.IsConnected;
                    DeckState = TDeckState.Unknown;
                    DeckControl = TDeckControl.None;
                }
            }
        }

        internal void SetOwner(CasparServer owner)
        {
            _ownerServer = owner;
        }

        internal event EventHandler<MediaEventArgs> CaptureSuccess;

        private void _recorder_FramesLeft(object sender, FramesLeftEventArgs e)
        {
            var media = _recordingMedia;
            if (media != null)
                TimeLimit = e.FramesLeft.SMPTEFramesToTimeSpan(media.VideoFormat);
        }

        private void _recorder_DeckState(object sender, DeckStateEventArgs e)
        {
            DeckState = (TDeckState)e.State;
        }

        private void _recorder_DeckControl(object sender, DeckControlEventArgs e)
        {
            if (e.ControlEvent == Svt.Caspar.DeckControl.capture_complete)
                _captureCompleted();
            if (e.ControlEvent == Svt.Caspar.DeckControl.aborted)
                _captureAborted();
            DeckControl = (TDeckControl)e.ControlEvent;
        }

        private void _captureAborted()
        {
            _recordingMedia?.Delete();
            RecordingMedia = null;
            Logger.Trace("Capture aborted notified");
        }

        private void _captureCompleted()
        {
            var media = _recordingMedia;
            if (media?.MediaStatus == TMediaStatus.Copying)
            {
                media.MediaStatus = TMediaStatus.Copied;
                Task.Run(() =>
                {
                    Thread.Sleep(500);
                    media.Verify();
                    if (media.MediaStatus == TMediaStatus.Available)
                        CaptureSuccess?.Invoke(this, new MediaEventArgs(media));
                });
            }
            Logger.Trace("Capture completed notified");
        }

        private void _recorder_DeckConnected(object sender, DeckConnectedEventArgs e)
        {
            IsDeckConnected = e.IsConnected;
            Logger.Trace("Deck {0}", e.IsConnected ? "connected" : "disconnected");
        }

        private void _recorder_Tc(object sender, TcEventArgs e)
        {
            if (e.Tc.IsValidSMPTETimecode(_tcFormat))
                CurrentTc = e.Tc.SMPTETimecodeToTimeSpan(_tcFormat);
        }

    }
}
