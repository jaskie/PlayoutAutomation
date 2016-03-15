using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using TAS.Common;
using TAS.Client.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.ViewModels
{
    public class PreviewViewmodel : ViewmodelBase
    {
        private IMedia _media;
        private IEvent _event;
        private readonly IPreview _preview;
        private readonly IPlayoutServerChannel _channelPRV;
        private static readonly int _sliderMaximum = 100;
        public PreviewViewmodel(IPreview preview)
        {
            preview.PropertyChanged += this.PreviewPropertyChanged;
            _channelPRV = preview.PlayoutChannelPRV;
            if (_channelPRV != null)
                _channelPRV.OwnerServer.PropertyChanged += this.OnServerPropertyChanged;
            _preview = preview;
            CreateCommands();
        }

        protected override void OnDispose()
        {
            _preview.PropertyChanged -= this.PreviewPropertyChanged;
            if (_channelPRV != null)
                _channelPRV.OwnerServer.PropertyChanged -= this.OnServerPropertyChanged;
            SelectedSegment = null;
        }

        public RationalNumber FrameRate { get { return _preview.PreviewFormatDescription.FrameRate; } }

        public IMedia Media
        {
            get { return _media; }
            set
            {
                if (_channelPRV != null)
                {
                    IMedia oldVal = _media;
                    IMedia newVal = _preview.FindPreviewMedia(value);
                    if (SetField(ref _media, newVal, "Media"))
                    {
                        if (oldVal != null)
                            oldVal.PropertyChanged -= Media_PropertyChanged;
                        if (newVal != null)
                            newVal.PropertyChanged += Media_PropertyChanged;
                        _event = null;
                        NotifyPropertyChanged("Event");
                        NotifyPropertyChanged("CommandPause");
                        NotifyPropertyChanged("CommandPlay");
                    }
                }
            }
        }

        private void Media_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "AudioVolume" && _preview.PreviewLoaded && _media != null)
                _preview.PreviewAudioLevel = _media.AudioVolume;
            if (e.PropertyName == "MediaStatus" && _media != null)
                InvalidateRequerySuggested();
        }

        private void Event_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            IEvent ev = _event;
            IMedia media = ev == null ? null : ev.Media;
            if (e.PropertyName == "AudioVolume" && _preview.PreviewLoaded && ev != null && media != null)
                _preview.PreviewAudioLevel = ev.AudioVolume == null? media.AudioVolume : (decimal)ev.AudioVolume;
        }

        public IEvent Event
        {
            get { return _event; }
            set
            {
                IEvent oldEvent = _event;
                if (SetField(ref _event, value, "Event"))
                {
                    if (oldEvent != null)
                        oldEvent.PropertyChanged -= Event_PropertyChanged;
                    if (value != null)
                        value.PropertyChanged += Event_PropertyChanged;
                    _media = null;
                    NotifyPropertyChanged("Media");
                    NotifyPropertyChanged("CommandPause");
                    NotifyPropertyChanged("CommandPlay");
                }
            }
        }

        private IMedia _loadedMedia;
        public IMedia LoadedMedia
        {
            get { return _loadedMedia; }
            private set
            {
                if (SetField(ref _loadedMedia, value, "LoadedMedia"))
                    NotifyPropertyChanged(null);
            }
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
                    IServerMedia media = _event.ServerMediaPRV;
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
                    IServerMedia media = _event.ServerMediaPRV;
                    if (media != null)
                        return _playWholeClip ? media.Duration : _event.Duration;
                }
                return TimeSpan.Zero;
            }
        }

        private bool _isSegmentsVisible;
        public bool IsSegmentsVisible { get { return _isSegmentsVisible; } set { SetField(ref _isSegmentsVisible, value, "IsSegmentsVisible"); } }

        public bool IsSegmentsEnabled { get { return _preview.PreviewMedia is IServerMedia; } }

        private void _mediaLoad(bool reloadSegments)
        {
            if (reloadSegments)
            {
                _playWholeClip = false;
                SelectedSegment = null;
            }
            TimeSpan duration = Duration;
            TimeSpan tcIn = StartTc;
            IMedia media = _event != null ? _event.ServerMediaPRV : _media;
            decimal audioVolume = _event != null && _event.AudioVolume != null ? (decimal)_event.AudioVolume : media != null ? media.AudioVolume : 0M;
            if (media != null
                && duration.Ticks >= _preview.PreviewFormatDescription.FrameTicks)
            {
                TcIn = tcIn;
                TcOut = tcIn + duration - TimeSpan.FromTicks(_preview.PreviewFormatDescription.FrameTicks);
                if (reloadSegments && media is IServerMedia)
                {
                    MediaSegments.Clear();
                    foreach (IMediaSegment ms in ((IServerMedia)media).MediaSegments.ToList())
                        MediaSegments.Add(new MediaSegmentViewmodel((IServerMedia)media, ms));
                }
                _loadedSeek = (tcIn.Ticks - media.TcStart.Ticks) / _preview.PreviewFormatDescription.FrameTicks;
                long newPosition = _preview.PreviewLoaded ? _preview.PreviewSeek + _preview.PreviewPosition - _loadedSeek : 0;
                if (newPosition < 0)
                    newPosition = 0;
                _loadedDuration = duration.Ticks / _preview.PreviewFormatDescription.FrameTicks;
                _loadedMedia = media;
                _preview.PreviewLoad(media, _loadedSeek, _loadedDuration, newPosition, audioVolume);
            }
            NotifyPropertyChanged(null);
        }

        void _mediaUnload()
        {
            TcIn = TimeSpan.Zero;
            TcOut = TimeSpan.Zero;
            _preview.PreviewUnload();
            NotifyPropertyChanged(null);
        }

        private TimeSpan _tcIn;
        public TimeSpan TcIn
        {
            get { return _tcIn; }
            set
            {
                if (SetField(ref _tcIn, value, "TcIn"))
                    NotifyPropertyChanged("CommandSaveSegment");
            }
        }

        private TimeSpan _tcOut;
        public TimeSpan TcOut
        {
            get { return _tcOut; }
            set
            {
                if (SetField(ref _tcOut, value, "TcOut"))
                    NotifyPropertyChanged("CommandSaveSegment");
            }
        }

        public TimeSpan DurationSelection { get { return new TimeSpan(TcOut.Ticks - TcIn.Ticks + _preview.PreviewFormatDescription.FrameTicks); } }

        public TimeSpan Position
        {
            get
            {
                return _preview.PreviewMedia == null ? TimeSpan.Zero : TimeSpan.FromTicks((long)((_preview.PreviewPosition + _preview.PreviewSeek) * TimeSpan.TicksPerSecond * FrameRate.Den / FrameRate.Num + _preview.PreviewMedia.TcStart.Ticks));
            }
            set
            {
                _preview.PreviewPosition = (value.Ticks - StartTc.Ticks) / _preview.PreviewFormatDescription.FrameTicks - _loadedSeek;
                NotifyPropertyChanged("SliderPosition");
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
        
        public long SliderPosition
        {
            get
            {
                return _loadedDuration <= 1 ? 0 : (_preview.PreviewPosition * _sliderMaximum) / (_loadedDuration-1);
            }
            set 
            {
                long newPos = _loadedSeek + (value * (_loadedDuration -1)) / _sliderMaximum;
                Position = TimeSpan.FromTicks(newPos * _preview.PreviewFormatDescription.FrameTicks + StartTc.Ticks);
                NotifyPropertyChanged("Position");
            }
        }

        public void OnServerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_channelPRV != null
                && sender == _channelPRV.OwnerServer)
                NotifyPropertyChanged("IsEnabled");
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
                    NotifyPropertyChanged("CommandSaveSegment");
                    NotifyPropertyChanged("CommandDeleteSegment");
                    NotifyPropertyChanged("SelectedSegmentName");
                    NotifyPropertyChanged("SelectedSegment");
                    _mediaLoad(false);
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
                    NotifyPropertyChanged("SelectedSegmentName");
                    NotifyPropertyChanged("CommandSaveSegment");
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


        protected TimeSpan MaxPos()
        {
            IMedia loadedMedia = LoadedMedia;
            if (loadedMedia != null)
            {
                if (_playWholeClip)
                    return loadedMedia.TcStart + loadedMedia.Duration;
                else
                    return TcOut;
            }
            return TimeSpan.Zero;
        }

        private bool _playWholeClip;
        public bool PlayWholeClip
        {
            get { return _playWholeClip; }
            set
            {
                if (SetField(ref _playWholeClip, value, "PlayWholeClip"))
                {
                    _mediaLoad(false);
                    NotifyPropertyChanged("SliderPosition");
                }
            }
        }

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
                            _mediaLoad(true);
                        else
                            if (_preview.PreviewIsPlaying)
                                _preview.PreviewPause();
                            else
                            {
                                _preview.PreviewPosition = 0;
                                NotifyPropertyChanged("Position");
                                NotifyPropertyChanged("SliderPosition");
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
                ExecuteDelegate = o => { _mediaUnload(); },
                CanExecuteDelegate = _canStop                   
            };
            CommandSeek = new UICommand()
            {
                ExecuteDelegate = param =>
                    {
                        if (_preview.PreviewIsPlaying)
                            _preview.PreviewPause();
                        int seekFrames = 0;
                        switch ((string)param)
                        {
                            case "fframe": seekFrames = 1;
                                break;
                            case "rframe": seekFrames = -1;
                                break;
                            case "fsecond": seekFrames = (int)(FrameRate.Num / FrameRate.Den);
                                break;
                            case "rsecond":
                                seekFrames = -(int)(FrameRate.Num / FrameRate.Den);
                                break;
                        }
                        _preview.PreviewPosition = _preview.PreviewPosition + seekFrames;
                        NotifyPropertyChanged("Position");
                        NotifyPropertyChanged("SliderPosition");
                    },
                CanExecuteDelegate = _canStop
            };

            CommandCopyToTcIn = new UICommand()
            {
                ExecuteDelegate = o =>
                    {
                        TcIn = Position;
                    },
                CanExecuteDelegate = _canStop
            };

            CommandCopyToTcOut = new UICommand()
            {
                ExecuteDelegate = o =>
                    {
                        TcOut = Position;
                    },
                CanExecuteDelegate = _canStop
            };

            CommandSaveSegment = new UICommand()
            {
                ExecuteDelegate = o =>
                    {
                        MediaSegmentViewmodel msVm = _selectedSegment;
                        IServerMedia media = LoadedMedia as IServerMedia;
                        if (msVm == null)
                        {
                            msVm = new MediaSegmentViewmodel(media) { TcIn = this.TcIn, TcOut = this.TcOut, SegmentName = this.SelectedSegmentName };
                            media.MediaSegments.Add(msVm.MediaSegment);
                            MediaSegments.Add(msVm);
                            SelectedSegment = msVm;
                        }
                        else
                        {
                            msVm.TcIn = TcIn;
                            msVm.TcOut = TcOut;
                            msVm.SegmentName = SelectedSegmentName;
                        }
                        msVm.Save();
                    },
                CanExecuteDelegate = o =>
                    {
                        var ss = SelectedSegment;
                        return (LoadedMedia != null 
                            && ((ss == null && !string.IsNullOrEmpty(SelectedSegmentName))
                                || (ss != null && (ss.Modified || SelectedSegmentName != ss.SegmentName || TcIn != ss.TcIn || TcOut != ss.TcOut))));
                    }
            };
            CommandDeleteSegment = new UICommand()
            {
                ExecuteDelegate = o =>
                    {
                        MediaSegmentViewmodel msVm = _selectedSegment;
                        if (msVm != null)
                        {
                            msVm.Delete();
                            MediaSegments.Remove(msVm);
                            SelectedSegment = null;
                        }
                    },
                CanExecuteDelegate = o => _selectedSegment != null
            };
            CommandNewSegment = new UICommand()
            {
                ExecuteDelegate = o =>
                    {
                        var media = LoadedMedia as IServerMedia;
                        if (media != null)
                        {
                            var msVm = new MediaSegmentViewmodel(media) { TcIn = this.TcIn, TcOut = this.TcOut, SegmentName = Common.Properties.Resources._title_NewSegment };
                            msVm.Save();
                            media.MediaSegments.Add(msVm.MediaSegment);
                            MediaSegments.Add(msVm);
                            SelectedSegment = msVm;
                            IsSegmentNameFocused = true;
                        }
                    },
            };
            CommandSetSegmentNameFocus = new UICommand()
            {
                ExecuteDelegate = o =>
                    {
                        IsSegmentNameFocused = true;
                        NotifyPropertyChanged("IsSegmentNameFocused");
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
            return duration.Ticks >= _preview.PreviewFormatDescription.FrameTicks;
        }

        bool _canLoad(IMedia media)
        {
            return media != null && media.MediaStatus == TMediaStatus.Available && media.FrameRate.Equals(_preview.PreviewFormatDescription.FrameRate);
        }

        private void SegmentPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged("CommandSaveSegment");
        }

        private void PreviewPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "PreviewPosition")
            {
                NotifyPropertyChanged("Position");
                NotifyPropertyChanged("SliderPosition");
            }
            if (e.PropertyName == "PreviewMedia")
                if (_preview.PreviewMedia != _loadedMedia)
                {
                    LoadedMedia = null;
                }
        }
    }
}
