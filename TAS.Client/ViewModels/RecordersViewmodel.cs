using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;
using resources = TAS.Client.Common.Properties.Resources;


namespace TAS.Client.ViewModels
{
    public class RecordersViewmodel : ViewmodelBase, IDataErrorInfo
    {
        private string _mediaName;
        private string _idAux;
        private IRecorder _recorder;
        private IEnumerable<IPlayoutServerChannel> _channels;
        private IPlayoutServerChannel _channel;
        private TimeSpan _tcIn;
        private bool _isNarrowMode;
        private TimeSpan _tcOut;
        private TimeSpan _currentTc;
        private TimeSpan _timeLimit;
        private IMedia _recordMedia;
        private TVideoFormat _videoFormat;
        private TDeckControl _deckControl;
        private TDeckState _deckState;
        private TimeSpan _recorderTimeLeft;
        private TMovieContainerFormat _fileFormat;

        public RecordersViewmodel(IEnumerable<IRecorder> recorders)
        {
            CreateCommands();
            Recorders = recorders;
            Recorder = Recorders.FirstOrDefault();
        }
        
        public ICommand CommandPlay { get; private set; }
        public ICommand CommandStop { get; private set; }
        public ICommand CommandFastForward { get; private set; }
        public ICommand CommandRewind { get; private set; }
        public ICommand CommandCapture { get; private set; }
        public ICommand CommandStartRecord { get; private set; }
        public ICommand CommandGetCurrentTcToIn { get; private set; }
        public ICommand CommandGetCurrentTcToOut { get; private set; }
        public ICommand CommandGoToTimecode { get; private set; }
        public ICommand CommandRecordFinish { get; private set; }
        public ICommand CommandSetRecordLimit { get; private set; }

        public string MediaName
        {
            get { return _mediaName; }
            set
            {
                if (SetField(ref _mediaName, value))
                    NotifyPropertyChanged(nameof(FileName));
            }
        }

        public string IdAux
        {
            get { return _idAux; }
            set
            {
                if (SetField(ref _idAux, value))
                    NotifyPropertyChanged(nameof(FileName));
            }
        }

        public string FileName => MediaExtensions.MakeFileName(IdAux, MediaName, $".{FileFormat}");

        public IRecorder Recorder
        {
            get { return _recorder; }
            set
            {
                var oldRecorder = _recorder;
                if (SetField(ref _recorder, value))
                {
                    if (oldRecorder != null)
                        oldRecorder.PropertyChanged -= Recorder_PropertyChanged;
                    if (value != null)
                        value.PropertyChanged += Recorder_PropertyChanged;
                    ResetDefaults();
                }
            }
        }

        public IEnumerable<IRecorder> Recorders { get; }

        public IEnumerable<IPlayoutServerChannel> Channels
        {
            get { return _channels; }
            private set { SetField(ref _channels, value); }
        }

        public IPlayoutServerChannel Channel
        {
            get { return _channel; }
            set
            {
                if (SetField(ref _channel, value))
                    VideoFormat = value.VideoFormat;
            }
        }

        public Array FileFormats { get; } = Enum.GetValues(typeof(TMovieContainerFormat));

        public TMovieContainerFormat FileFormat
        {
            get { return _fileFormat; }
            set
            {
                if (SetField(ref _fileFormat, value))
                    NotifyPropertyChanged(nameof(FileName));
            }
        }

        public bool IsNarrowMode
        {
            get { return _isNarrowMode; }
            set { SetField(ref _isNarrowMode, value); }
        }

        public TimeSpan TcIn
        {
            get { return _tcIn; }
            set { SetField(ref _tcIn, value); }
        }

        public TimeSpan TcOut
        {
            get { return _tcOut; }
            set { SetField(ref _tcOut, value); }
        }

        public TimeSpan CurrentTc
        {
            get { return _currentTc; }
            set { SetField(ref _currentTc, value); }
        }

        public TimeSpan TimeLimit
        {
            get { return _timeLimit; }
            set { SetField(ref _timeLimit, value); }
        }

        public TimeSpan RecorderTimeLeft
        {
            get { return _recorderTimeLeft; }
            private set { SetField(ref _recorderTimeLeft, value); }
        }

        public TDeckState DeckState
        {
            get { return _deckState; }
            private set
            {
                if (SetField(ref _deckState, value))
                    InvalidateRequerySuggested();
            }
        }

        public TDeckControl DeckControl
        {
            get { return _deckControl; }
            private set
            {
                if (SetField(ref _deckControl, value))
                    InvalidateRequerySuggested();
            }
        }

        public TVideoFormat VideoFormat
        {
            get { return _videoFormat; }
            private set
            {
                if (SetField(ref _videoFormat, value))
                {
                    NotifyPropertyChanged(nameof(IsStandardDefinition));
                    NotifyPropertyChanged(nameof(IsNarrowMode));
                }
            }
        }

        public IMedia RecordingMedia
        {
            get { return _recorder?.RecordingMedia; }
            private set
            {
                var oldRecordMedia = _recordMedia;
                if (SetField(ref _recordMedia, value))
                {
                    if (oldRecordMedia != null)
                        oldRecordMedia.PropertyChanged -= RecordMedia_PropertyChanged;
                    if (value != null)
                        value.PropertyChanged += RecordMedia_PropertyChanged;
                    InvalidateRequerySuggested();
                }
            }
        }

        public bool IsStandardDefinition => _videoFormat < TVideoFormat.HD720p2500;

        public string this[string propertyName]
        {
            get
            {
                switch (propertyName)
                {
                    case nameof(MediaName):
                        return string.IsNullOrWhiteSpace(MediaName)
                            ? resources._validate_FileNameEmpty
                            : string.Empty;
                    case nameof(FileName):
                        return 
                            string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(FileName)) || _recorder?.RecordingDirectory.FileExists(FileName) == true
                            ? resources._validate_FileAlreadyExists
                            : string.Empty;
                }
                return string.Empty;
            }
        }

        public string Error => null;

        protected override void OnDispose()
        {
            Recorder = null; // ensure that events are disconnected
            RecordingMedia = null;
        }

        private void Recorder_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var appInstance = Application.Current;
            if (appInstance == null)
                return;
            if (e.PropertyName == nameof(IRecorder.CurrentTc))
                appInstance.Dispatcher.BeginInvoke((Action) (() => CurrentTc = ((IRecorder) sender).CurrentTc));
            if (e.PropertyName == nameof(IRecorder.DeckControl))
                appInstance.Dispatcher.BeginInvoke((Action) (() => DeckControl = ((IRecorder) sender).DeckControl));
            if (e.PropertyName == nameof(IRecorder.DeckState))
                appInstance.Dispatcher.BeginInvoke((Action) (() => DeckState = ((IRecorder) sender).DeckState));
            if (e.PropertyName == nameof(IRecorder.IsDeckConnected)
                || e.PropertyName == nameof(IRecorder.IsServerConnected))
                NotifyPropertyChanged(null);
            if (e.PropertyName == nameof(IRecorder.TimeLimit))
                appInstance.Dispatcher.BeginInvoke((Action) (() => RecorderTimeLeft = ((IRecorder) sender).TimeLimit));
        }

        private void RecordMedia_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(IMedia.MediaStatus))
                return;
            NotifyPropertyChanged(nameof(CommandStartRecord));
            NotifyPropertyChanged(nameof(CommandCapture));
            NotifyPropertyChanged(nameof(CommandRecordFinish));
            Application.Current.Dispatcher.BeginInvoke((Action)ResetDefaults);
        }

        private void CreateCommands()
        {
            CommandFastForward = new UICommand
            {
                ExecuteDelegate = FastForward,
                CanExecuteDelegate = o => CanExecute(TDeckState.ShuttleForward)
            };
            CommandRewind = new UICommand {ExecuteDelegate = Rewind, CanExecuteDelegate = CanRewind};
            CommandPlay = new UICommand
            {
                ExecuteDelegate = Play,
                CanExecuteDelegate = o => CanExecute(TDeckState.Playing)
            };
            CommandStop = new UICommand
            {
                ExecuteDelegate = Stop,
                CanExecuteDelegate = o => CanExecute(TDeckState.Stopped)
            };
            CommandCapture = new UICommand {ExecuteDelegate = Capture, CanExecuteDelegate = CanCapture};
            CommandStartRecord = new UICommand {ExecuteDelegate = StartRecord, CanExecuteDelegate = CanStartRecord};
            CommandGetCurrentTcToIn = new UICommand {ExecuteDelegate = o => TcIn = CurrentTc};
            CommandGetCurrentTcToOut = new UICommand {ExecuteDelegate = o => TcOut = CurrentTc};
            CommandGoToTimecode = new UICommand {ExecuteDelegate = GoToTimecode, CanExecuteDelegate = CanGoToTimecode};
            CommandSetRecordLimit = new UICommand
            {
                ExecuteDelegate = SetRecordTimeLimit,
                CanExecuteDelegate = CanSetRecordTimeLimit
            };
            CommandRecordFinish = new UICommand {ExecuteDelegate = FinishRecord, CanExecuteDelegate = CanFinishRecord};
        }

        private bool CanFinishRecord(object obj)
        {
            return _recorder != null && _recordMedia?.MediaStatus == TMediaStatus.Copying;
        }

        private void FinishRecord(object obj)
        {
            _recorder.Finish();
        }

        private void StartRecord(object obj)
        {
            RecordingMedia = _recorder?.Capture(_channel, _timeLimit, IsNarrowMode, MediaName, FileName);
        }

        private bool CanStartRecord(object obj)
        {
            return _channel != null
                   && _recorder?.IsServerConnected == true
                   && _timeLimit > TimeSpan.FromSeconds(1)
                   && _recordMedia?.MediaStatus != TMediaStatus.Copying
                   && !string.IsNullOrEmpty(MediaName)
                   && !_recorder.RecordingDirectory.FileExists(FileName);
        }

        private bool CanSetRecordTimeLimit(object obj)
        {
            return _recorder?.RecordingMedia != null;
        }

        private void SetRecordTimeLimit(object obj)
        {
            _recorder.SetTimeLimit(TimeLimit);
        }

        private void GoToTimecode(object obj)
        {
            _recorder.GoToTimecode(_currentTc, _channel.VideoFormat);
        }

        private bool CanGoToTimecode(object obj)
        {
            IRecorder recorder = _recorder;
            return recorder != null && recorder.IsDeckConnected && _channel != null;
        }

        private void Rewind(object obj)
        {
            _recorder?.DeckRewind();
        }

        private bool CanRewind(object obj)
        {
            return CanExecute(TDeckState.ShuttleForward);
        }

        private void Capture(object obj)
        {
            RecordingMedia = _recorder.Capture(_channel, TcIn, TcOut, IsNarrowMode, MediaName, FileName);
        }

        private bool CanCapture(object obj)
        {
            return _channel != null && _tcOut > _tcIn
                   && _recorder?.IsServerConnected == true
                   && _recorder.IsDeckConnected 
                   && _recordMedia?.MediaStatus != TMediaStatus.Copying
                   && !string.IsNullOrEmpty(MediaName)
                   && !_recorder.RecordingDirectory.FileExists(FileName);
        }

        private bool CanExecute(TDeckState state)
        {
            IRecorder recorder = _recorder;
            return recorder != null && recorder.IsDeckConnected && recorder.DeckState != state;
        }

        private void Stop(object obj)
        {
            _recorder?.DeckStop();
        }

        private void Play(object obj)
        {
            _recorder?.DeckPlay();
        }

        private void FastForward(object obj)
        {
            _recorder?.DeckFastForward();
        }

        private void ResetDefaults()
        {
            if (_recorder == null)
                return;
            Channels = _recorder.Channels;
            CurrentTc = _recorder.CurrentTc;
            TimeLimit = TimeSpan.FromHours(2);
            Channel = Channels.ElementAtOrDefault(_recorder.DefaultChannel) ?? Channels.LastOrDefault();
            RecorderTimeLeft = _recorder.TimeLimit;
            DeckState = _recorder.DeckState;
            DeckControl = _recorder.DeckControl;
            FileFormat = TMovieContainerFormat.mov;
            RecordingMedia = _recorder.RecordingMedia;
            IsNarrowMode = false;
        }

    }
}