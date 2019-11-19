using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.ObjectModel;
using TAS.Client.Common;
using System.Windows.Input;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Client.ViewModels
{
    public class PreviewViewmodel : ViewModelBase
    {
        private IMedia _selectedMedia;
        private IEvent _selectedEvent;
        private readonly IPreview _preview;
        private readonly bool _canTrimMedia;
        private readonly IPlayoutServerChannel _channel;
        private IMediaSegment _lastAddedSegment;
        private bool _playWholeClip;
        private IMedia _loadedMedia;
        private long _loadedDuration;
        private TimeSpan _tcIn;
        private TimeSpan _tcOut;
        private string _segmentName;
        private MediaSegmentViewmodel _selectedSegment;
        private bool _isSegmentsVisible;
        private TimeSpan _duration;
        private TimeSpan _startTc;
        private IIngestOperation _selectedIngestOperation;
        private bool _isStillButtonsVisible;
        private static readonly TimeSpan EndDuration = TimeSpan.FromSeconds(3);

        public PreviewViewmodel(IPreview preview, bool canTrimMedia)
        {
            preview.PropertyChanged += PreviewPropertyChanged;
            _channel = preview.Channel;
            if (_channel != null)
                _channel.PropertyChanged += OnChannelPropertyChanged;
            _preview = preview;
            _canTrimMedia = canTrimMedia;
            FormatDescription = _preview.FormatDescription;
            CreateCommands();
        }

        public VideoFormatDescription FormatDescription { get; }

        public TVideoFormat VideoFormat => FormatDescription.Format;

        public IMedia SelectedMedia
        {
            get => _selectedMedia;
            set
            {
                var oldVal = _selectedMedia;
                var newVal = value;
                if (!SetField(ref _selectedMedia, newVal))
                    return;
                if (oldVal != null)
                    oldVal.PropertyChanged -= Media_PropertyChanged;
                if (newVal != null)
                    newVal.PropertyChanged += Media_PropertyChanged;
                InvalidateRequerySuggested();
            }
        }

        public IEvent SelectedEvent
        {
            get => _selectedEvent;
            set
            {
                var oldEvent = _selectedEvent;
                if (!SetField(ref _selectedEvent, value))
                    return;
                if (oldEvent != null)
                    oldEvent.PropertyChanged -= Event_PropertyChanged;
                if (value != null)
                    value.PropertyChanged += Event_PropertyChanged;
                InvalidateRequerySuggested();
            }
        }

        public IIngestOperation SelectedIngestOperation
        {
            get => _selectedIngestOperation;
            set
            {
                if (SetField(ref _selectedIngestOperation, value))
                    InvalidateRequerySuggested();
            }
        }


        public IMedia LoadedMedia
        {
            get => _loadedMedia;
            private set
            {
                var pm = _loadedMedia as IPersistentMedia;
                if (SetField(ref _loadedMedia, value))
                {
                    if (pm != null)
                    {
                        pm.GetMediaSegments().SegmentAdded -= _mediaSegments_SegmentAdded;
                        pm.GetMediaSegments().SegmentRemoved -= _mediaSegments_SegmentRemoved;
                    }
                    pm = value as IPersistentMedia;
                    if (pm != null)
                    {
                        pm.GetMediaSegments().SegmentAdded += _mediaSegments_SegmentAdded;
                        pm.GetMediaSegments().SegmentRemoved += _mediaSegments_SegmentRemoved;
                    }
                    NotifyPropertyChanged(nameof(IsLoaded));
                }
            }
        }

        public TimeSpan StartTc
        {
            get => _startTc;
            set => SetField(ref _startTc, value);
        }

        public TimeSpan Duration
        {
            get => _duration;
            set => SetField(ref _duration, value);
        }

        public bool IsSegmentsVisible
        {
            get => _isSegmentsVisible;
            set => SetField(ref _isSegmentsVisible, value);
        }

        public bool IsSegmentsEnabled => _preview.Media is IServerMedia;

        public TimeSpan TcIn
        {
            get => _tcIn;
            set
            {
                if (SetField(ref _tcIn, value))
                    InvalidateRequerySuggested();
            }
        }

        public TimeSpan TcOut
        {
            get => _tcOut;
            set
            {
                if (SetField(ref _tcOut, value))
                    InvalidateRequerySuggested();
            }
        }

        public TimeSpan Position
        {
            get => _loadedMedia == null || _preview.Media == null ? TimeSpan.Zero : (_preview.Position + _preview.LoadedSeek).SmpteFramesToTimeSpan(FormatDescription.FrameRate) + _loadedMedia.TcStart;
            set
            {
                if (_loadedMedia == null)
                    return;
                _preview.Position = (value - _loadedMedia.TcStart).ToSmpteFrames(FormatDescription.FrameRate);
            }
        }

        public bool IsLoaded => LoadedMedia != null;

        public long LoadedDuration
        {
            get => _loadedDuration;
            private set
            {
                if (SetField(ref _loadedDuration, value))
                    NotifyPropertyChanged(nameof(SliderTickFrequency));
            }
        }

        public long SliderTickFrequency => _loadedDuration / 50;

        public long SliderPosition
        {
            get => _loadedMedia == null || _preview.Media == null ? 0 : _preview.Position;
            set
            {
                if (_loadedMedia == null)
                    return;
                _preview.Position = value;
            }
        }

        public long FramesPerSecond => FormatDescription.FrameRate.Num / FormatDescription.FrameRate.Den;

        public ObservableCollection<MediaSegmentViewmodel> MediaSegments { get; } = new ObservableCollection<MediaSegmentViewmodel>();

        public MediaSegmentViewmodel SelectedSegment
        {
            get => _selectedSegment;
            set
            {
                MediaSegmentViewmodel oldValue = _selectedSegment;
                if (oldValue != value)
                {
                    if (oldValue != null)
                        oldValue.PropertyChanged -= SegmentPropertyChanged;
                    _selectedSegment = value;
                    if (value != null)
                    {
                        value.PropertyChanged += SegmentPropertyChanged;
                        SegmentName = value.SegmentName;
                    }
                    else
                        SegmentName = string.Empty;
                    NotifyPropertyChanged(nameof(SelectedSegment));
                    InvalidateRequerySuggested();
                    _mediaLoad(_loadedMedia, false);
                }
            }
        }

        public string SegmentName
        {
            get => _segmentName;
            set
            {
                if (SetField(ref _segmentName, value))
                    InvalidateRequerySuggested();
            }
        }

        public bool IsEnabled => _channel?.IsServerConnected == true;

        public bool IsSegmentNameFocused { get; set; }

        public bool PlayWholeClip
        {
            get => _playWholeClip;
            set
            {
                if (SetField(ref _playWholeClip, value))
                    _mediaLoad(_loadedMedia, false);
            }
        }

        public bool IsStillButtonsVisible { get => _isStillButtonsVisible; private set => SetField(ref _isStillButtonsVisible, value); }

        #region Commands

        public ICommand CommandPause { get; private set; }
        public ICommand CommandPlay { get; private set; }
        public ICommand CommandPlayTheEnd { get; private set; }
        public ICommand CommandUnload { get; private set; }
        public ICommand CommandSeek { get; private set; }
        public ICommand CommandCopyToTcIn { get; private set; }
        public ICommand CommandCopyToTcOut { get; private set; }
        public ICommand CommandSaveSegment { get; private set; }
        public ICommand CommandDeleteSegment { get; private set; }
        public ICommand CommandNewSegment { get; private set; }
        public ICommand CommandSetSegmentNameFocus { get; private set; }
        public ICommand CommandTrimSource { get; private set; }

        public ICommand CommandFastForward { get; private set; }
        public ICommand CommandBackward { get; private set; }
        public ICommand CommandFastForwardOneFrame { get; private set; }
        public ICommand CommandBackwardOneFrame { get; private set; }

        public ICommand CommandToggleLayer { get; private set; }



        private void CreateCommands()
        {
            CommandPause = new UiCommand
            (
                o =>
                    {
                        if (LoadedMedia == null)
                            _mediaLoad(MediaToLoad, true);
                        else
                            if (_preview.IsPlaying)
                            _preview.Pause();
                        else
                        {
                            _preview.Position = 0;
                            NotifyPropertyChanged(nameof(Position));
                        }
                    },
                o => LoadedMedia?.MediaStatus == TMediaStatus.Available
                                          || _canLoad(MediaToLoad)
            );
            CommandPlay = new UiCommand
            (
                o =>
                    {
                        if (LoadedMedia != null)
                        {
                            if (_preview.IsPlaying)
                                _preview.Pause();
                            else
                                _preview.Play();
                        }
                        else
                        {
                            CommandPause.Execute(null);
                            if (LoadedMedia != null)
                                _preview.Play();
                        }
                    },
                o => LoadedMedia?.MediaStatus == TMediaStatus.Available
                                          || _canLoad(MediaToLoad)
            );
            CommandPlayTheEnd = new UiCommand
            (
                o =>
                {
                    if (LoadedMedia == null)
                        return;
                    Position = LoadedMedia.TcStart + Duration - EndDuration;
                    _preview.Play();
                },
                o => LoadedMedia?.MediaStatus == TMediaStatus.Available && Duration > EndDuration
            );
            CommandUnload = new UiCommand
            (
                o => _mediaUnload(),
                _canUnload
            );
            CommandSeek = new UiCommand
            (
                param =>
                    {
                        long seekFrames;
                        switch ((string)param)
                        {
                            case "fframe":
                                seekFrames = 1;
                                break;
                            case "rframe":
                                seekFrames = -1;
                                break;
                            case "fsecond":
                                seekFrames = FramesPerSecond;
                                break;
                            case "rsecond":
                                seekFrames = -FramesPerSecond;
                                break;
                            default:
                                seekFrames = 0;
                                break;
                        }
                        _preview.Position = _preview.Position + seekFrames;
                        NotifyPropertyChanged(nameof(Position));
                    },
                _canUnload
            );

            CommandCopyToTcIn = new UiCommand
            (
                o => TcIn = Position,
                _canUnload
            );

            CommandCopyToTcOut = new UiCommand
            (
                o => TcOut = Position,
                _canUnload
            );

            CommandSaveSegment = new UiCommand
            (
                o =>
                    {
                        if (!(LoadedMedia is IPersistentMedia media))
                            return;
                        if (_selectedSegment == null)
                        {
                            _lastAddedSegment = media.GetMediaSegments().Add(TcIn, TcOut, SegmentName);
                            _lastAddedSegment.Save();
                        }
                        else
                        {
                            _selectedSegment.TcIn = TcIn;
                            _selectedSegment.TcOut = TcOut;
                            _selectedSegment.SegmentName = SegmentName;
                            _selectedSegment.Save();
                        }
                    },
                o =>
                    {
                        var ss = SelectedSegment;
                        return (LoadedMedia != null
                            && ((ss == null && !string.IsNullOrEmpty(SegmentName))
                                || (ss != null && (ss.IsModified || SegmentName != ss.SegmentName || TcIn != ss.TcIn || TcOut != ss.TcOut))));
                    }
            );
            CommandDeleteSegment = new UiCommand
            (
                o => _selectedSegment?.MediaSegment.Delete(),
                o => _selectedSegment != null
            );
            CommandNewSegment = new UiCommand
            (
                o =>
                    {
                        if (LoadedMedia is IPersistentMedia media)
                            _lastAddedSegment = media.GetMediaSegments().Add(TcIn, TcOut, Common.Properties.Resources._title_NewSegment);
                    }
            );
            CommandSetSegmentNameFocus = new UiCommand
            (
                o =>
                    {
                        IsSegmentNameFocused = true;
                        NotifyPropertyChanged(nameof(IsSegmentNameFocused));
                    }
            );

            CommandTrimSource = new UiCommand
            (
                o =>
                {
                    var durationSelection = TcOut.Ticks - TcIn.Ticks + FormatDescription.FrameTicks;
                    if (SelectedMedia is IPersistentMedia media)
                    {
                        media.TcPlay = TcIn;
                        media.DurationPlay = new TimeSpan(durationSelection);
                        media.Save();
                    }
                    if (SelectedEvent != null)
                    {
                        SelectedEvent.ScheduledTc = TcIn;
                        SelectedEvent.Duration = new TimeSpan(durationSelection);
                        SelectedEvent.Save();
                    }
                    if (SelectedIngestOperation != null)
                    {
                        SelectedIngestOperation.StartTC = TcIn;
                        SelectedIngestOperation.Duration = new TimeSpan(durationSelection);
                    }
                },
                o =>
                {
                    if (!_canTrimMedia)
                        return false;
                    var durationSelection = TcOut.Ticks - TcIn.Ticks + FormatDescription.FrameTicks;
                    if (IsLoaded && LoadedMedia == MediaToLoad)
                    {
                        if (SelectedMedia != null)
                            return SelectedMedia.TcPlay != TcIn || SelectedMedia.DurationPlay.Ticks != durationSelection;
                        if (SelectedEvent?.HaveRight(EventRight.Modify) == true)
                            return SelectedEvent.ScheduledTc != TcIn || SelectedEvent.Duration.Ticks != durationSelection;
                        if (SelectedIngestOperation != null)
                            return SelectedIngestOperation.Trim && (SelectedIngestOperation.StartTC != TcIn || SelectedIngestOperation.Duration.Ticks != durationSelection);
                    }
                    return false;
                }
            );
            CommandFastForwardOneFrame = new UiCommand
            (
                o => SliderPosition = Math.Min(SliderPosition + 1, LoadedDuration),
                o => IsLoaded && SliderPosition < LoadedDuration
            );
            CommandFastForward = new UiCommand
            (
                o => SliderPosition = Math.Min(SliderPosition + FramesPerSecond, LoadedDuration),
                o => IsLoaded && SliderPosition < LoadedDuration
            );
            CommandBackwardOneFrame = new UiCommand
            (
                o => SliderPosition = Math.Max(SliderPosition - 1, 0),
                o => IsLoaded && SliderPosition > 0
            );
            CommandBackward = new UiCommand
            (
                o => SliderPosition = Math.Max(SliderPosition - FramesPerSecond, 0),
                o => IsLoaded && SliderPosition > 0
            );
            CommandToggleLayer = new UiCommand(_stillToggle, _canStillToggle);
        }

        private bool _canStillToggle(object obj)
        {
            if (!(obj is string s && int.TryParse(s, out var layer)))
                return false;
            var videoLayer = (VideoLayer)((int)VideoLayer.PreviewCG1 + layer);
            return true;
        }

        private void _stillToggle(object obj)
        {

        }

        private bool _canUnload(object o)
        {
            MediaSegmentViewmodel segment = PlayWholeClip ? SelectedSegment : null;
            IMedia media = LoadedMedia;
            if (media == null)
                return false;
            TimeSpan duration = PlayWholeClip ? media.Duration : (segment?.Duration ?? media.Duration);
            return duration.Ticks >= _preview.FormatDescription.FrameTicks;
        }

        private bool _canLoad(IMedia media)
        {
            return media != null
                && media.MediaType == TMediaType.Movie
                && (media.Directory is IServerDirectory || media.Directory is IArchiveDirectory || (media.Directory is IIngestDirectory && ((IIngestDirectory)media.Directory).AccessType == TDirectoryAccessType.Direct))
                && media.MediaStatus == TMediaStatus.Available
                && media.FrameRate().Equals(_preview.FormatDescription.FrameRate);
        }

        #endregion // Commands

        protected override void OnDispose()
        {
            if (LoadedMedia == _preview.Media)
                _preview.Unload();
            _preview.PropertyChanged -= PreviewPropertyChanged;
            if (_channel != null)
                _channel.PropertyChanged -= OnChannelPropertyChanged;
            LoadedMedia = null;
            SelectedSegment = null;
        }

        private void _mediaLoad(IMedia media, bool reloadSegments)
        {
            if (media == null)
                return;
            LoadedMedia = media;
            if (reloadSegments)
            {
                PlayWholeClip = false;
                SelectedSegment = null;
            }
            Duration = GetDuration();
            StartTc = GetStartTc();
            TimeSpan duration = Duration;
            TimeSpan tcIn = StartTc;
            double audioVolume = _selectedEvent?.AudioVolume ?? media.AudioVolume;
            if (duration.Ticks >= FormatDescription.FrameTicks)
            {
                TcIn = tcIn;
                TcOut = tcIn + duration - TimeSpan.FromTicks(FormatDescription.FrameTicks);
                if (reloadSegments && media is IPersistentMedia)
                {
                    MediaSegments.Clear();
                    foreach (IMediaSegment ms in ((IPersistentMedia)media).GetMediaSegments().Segments)
                        MediaSegments.Add(new MediaSegmentViewmodel((IPersistentMedia)media, ms));
                }
                var seek = (tcIn.Ticks - media.TcStart.Ticks) / FormatDescription.FrameTicks;
                long newPosition = _preview.IsLoaded && _loadedMedia != null ? _preview.LoadedSeek + _preview.Position - seek : 0;
                if (newPosition < 0)
                    newPosition = 0;
                LoadedDuration = duration.Ticks / FormatDescription.FrameTicks;
                _preview.LoadMovie(media, seek, LoadedDuration, newPosition, audioVolume);
            }
        }

        private void _mediaUnload()
        {
            _preview.Unload();
            LoadedMedia = null;
            TcIn = TimeSpan.Zero;
            TcOut = TimeSpan.Zero;
            LoadedDuration = 0;
            StartTc = TimeSpan.Zero;
            Duration = TimeSpan.Zero;
            NotifyPropertyChanged(nameof(SliderPosition));
        }

        private TimeSpan GetStartTc()
        {
            if (_selectedSegment != null & !_playWholeClip)
                return _selectedSegment.TcIn;
            if (_selectedMedia != null)
                return _playWholeClip ? _selectedMedia.TcStart : _selectedMedia.TcPlay;
            if (SelectedIngestOperation != null)
                return _playWholeClip ? _selectedIngestOperation.StartTC : _selectedIngestOperation.Source.TcStart;
            IMedia media = _selectedEvent?.Media;
            if (media != null)
                return _playWholeClip ? media.TcStart : _selectedEvent.ScheduledTc;
            return TimeSpan.Zero;
        }

        private TimeSpan GetDuration()
        {
            if (_selectedSegment != null & !_playWholeClip)
                return _selectedSegment.Duration;
            if (_selectedMedia != null)
                return _playWholeClip ? _selectedMedia.Duration : _selectedMedia.DurationPlay;
            if (SelectedIngestOperation != null)
                return _playWholeClip ? _selectedIngestOperation.Duration : _selectedIngestOperation.Source.Duration;
            IMedia media = _selectedEvent?.Media;
            if (media != null)
                return _playWholeClip ? media.Duration : _selectedEvent.Duration;
            return TimeSpan.Zero;
        }

        private void SegmentPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            InvalidateRequerySuggested();
        }

        private void PreviewPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (LoadedMedia != null && e.PropertyName == nameof(IPreview.Position))
            {
                NotifyPropertyChanged(nameof(Position));
                NotifyPropertyChanged(nameof(SliderPosition));
            }
            if (e.PropertyName == nameof(IPreview.Media))
                if (_preview.Media != LoadedMedia)
                    LoadedMedia = null;
        }

        private void OnChannelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IPlayoutServerChannel.IsServerConnected))
                NotifyPropertyChanged(nameof(IsEnabled));
        }

        private void _mediaSegments_SegmentRemoved(object sender, MediaSegmentEventArgs e)
        {
            OnUiThread(() =>
            {
                if (!(LoadedMedia is IPersistentMedia media) || sender != media.GetMediaSegments())
                    return;
                var vM = MediaSegments.FirstOrDefault(s => s.MediaSegment == e.Segment);
                if (vM != null)
                    MediaSegments.Remove(vM);
                if (_selectedSegment == vM)
                    SelectedSegment = null;
            });
        }

        private void _mediaSegments_SegmentAdded(object sender, MediaSegmentEventArgs e)
        {
            OnUiThread(() =>
            {
                if (LoadedMedia is IPersistentMedia media && sender == media.GetMediaSegments())
                {
                    var newVm = new MediaSegmentViewmodel(media, e.Segment);
                    MediaSegments.Add(newVm);
                    if (e.Segment == _lastAddedSegment)
                        SelectedSegment = newVm;
                }
            });
        }

        private void Media_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IMedia.AudioVolume) && _preview.IsLoaded && _selectedMedia != null)
                _preview.AudioVolume = _selectedMedia.AudioVolume;
            if (e.PropertyName == nameof(IMedia.MediaStatus) && _selectedMedia != null)
                InvalidateRequerySuggested();
        }

        private void Event_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var ev = _selectedEvent;
            var media = ev?.Media;
            if (e.PropertyName == nameof(IEvent.AudioVolume) && _preview.IsLoaded && ev != null && media != null)
                _preview.AudioVolume = ev.AudioVolume ?? media.AudioVolume;
        }

        private IMedia MediaToLoad => SelectedMedia ?? SelectedEvent?.Media ?? SelectedIngestOperation?.Source;

    }
}
