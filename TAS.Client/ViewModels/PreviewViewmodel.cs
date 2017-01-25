using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using TAS.Common;
using TAS.Client.Common;
using TAS.Server.Interfaces;
using System.Windows;

namespace TAS.Client.ViewModels
{
    public class PreviewViewmodel : ViewmodelBase
    {
        private IMedia _media;
        private IEvent _event;
        private readonly IPreview _preview;
        private readonly IPlayoutServerChannel _channelPRV;
        private readonly VideoFormatDescription _formatDescription;
        private readonly Views.PreviewView _view;
        private IMediaSegment _lastAddedSegment;
        public PreviewViewmodel(IPreview preview)
        {
            preview.PropertyChanged += this.PreviewPropertyChanged;
            _channelPRV = preview.PlayoutChannelPRV;
            if (_channelPRV != null)
                _channelPRV.OwnerServer.PropertyChanged += this.OnServerPropertyChanged;
            _preview = preview;
            _formatDescription = _preview.FormatDescription;
            _view = new Views.PreviewView(_formatDescription.FrameRate) { DataContext = this };
            CreateCommands();
        }

        protected override void OnDispose()
        {
            if (LoadedMedia == _preview.PreviewMedia)
                _preview.PreviewUnload();
            _preview.PropertyChanged -= this.PreviewPropertyChanged;
            if (_channelPRV != null)
                _channelPRV.OwnerServer.PropertyChanged -= this.OnServerPropertyChanged;
            LoadedMedia = null;
            SelectedSegment = null;
        }

        public Views.PreviewView View { get { return _view; } }

        public IMedia Media
        {
            get { return _media; }
            set
            {
                if (_channelPRV != null)
                {
                    IMedia oldVal = _media;
                    IMedia newVal = value;
                    if (SetField(ref _media, newVal, nameof(Media)))
                    {
                        if (oldVal != null)
                            oldVal.PropertyChanged -= Media_PropertyChanged;
                        if (newVal != null)
                            newVal.PropertyChanged += Media_PropertyChanged;
                        _event = null;
                        NotifyPropertyChanged(nameof(Event));
                        InvalidateRequerySuggested();
                    }
                }
            }
        }

        private void Media_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IMedia.AudioVolume) && _preview.PreviewLoaded && _media != null)
                _preview.PreviewAudioLevel = _media.AudioVolume;
            if (e.PropertyName == nameof(IMedia.MediaStatus) && _media != null)
                InvalidateRequerySuggested();
        }

        private void Event_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            IEvent ev = _event;
            IMedia media = ev == null ? null : ev.Media;
            if (e.PropertyName == nameof(IEvent.AudioVolume) && _preview.PreviewLoaded && ev != null && media != null)
                _preview.PreviewAudioLevel = ev.AudioVolume == null? media.AudioVolume : (decimal)ev.AudioVolume;
        }

        public IEvent Event
        {
            get { return _event; }
            set
            {
                IEvent oldEvent = _event;
                if (SetField(ref _event, value, nameof(Event)))
                {
                    if (oldEvent != null)
                        oldEvent.PropertyChanged -= Event_PropertyChanged;
                    if (value != null)
                        value.PropertyChanged += Event_PropertyChanged;
                    _media = null;
                    NotifyPropertyChanged(nameof(Media));
                    InvalidateRequerySuggested();
                }
            }
        }

        private IMedia _loadedMedia;
        public IMedia LoadedMedia
        {
            get { return _loadedMedia; }
            private set
            {
                var pm = _loadedMedia as IPersistentMedia;
                if (_loadedMedia != value)
                {
                    if (pm != null)
                    {
                        pm.MediaSegments.SegmentAdded -= _mediaSegments_SegmentAdded;
                        pm.MediaSegments.SegmentRemoved -= _mediaSegments_SegmentRemoved;
                    }
                    pm = value as IPersistentMedia;
                    if (pm != null)
                    {
                        pm.MediaSegments.SegmentAdded += _mediaSegments_SegmentAdded;
                        pm.MediaSegments.SegmentRemoved += _mediaSegments_SegmentRemoved;
                    }
                    _mediaLoad(value, true);
                }
            }
        }

        private void _mediaSegments_SegmentRemoved(object sender, MediaSegmentEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate ()
            {
                IPersistentMedia media = _loadedMedia as IPersistentMedia;
                if (media != null && sender == media.MediaSegments)
                {
                    var vM = _mediaSegments.FirstOrDefault(s => s.MediaSegment == e.Segment);
                    if (vM != null)
                        _mediaSegments.Remove(vM);
                    if (_selectedSegment == vM)
                        SelectedSegment = null;
                }
            });
        }

        private void _mediaSegments_SegmentAdded(object sender, MediaSegmentEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate ()
            {
                IPersistentMedia media = _loadedMedia as IPersistentMedia;
                if (media != null && sender == media.MediaSegments)
                {
                    var newVM = new MediaSegmentViewmodel(media, e.Segment);
                    _mediaSegments.Add(newVM);
                    if (e.Segment == _lastAddedSegment)
                        SelectedSegment = newVM;
                }
            });
        }

        private long _loadedSeek;
        private long _loadedDuration;

        public TimeSpan StartTc
        {
            get
            {
                if (_selectedSegment != null & !_playWholeClip)
                    return _selectedSegment.TcIn;
                if (_media != null)
                    return _playWholeClip ? _media.TcStart : _media.TcPlay;
                if (_event != null)
                {
                    IMedia media = _event.Media;
                    if (media != null)
                        return _playWholeClip ? media.TcStart : _event.ScheduledTc;
                }
                return TimeSpan.Zero;
            }
        }

        public TimeSpan Duration
        {
            get
            {
                if (_selectedSegment != null & !_playWholeClip)
                    return _selectedSegment.Duration;
                if (_media != null)
                    return _playWholeClip ? _media.Duration : _media.DurationPlay;
                if (_event != null)
                {
                    IMedia media = _event.Media;
                    if (media != null)
                        return _playWholeClip ? media.Duration : _event.Duration;
                }
                return TimeSpan.Zero;
            }
        }

        private bool _isSegmentsVisible;
        public bool IsSegmentsVisible { get { return _isSegmentsVisible; } set { SetField(ref _isSegmentsVisible, value, nameof(IsSegmentsVisible)); } }

        public bool IsSegmentsEnabled { get { return _preview.PreviewMedia is IServerMedia; } }

        private void _mediaLoad(IMedia media, bool reloadSegments)
        {
            if (reloadSegments)
            {
                _playWholeClip = false;
                SelectedSegment = null;
            }
            TimeSpan duration = Duration;
            TimeSpan tcIn = StartTc;
            decimal audioVolume = _event != null && _event.AudioVolume != null ? (decimal)_event.AudioVolume : media != null ? media.AudioVolume : 0M;
            if (media != null
                && duration.Ticks >= _formatDescription.FrameTicks)
            {
                TcIn = tcIn;
                TcOut = tcIn + duration - TimeSpan.FromTicks(_formatDescription.FrameTicks);
                if (reloadSegments && media is IPersistentMedia)
                {
                    MediaSegments.Clear();
                    foreach (IMediaSegment ms in ((IPersistentMedia)media).MediaSegments.Segments)
                        MediaSegments.Add(new MediaSegmentViewmodel((IPersistentMedia)media, ms));
                }
                _loadedSeek = (tcIn.Ticks - media.TcStart.Ticks) / _formatDescription.FrameTicks;
                long newPosition = _preview.PreviewLoaded ? _preview.PreviewSeek + _preview.PreviewPosition - _loadedSeek : 0;
                if (newPosition < 0)
                    newPosition = 0;
                _loadedDuration = duration.Ticks / _formatDescription.FrameTicks;
                _loadedMedia = media;
                _preview.PreviewLoad(media, _loadedSeek, _loadedDuration, newPosition, audioVolume);
            }
            NotifyPropertyChanged(null);
        }

        void _mediaUnload()
        {
            _tcIn = TimeSpan.Zero;
            _tcOut = TimeSpan.Zero;
            _loadedSeek = 0;
            _preview.PreviewUnload();
            _loadedMedia = null;
            _preview.PreviewUnload();
            NotifyPropertyChanged(null);
        }

        private TimeSpan _tcIn;
        public TimeSpan TcIn
        {
            get { return _tcIn; }
            set
            {
                if (SetField(ref _tcIn, value, nameof(TcIn)))
                    InvalidateRequerySuggested();
            }
        }

        private TimeSpan _tcOut;
        public TimeSpan TcOut
        {
            get { return _tcOut; }
            set
            {
                if (SetField(ref _tcOut, value, nameof(TcOut)))
                    InvalidateRequerySuggested();
            }
        }

        public TimeSpan DurationSelection { get { return new TimeSpan(TcOut.Ticks - TcIn.Ticks + _formatDescription.FrameTicks); } }

        public TimeSpan Position
        {
            get
            {
                return _preview.PreviewMedia == null ? TimeSpan.Zero : TimeSpan.FromTicks((long)((_preview.PreviewPosition + _preview.PreviewSeek) * TimeSpan.TicksPerSecond * _formatDescription.FrameRate.Den / _formatDescription.FrameRate.Num + _preview.PreviewMedia.TcStart.Ticks));
            }
            set
            {
                _preview.PreviewPosition = (value.Ticks - StartTc.Ticks) / _formatDescription.FrameTicks - _loadedSeek;
            }
        }

        public bool IsPlayable { get { return LoadedMedia != null && LoadedMedia.MediaStatus == TMediaStatus.Available; } }
        public bool IsLoaded { get { return LoadedMedia != null; } }

        public string MediaName
        {
            get
            {
                IMedia loadedMedia = LoadedMedia;
                return (loadedMedia == null) ? string.Empty : loadedMedia.MediaName;
            }
        }
        public string FileName 
        {
            get
            {
                IMedia loadedMedia = LoadedMedia;
                return (loadedMedia == null) ? string.Empty : loadedMedia.FileName;
            }
        }
        
        public long SliderMaximum
        {
            get { return _loadedDuration; }
        }

        public double SliderTickFrequency
        {
            get { return _loadedDuration / 50; }
        }

        public long SliderPosition
        {
            get
            {
                return _preview.PreviewMedia == null ? 0 : _preview.PreviewPosition;
            }
            set
            {
                _preview.PreviewPosition = value;
            }
        }

        public long FramesPerSecond { get { return _formatDescription.FrameRate.Num / _formatDescription.FrameRate.Den; } }

        public void OnServerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_channelPRV != null
                && sender == _channelPRV.OwnerServer)
                NotifyPropertyChanged(nameof(IsEnabled));
        }
        

        private ObservableCollection<MediaSegmentViewmodel> _mediaSegments = new ObservableCollection<MediaSegmentViewmodel>();
        public ObservableCollection<MediaSegmentViewmodel> MediaSegments { get { return _mediaSegments; } }

        private MediaSegmentViewmodel _selectedSegment;
        public MediaSegmentViewmodel SelectedSegment
        {
            get { return _selectedSegment; }
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
                        SelectedSegmentName = value.SegmentName;
                    }
                    else
                        SelectedSegmentName = string.Empty;
                    NotifyPropertyChanged(nameof(SelectedSegmentName));
                    NotifyPropertyChanged(nameof(SelectedSegment));
                    InvalidateRequerySuggested();
                    _mediaLoad(_loadedMedia, false);
                }
            }
        }


        private string _selectedSegmentName;
        public string SelectedSegmentName
        {
            get { return _selectedSegmentName; }
            set
            {
                if (_selectedSegmentName != value)
                {
                    _selectedSegmentName = value;
                    NotifyPropertyChanged(nameof(SelectedSegmentName));
                    InvalidateRequerySuggested();
                }
            }
        }

        public bool IsEnabled
        {
            get
            {
                if (_channelPRV != null)
                {
                    var server = _channelPRV.OwnerServer;
                    if (server != null)
                        return server.IsConnected;
                }
                return false;
            }
        } 

        
        public bool IsSegmentNameFocused { get; set; }


        private bool _playWholeClip;
        public bool PlayWholeClip
        {
            get { return _playWholeClip; }
            set
            {
                if (SetField(ref _playWholeClip, value, nameof(PlayWholeClip)))
                    _mediaLoad(_loadedMedia, false);
            }
        }

        #region Commands

        public UICommand CommandPause { get; private set; }
        public UICommand CommandPlay { get; private set; }
        public UICommand CommandStop { get; private set; }
        public UICommand CommandSeek { get; private set; }
        public UICommand CommandCopyToTcIn { get; private set; }
        public UICommand CommandCopyToTcOut { get; private set; }
        public UICommand CommandSaveSegment { get; private set; }
        public UICommand CommandDeleteSegment { get; private set; }
        public UICommand CommandNewSegment { get; private set; }
        public UICommand CommandSetSegmentNameFocus { get; private set; }

        private void CreateCommands()
        {
            CommandPause = new UICommand()
            {
                ExecuteDelegate = o =>
                    {
                        if (LoadedMedia == null)
                            LoadedMedia = _media ?? _event?.Media;
                        else
                            if (_preview.PreviewIsPlaying)
                            _preview.PreviewPause();
                        else
                        {
                            _preview.PreviewPosition = 0;
                            NotifyPropertyChanged(nameof(Position));
                        }
                    },
                CanExecuteDelegate = o =>
                    {
                        IMedia media = Media ?? (Event != null ? Event.Media : null);
                        return (LoadedMedia != null && LoadedMedia.MediaStatus == TMediaStatus.Available)
                            || _canLoad(media);
                    }
            };
            CommandPlay = new UICommand()
            {
                ExecuteDelegate = o =>
                    {
                        IMedia loadedMedia = LoadedMedia;
                        if (loadedMedia != null)
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
                CanExecuteDelegate = o =>
                    {
                        IMedia media = Media ?? (Event != null ? Event.Media : null);
                        return (LoadedMedia != null && LoadedMedia.MediaStatus == TMediaStatus.Available)
                            || _canLoad(media);
                    }
            };
            CommandStop = new UICommand()
            {
                ExecuteDelegate = o => _mediaUnload(),
                CanExecuteDelegate = _canStop                   
            };
            CommandSeek = new UICommand()
            {
                ExecuteDelegate = param =>
                    {
                        if (_preview.PreviewIsPlaying)
                            _preview.PreviewPause();
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
                CanExecuteDelegate = _canStop
            };

            CommandCopyToTcIn = new UICommand()
            {
                ExecuteDelegate = o => TcIn = Position,
                CanExecuteDelegate = _canStop
            };

            CommandCopyToTcOut = new UICommand()
            {
                ExecuteDelegate = o => TcOut = Position,
                CanExecuteDelegate = _canStop
            };

            CommandSaveSegment = new UICommand()
            {
                ExecuteDelegate = o =>
                    {
                        MediaSegmentViewmodel msVm = _selectedSegment;
                        IPersistentMedia media = LoadedMedia as IPersistentMedia;
                        if (msVm == null)
                        {
                            _lastAddedSegment = media.MediaSegments.Add(TcIn, TcOut, SelectedSegmentName);
                            _lastAddedSegment.Save();
                        }
                        else
                        {
                            msVm.TcIn = TcIn;
                            msVm.TcOut = TcOut;
                            msVm.SegmentName = SelectedSegmentName;
                            msVm.Save();
                        }
                    },
                CanExecuteDelegate = o =>
                    {
                        var ss = SelectedSegment;
                        return (LoadedMedia != null 
                            && ((ss == null && !string.IsNullOrEmpty(SelectedSegmentName))
                                || (ss != null && (ss.IsModified || SelectedSegmentName != ss.SegmentName || TcIn != ss.TcIn || TcOut != ss.TcOut))));
                    }
            };
            CommandDeleteSegment = new UICommand()
            {
                ExecuteDelegate = o =>
                    {
                        MediaSegmentViewmodel msVm = _selectedSegment;
                        if (msVm != null)
                            msVm.MediaSegment.Delete();
                    },
                CanExecuteDelegate = o => _selectedSegment != null
            };
            CommandNewSegment = new UICommand()
            {
                ExecuteDelegate = o =>
                    {
                        var media = LoadedMedia as IPersistentMedia;
                        if (media != null)
                            _lastAddedSegment = media.MediaSegments.Add(TcIn, TcOut, Common.Properties.Resources._title_NewSegment);
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
        }

        bool _canStop(object o)
        {
            MediaSegmentViewmodel segment = PlayWholeClip ? SelectedSegment : null;
            IMedia media = LoadedMedia;
            if (media == null) 
                return false;
            TimeSpan duration = PlayWholeClip ? media.Duration : (segment == null ? media.Duration : segment.Duration);
            return duration.Ticks >= _preview.FormatDescription.FrameTicks;
        }

        bool _canLoad(IMedia media)
        {
            return media != null 
                && (media.Directory is IServerDirectory || media.Directory is IArchiveDirectory || (media.Directory is IIngestDirectory && ((IIngestDirectory)media.Directory).AccessType == TDirectoryAccessType.Direct))
                && media.MediaStatus == TMediaStatus.Available 
                && media.FrameRate.Equals(_preview.FormatDescription.FrameRate);
        }

        #endregion // Commands

        private void SegmentPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            InvalidateRequerySuggested();
        }

        private void PreviewPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_loadedMedia != null && e.PropertyName == nameof(IPreview.PreviewPosition))
            {
                NotifyPropertyChanged(nameof(Position));
                NotifyPropertyChanged(nameof(SliderPosition));
            }
            if (e.PropertyName == nameof(IPreview.PreviewMedia))
                if (_preview.PreviewMedia != _loadedMedia)
                    LoadedMedia = null;
        }
    }
}
