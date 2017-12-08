using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.ObjectModel;
using TAS.Client.Common;
using System.Windows;
using System.Windows.Input;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class PreviewViewmodel : ViewmodelBase
    {
        private IMedia _selectedMedia;
        private IEvent _selectedEvent;
        private readonly IEngine _engine;
        private readonly IPreview _preview;
        private readonly IPlayoutServerChannel _channel;
        private readonly VideoFormatDescription _formatDescription;
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
        private static readonly TimeSpan EndDuration = TimeSpan.FromSeconds(3);


        public PreviewViewmodel(IEngine engine, IPreview preview)
        {
            preview.PropertyChanged += PreviewPropertyChanged;
            _channel = preview.PlayoutChannelPRV;
            if (_channel != null)
                _channel.PropertyChanged += OnChannelPropertyChanged;
            _engine = engine;
            _preview = preview;
            _formatDescription = _preview.FormatDescription;
            CreateCommands();
        }

        public TVideoFormat VideoFormat => _formatDescription.Format;

        public IMedia SelectedMedia
        {
            get => _selectedMedia;
            set
            {
                if (_channel != null)
                {
                    IMedia oldVal = _selectedMedia;
                    IMedia newVal = value;
                    if (SetField(ref _selectedMedia, newVal))
                    {
                        if (oldVal != null)
                            oldVal.PropertyChanged -= Media_PropertyChanged;
                        if (newVal != null)
                            newVal.PropertyChanged += Media_PropertyChanged;
                        InvalidateRequerySuggested();
                    }
                }
            }
        }

        public IEvent SelectedEvent
        {
            get => _selectedEvent;
            set
            {
                IEvent oldEvent = _selectedEvent;
                if (SetField(ref _selectedEvent, value))
                {
                    if (oldEvent != null)
                        oldEvent.PropertyChanged -= Event_PropertyChanged;
                    if (value != null)
                        value.PropertyChanged += Event_PropertyChanged;
                    InvalidateRequerySuggested();
                }
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
            private set => SetField(ref _startTc, value);
        }

        public TimeSpan Duration
        {
            get => _duration;
            private set => SetField(ref _duration, value);
        }

        public bool IsSegmentsVisible {
            get => _isSegmentsVisible;
            set => SetField(ref _isSegmentsVisible, value);
        }

        public bool IsSegmentsEnabled => _preview.PreviewMedia is IServerMedia;

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

        public TimeSpan DurationSelection => new TimeSpan(TcOut.Ticks - TcIn.Ticks + _formatDescription.FrameTicks);

        public TimeSpan Position
        {
            get => _preview.PreviewMedia == null ? TimeSpan.Zero : TimeSpan.FromTicks((_preview.PreviewPosition + _preview.PreviewLoadedSeek) * TimeSpan.TicksPerSecond * _formatDescription.FrameRate.Den / _formatDescription.FrameRate.Num + _loadedMedia.TcStart.Ticks);
            set => _preview.PreviewPosition = (value.Ticks - _loadedMedia?.TcStart.Ticks) / _formatDescription.FrameTicks - _preview.PreviewLoadedSeek ?? 0;
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
            get => _preview.PreviewMedia == null ? 0 : _preview.PreviewPosition;
            set => _preview.PreviewPosition = value;
        }

        public long FramesPerSecond => _formatDescription.FrameRate.Num / _formatDescription.FrameRate.Den;

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

        

        private void CreateCommands()
        {
            CommandPause = new UICommand
            {
                ExecuteDelegate = o =>
                    {
                        if (LoadedMedia == null)
                            _mediaLoad(MediaToLoad, true);
                        else
                            if (_preview.PreviewIsPlaying)
                            _preview.PreviewPause();
                        else
                        {
                            _preview.PreviewPosition = 0;
                            NotifyPropertyChanged(nameof(Position));
                        }
                    },
                CanExecuteDelegate = o => LoadedMedia?.MediaStatus == TMediaStatus.Available
                                          || _canLoad(MediaToLoad)
            };
            CommandPlay = new UICommand
            {
                ExecuteDelegate = o =>
                    {
                        if (LoadedMedia != null)
                        {
                            if (_preview.PreviewIsPlaying)
                                _preview.PreviewPause();
                            else
                                _preview.PreviewPlay();
                        }
                        else
                        {
                            CommandPause.Execute(null);
                            if (LoadedMedia != null)
                                _preview.PreviewPlay();
                        }
                    },
                CanExecuteDelegate = o => LoadedMedia?.MediaStatus == TMediaStatus.Available
                                          || _canLoad(MediaToLoad)
            };
            CommandPlayTheEnd = new UICommand
            {
                ExecuteDelegate = o =>
                {
                    if (LoadedMedia != null)
                        Position = StartTc + Duration - EndDuration;
                    _preview.PreviewPlay();
                },
                CanExecuteDelegate = o => LoadedMedia?.MediaStatus == TMediaStatus.Available && Duration > EndDuration
            };
            CommandUnload = new UICommand
            {
                ExecuteDelegate = o => _mediaUnload(),
                CanExecuteDelegate = _canUnload                   
            };
            CommandSeek = new UICommand
            {
                ExecuteDelegate = param =>
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
                        _preview.PreviewPosition = _preview.PreviewPosition + seekFrames;
                        NotifyPropertyChanged(nameof(Position));
                    },
                CanExecuteDelegate = _canUnload
            };

            CommandCopyToTcIn = new UICommand
            {
                ExecuteDelegate = o => TcIn = Position,
                CanExecuteDelegate = _canUnload
            };

            CommandCopyToTcOut = new UICommand
            {
                ExecuteDelegate = o => TcOut = Position,
                CanExecuteDelegate = _canUnload
            };

            CommandSaveSegment = new UICommand
            {
                ExecuteDelegate = o =>
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
                CanExecuteDelegate = o =>
                    {
                        var ss = SelectedSegment;
                        return (LoadedMedia != null 
                            && ((ss == null && !string.IsNullOrEmpty(SegmentName))
                                || (ss != null && (ss.IsModified || SegmentName != ss.SegmentName || TcIn != ss.TcIn || TcOut != ss.TcOut))));
                    }
            };
            CommandDeleteSegment = new UICommand()
            {
                ExecuteDelegate = o =>_selectedSegment?.MediaSegment.Delete(),
                CanExecuteDelegate = o => _selectedSegment != null
            };
            CommandNewSegment = new UICommand()
            {
                ExecuteDelegate = o =>
                    {
                        if (LoadedMedia is IPersistentMedia media)
                            _lastAddedSegment = media.GetMediaSegments().Add(TcIn, TcOut, Common.Properties.Resources._title_NewSegment);
                    },
            };
            CommandSetSegmentNameFocus = new UICommand()
            {
                ExecuteDelegate = o =>
                    {
                        IsSegmentNameFocused = true;
                        NotifyPropertyChanged(nameof(IsSegmentNameFocused));
                    },
            };

            CommandTrimSource = new UICommand
            {
                CanExecuteDelegate = o =>
                {
                    if (IsLoaded && LoadedMedia == (MediaToLoad))
                    {
                        if (SelectedMedia != null && _engine.HaveRight(EngineRight.MediaEdit))
                            return SelectedMedia.TcStart != TcIn || SelectedMedia.DurationPlay != DurationSelection;
                        if (SelectedEvent?.HaveRight(EventRight.Modify) == true)
                            return SelectedEvent.ScheduledTc != TcIn || SelectedEvent.Duration != DurationSelection;
                        if (SelectedIngestOperation != null)
                            return SelectedIngestOperation.Trim && (SelectedIngestOperation.StartTC != TcIn || SelectedIngestOperation.Duration != DurationSelection);
                    }
                    return false;
                },
                ExecuteDelegate = o =>
                {
                    if (SelectedMedia is IPersistentMedia media)
                    {
                        media.TcPlay= TcIn;
                        media.DurationPlay = DurationSelection;
                        media.Save();
                    }
                    if (SelectedEvent != null)
                    {
                        SelectedEvent.ScheduledTc = TcIn;
                        SelectedEvent.Duration = DurationSelection;
                        SelectedEvent.Save();
                    }
                    if (SelectedIngestOperation != null)
                    {
                        SelectedIngestOperation.StartTC = TcIn;
                        SelectedIngestOperation.Duration = DurationSelection;
                    }
                }
            };
            CommandFastForwardOneFrame = new UICommand
            {
                ExecuteDelegate = o => SliderPosition = Math.Min(SliderPosition + 1, LoadedDuration),
                CanExecuteDelegate = o => IsLoaded && SliderPosition < LoadedDuration
            };
            CommandFastForward = new UICommand
            {
                ExecuteDelegate = o => SliderPosition = Math.Min(SliderPosition + FramesPerSecond, LoadedDuration),
                CanExecuteDelegate = o => IsLoaded && SliderPosition < LoadedDuration
            };
            CommandBackwardOneFrame = new UICommand
            {
                ExecuteDelegate = o => SliderPosition = Math.Max(SliderPosition - 1, 0),
                CanExecuteDelegate = o => IsLoaded && SliderPosition > 0
            };
            CommandBackward = new UICommand
            {
                ExecuteDelegate = o => SliderPosition = Math.Max(SliderPosition - FramesPerSecond, 0),
                CanExecuteDelegate = o => IsLoaded && SliderPosition > 0
            };
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
                && (media.Directory is IServerDirectory || media.Directory is IArchiveDirectory || (media.Directory is IIngestDirectory && ((IIngestDirectory)media.Directory).AccessType == TDirectoryAccessType.Direct))
                && media.MediaStatus == TMediaStatus.Available 
                && media.FrameRate().Equals(_preview.FormatDescription.FrameRate);
        }

        #endregion // Commands

        protected override void OnDispose()
        {
            if (LoadedMedia == _preview.PreviewMedia)
                _preview.PreviewUnload();
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
            if (duration.Ticks >= _formatDescription.FrameTicks)
            {
                TcIn = tcIn;
                TcOut = tcIn + duration - TimeSpan.FromTicks(_formatDescription.FrameTicks);
                if (reloadSegments && media is IPersistentMedia)
                {
                    MediaSegments.Clear();
                    foreach (IMediaSegment ms in ((IPersistentMedia)media).GetMediaSegments().Segments)
                        MediaSegments.Add(new MediaSegmentViewmodel((IPersistentMedia)media, ms));
                }
                var seek = (tcIn.Ticks - media.TcStart.Ticks) / _formatDescription.FrameTicks;
                long newPosition = _preview.PreviewLoaded ? _preview.PreviewLoadedSeek + _preview.PreviewPosition - seek : 0;
                if (newPosition < 0)
                    newPosition = 0;
                LoadedDuration = duration.Ticks / _formatDescription.FrameTicks;
                _preview.PreviewLoad(media, seek, LoadedDuration, newPosition, audioVolume);
            }
        }

        private void _mediaUnload()
        {
            _preview.PreviewUnload();
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
            if (LoadedMedia != null && e.PropertyName == nameof(IPreview.PreviewPosition))
            {
                NotifyPropertyChanged(nameof(Position));
                NotifyPropertyChanged(nameof(SliderPosition));
            }
            if (e.PropertyName == nameof(IPreview.PreviewMedia))
                if (_preview.PreviewMedia != LoadedMedia)
                    LoadedMedia = null;
        }

        private void OnChannelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IPlayoutServerChannel.IsServerConnected))
                NotifyPropertyChanged(nameof(IsEnabled));
        }

        private void _mediaSegments_SegmentRemoved(object sender, MediaSegmentEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
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
            Application.Current.Dispatcher.BeginInvoke((Action)delegate 
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
            if (e.PropertyName == nameof(IMedia.AudioVolume) && _preview.PreviewLoaded && _selectedMedia != null)
                _preview.PreviewAudioVolume = _selectedMedia.AudioVolume;
            if (e.PropertyName == nameof(IMedia.MediaStatus) && _selectedMedia != null)
                InvalidateRequerySuggested();
        }

        private void Event_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var ev = _selectedEvent;
            var media = ev?.Media;
            if (e.PropertyName == nameof(IEvent.AudioVolume) && _preview.PreviewLoaded && ev != null && media != null)
                _preview.PreviewAudioVolume = ev.AudioVolume ?? media.AudioVolume;
        }

        private IMedia MediaToLoad => SelectedMedia ?? SelectedEvent?.Media ?? SelectedIngestOperation?.Source;

    }
}
