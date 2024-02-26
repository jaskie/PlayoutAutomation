using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using resources = TAS.Client.Common.Properties.Resources;


namespace TAS.Client.ViewModels
{
    public class RecordersViewmodel : ViewModelBase, IDataErrorInfo
    {
        private readonly IEngine _engine;
        private string _mediaName;
        private string _idAux;
        private IRecorder _selectedRecorder;
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
        private TAudioChannelMappingConversion _selectedAudioChannelMappingConversion;
        private bool _isAudioChannelMappingConversionVisible;

        public RecordersViewmodel(IEngine engine, IEnumerable<IRecorder> recorders)
        {
            _engine = engine;
            CreateCommands();
            Recorders = recorders;
            SelectedRecorder = Recorders.FirstOrDefault();
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
            get => _mediaName;
            set
            {
                if (SetField(ref _mediaName, value))
                    NotifyPropertyChanged(nameof(FileName));
            }
        }

        public string IdAux
        {
            get => _idAux;
            set
            {
                if (SetField(ref _idAux, value))
                    NotifyPropertyChanged(nameof(FileName));
            }
        }

        public string FileName => MediaExtensions.MakeFileName(IdAux, MediaName, $".{FileFormat}");

        public IRecorder SelectedRecorder
        {
            get => _selectedRecorder;
            set
            {
                var oldRecorder = _selectedRecorder;
                if (!SetField(ref _selectedRecorder, value))
                    return;
                if (oldRecorder != null)
                    oldRecorder.PropertyChanged -= Recorder_PropertyChanged;
                if (value != null)
                    value.PropertyChanged += Recorder_PropertyChanged;
                ResetDefaults();
            }
        }

        public IEnumerable<IRecorder> Recorders { get; }

        public IEnumerable<IPlayoutServerChannel> Channels
        {
            get => _channels;
            private set => SetField(ref _channels, value);
        }

        public IPlayoutServerChannel Channel
        {
            get => _channel;
            set
            {
                if (!SetField(ref _channel, value))
                    return;
                VideoFormat = value.VideoFormat;
                IsAudioChannelMappingConversionVisible = value.AudioChannelCount >= 4;
                FileFormat = value.MovieContainerFormat;
                NotifyPropertyChanged(nameof(FileName));
            }
        }

        public bool IsAudioChannelMappingConversionVisible
        {
            get => _isAudioChannelMappingConversionVisible;
            set => SetField(ref _isAudioChannelMappingConversionVisible, value);
        }

        public Array AudioChannelMappingConversions { get; } = new[]
        {
            TAudioChannelMappingConversion.FirstTwoChannels,
            TAudioChannelMappingConversion.FirstChannelOnly,
            TAudioChannelMappingConversion.SecondChannelOnly,
            TAudioChannelMappingConversion.SecondTwoChannels
        };

        public TAudioChannelMappingConversion SelectedAudioChannelMappingConversion
        {
            get => _selectedAudioChannelMappingConversion;
            set => SetField(ref _selectedAudioChannelMappingConversion, value);
        }

        public Array FileFormats { get; } = Enum.GetValues(typeof(TMovieContainerFormat));

        public TMovieContainerFormat FileFormat
        {
            get => _fileFormat;
            set
            {
                if (SetField(ref _fileFormat, value))
                    NotifyPropertyChanged(nameof(FileName));
            }
        }

        public bool IsNarrowMode
        {
            get => _isNarrowMode;
            set => SetField(ref _isNarrowMode, value);
        }

        public TimeSpan TcIn
        {
            get => _tcIn;
            set => SetField(ref _tcIn, value);
        }

        public TimeSpan TcOut
        {
            get => _tcOut;
            set => SetField(ref _tcOut, value);
        }

        public TimeSpan CurrentTc
        {
            get => _currentTc;
            set => SetField(ref _currentTc, value);
        }

        public TimeSpan TimeLimit
        {
            get => _timeLimit;
            set => SetField(ref _timeLimit, value);
        }

        public TimeSpan RecorderTimeLeft
        {
            get => _recorderTimeLeft;
            set => SetField(ref _recorderTimeLeft, value);
        }

        public TDeckState DeckState
        {
            get => _deckState;
            private set
            {
                if (SetField(ref _deckState, value))
                    InvalidateRequerySuggested();
            }
        }

        public TDeckControl DeckControl
        {
            get => _deckControl;
            private set
            {
                if (SetField(ref _deckControl, value))
                    InvalidateRequerySuggested();
            }
        }

        public TVideoFormat VideoFormat
        {
            get => _videoFormat;
            private set
            {
                if (!SetField(ref _videoFormat, value))
                    return;
                NotifyPropertyChanged(nameof(IsStandardDefinition));
                NotifyPropertyChanged(nameof(IsNarrowMode));
            }
        }

        public IMedia RecordingMedia
        {
            get => _selectedRecorder?.RecordingMedia;
            private set
            {
                var oldRecordMedia = _recordMedia;
                if (!SetField(ref _recordMedia, value))
                    return;
                if (oldRecordMedia != null)
                    oldRecordMedia.PropertyChanged -= RecordMedia_PropertyChanged;
                if (value != null)
                    value.PropertyChanged += RecordMedia_PropertyChanged;
                InvalidateRequerySuggested();
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
                        if (string.IsNullOrWhiteSpace(MediaName))
                            return resources._validate_FileNameEmpty;
                        if (_engine.ServerMediaFieldLengths.TryGetValue(nameof(IServerMedia.MediaName), out var mnLength) && MediaName.Length > mnLength)
                            return resources._validate_TextTooLong;
                        break;
                    case nameof(FileName):
                        if (string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(FileName)))
                            return resources._validate_FileNameEmpty;
                        if (_selectedRecorder?.RecordingDirectory.FileExists(FileName) == true)
                            return resources._validate_FileAlreadyExists;
                        if (_engine.ServerMediaFieldLengths.TryGetValue(nameof(IServerMedia.FileName), out var fnLength) && FileName.Length > fnLength)
                            return resources._validate_TextTooLong;
                        break;
                }
                return string.Empty;
            }
        }

        public string Error => null;

        protected override void OnDispose()
        {
            SelectedRecorder = null; // ensure that events are disconnected
            RecordingMedia = null;
        }

        private void Recorder_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IRecorder.CurrentTc))
                OnUiThread(() => CurrentTc = ((IRecorder)sender).CurrentTc);
            if (e.PropertyName == nameof(IRecorder.DeckControl))
                OnUiThread(() => DeckControl = ((IRecorder)sender).DeckControl);
            if (e.PropertyName == nameof(IRecorder.DeckState))
                OnUiThread(() => DeckState = ((IRecorder)sender).DeckState);
            if (e.PropertyName == nameof(IRecorder.IsDeckConnected)
                || e.PropertyName == nameof(IRecorder.IsServerConnected))
                NotifyPropertyChanged(null);
            if (e.PropertyName == nameof(IRecorder.TimeLimit))
                OnUiThread(() => RecorderTimeLeft = ((IRecorder)sender).TimeLimit);
            if (e.PropertyName == nameof(IRecorder.RecordingMedia))
                RecordingMedia = ((IRecorder)sender).RecordingMedia;
        }

        private void RecordMedia_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(IMedia.MediaStatus))
                return;
            NotifyPropertyChanged(nameof(CommandStartRecord));
            NotifyPropertyChanged(nameof(CommandCapture));
            NotifyPropertyChanged(nameof(CommandRecordFinish));
            OnUiThread(ResetDefaults);
        }

        private void CreateCommands()
        {
            CommandFastForward = new UiCommand(FastForward, o => CanExecute(TDeckState.ShuttleForward));
            CommandRewind = new UiCommand(Rewind, CanRewind);
            CommandPlay = new UiCommand(Play, o => CanExecute(TDeckState.Playing));
            CommandStop = new UiCommand(Stop, o => CanExecute(TDeckState.Stopped));
            CommandCapture = new UiCommand(Capture, CanCapture);
            CommandStartRecord = new UiCommand(StartRecord, CanStartRecord);
            CommandGetCurrentTcToIn = new UiCommand(o => TcIn = CurrentTc);
            CommandGetCurrentTcToOut = new UiCommand(o => TcOut = CurrentTc);
            CommandGoToTimecode = new UiCommand(GoToTimecode, CanGoToTimecode);
            CommandSetRecordLimit = new UiCommand(SetRecordTimeLimit, CanSetRecordTimeLimit);
            CommandRecordFinish = new UiCommand(FinishRecord, CanFinishRecord);
        }

        private bool CanFinishRecord(object obj)
        {
            return _selectedRecorder != null && _recordMedia?.MediaStatus == TMediaStatus.Copying;
        }

        private void FinishRecord(object obj)
        {
            _selectedRecorder.Finish();
            RecordingMedia = null;
        }

        private void StartRecord(object obj)
        {
            RecordingMedia = _selectedRecorder?.Capture(_channel, _timeLimit, IsNarrowMode, MediaName, FileName, GetChannelMap());
        }

        private bool CanStartRecord(object obj)
        {
            return _channel != null
                   && _selectedRecorder?.IsServerConnected == true
                   && _timeLimit > TimeSpan.FromSeconds(1)
                   && _recordMedia?.MediaStatus != TMediaStatus.Copying
                   && !string.IsNullOrEmpty(MediaName)
                   && !_selectedRecorder.RecordingDirectory.FileExists(FileName)
                   && _selectedRecorder.RecordingMedia == null;
        }

        private bool CanSetRecordTimeLimit(object obj)
        {
            return _selectedRecorder?.RecordingMedia != null;
        }

        private void SetRecordTimeLimit(object obj)
        {
            _selectedRecorder.SetTimeLimit(TimeLimit);
        }

        private void GoToTimecode(object obj)
        {
            _selectedRecorder.GoToTimecode(_currentTc, _channel.VideoFormat);
        }

        private bool CanGoToTimecode(object obj)
        {
            IRecorder recorder = _selectedRecorder;
            return recorder != null && recorder.IsDeckConnected && _channel != null;
        }

        private void Rewind(object obj)
        {
            _selectedRecorder?.DeckRewind();
        }

        private bool CanRewind(object obj)
        {
            return CanExecute(TDeckState.ShuttleForward);
        }

        private void Capture(object obj)
        {
            RecordingMedia = _selectedRecorder.Capture(_channel, TcIn, TcOut, IsNarrowMode, MediaName, FileName, GetChannelMap());
        }

        private int[] GetChannelMap()
        {
            if (!IsAudioChannelMappingConversionVisible)
                return null;
            switch (SelectedAudioChannelMappingConversion)
            {
                case TAudioChannelMappingConversion.Default:
                case TAudioChannelMappingConversion.FirstTwoChannels:
                    return new[] {0, 1};
                case TAudioChannelMappingConversion.FirstChannelOnly:
                    return new[] {0, 0};
                case TAudioChannelMappingConversion.SecondChannelOnly:
                    return new[] {1, 1};
                case TAudioChannelMappingConversion.SecondTwoChannels:
                    return new[] {2, 3};
                default:
                    throw new NotSupportedException("Invalid audio channel mapping");
            }
        }

        private bool CanCapture(object obj)
        {
            return _channel != null && _tcOut > _tcIn
                   && _selectedRecorder?.IsServerConnected == true
                   && _selectedRecorder.IsDeckConnected
                   && _recordMedia?.MediaStatus != TMediaStatus.Copying
                   && !string.IsNullOrEmpty(MediaName)
                   && !_selectedRecorder.RecordingDirectory.FileExists(FileName);
        }

        private bool CanExecute(TDeckState state)
        {
            IRecorder recorder = _selectedRecorder;
            return recorder != null && recorder.IsDeckConnected && recorder.DeckState != state;
        }

        private void Stop(object obj)
        {
            _selectedRecorder?.DeckStop();
            RecordingMedia = null;
        }

        private void Play(object obj)
        {
            _selectedRecorder?.DeckPlay();
        }

        private void FastForward(object obj)
        {
            _selectedRecorder?.DeckFastForward();
        }

        private void ResetDefaults()
        {
            if (_selectedRecorder == null)
                return;
            Channels = _selectedRecorder.Channels.ToList();
            CurrentTc = _selectedRecorder.CurrentTc;
            TimeLimit = TimeSpan.FromHours(2);
            Channel = Channels.FirstOrDefault(c => c.Id == _selectedRecorder.DefaultChannel) ?? Channels.LastOrDefault();
            RecorderTimeLeft = _selectedRecorder.TimeLimit;
            DeckState = _selectedRecorder.DeckState;
            DeckControl = _selectedRecorder.DeckControl;
            FileFormat = Channel?.MovieContainerFormat ?? TMovieContainerFormat.mov;
            RecordingMedia = _selectedRecorder.RecordingMedia;
            IsNarrowMode = false;
            SelectedAudioChannelMappingConversion = TAudioChannelMappingConversion.FirstTwoChannels;
            NotifyPropertyChanged(nameof(FileName));
        }

    }
}