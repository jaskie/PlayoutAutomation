using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Server.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public abstract class EventPanelRundownElementViewmodelBase : EventPanelViewmodelBase
    {
        private string _timeLeft = string.Empty;
        private IMedia _media;

        public EventPanelRundownElementViewmodelBase(IEvent ev, EventPanelViewmodelBase parent) : base(ev, parent)
        {
            Media = ev.Media;
            ev.PositionChanged += _eventPositionChanged;

            CommandToggleHold = new UICommand()
            {
                ExecuteDelegate = (o) =>
                {
                    _event.IsHold = !_event.IsHold;
                    _event.Save();
                },
                CanExecuteDelegate = (o) => _event.PlayState == TPlayState.Scheduled &&
                                            _event.StartType == TStartType.After
            };
            CommandToggleEnabled = new UICommand()
            {
                ExecuteDelegate = (o) =>
                {
                    _event.IsEnabled = !_event.IsEnabled;
                    _event.Save();
                },
                CanExecuteDelegate = (o) => _event.PlayState == TPlayState.Scheduled
            };
            CommandToggleLayer = new UICommand()
            {
                ExecuteDelegate = (l) =>
                {
                    VideoLayer layer = (VideoLayer) sbyte.Parse((string) l);
                    if (_hasSubItemsOnLayer(layer))
                    {
                        var layerEvent = _event.SubEvents.FirstOrDefault(e => e.Layer == layer);
                        layerEvent?.Delete();
                    }
                    else
                        EngineViewmodel.AddMediaEvent(_event, TStartType.WithParent, TMediaType.Still, layer, true);
                },
                CanExecuteDelegate = (o) => _event.PlayState == TPlayState.Scheduled ||
                                            _event.PlayState == TPlayState.Playing ||
                                            _event.PlayState == TPlayState.Paused
            };
            CommandAddNextRundown = new UICommand()
            {
                ExecuteDelegate = o => EngineViewmodel.AddSimpleEvent(_event, TEventType.Rundown, false),
                CanExecuteDelegate = _canAddNextItem
            };
            CommandAddNextEmptyMovie = new UICommand()
            {
                ExecuteDelegate = o => EngineViewmodel.AddSimpleEvent(_event, TEventType.Movie, false),
                CanExecuteDelegate = CanAddNextMovie
            };
            CommandAddNextLive = new UICommand()
            {
                ExecuteDelegate = o => EngineViewmodel.AddSimpleEvent(_event, TEventType.Live, false),
                CanExecuteDelegate = CanAddNewLive
            };
            CommandAddNextMovie = new UICommand()
            {
                ExecuteDelegate = o => EngineViewmodel.AddMediaEvent(_event, TStartType.After, TMediaType.Movie,
                    VideoLayer.Program, false),
                CanExecuteDelegate = CanAddNextMovie
            };
            CommandAddAnimation = new UICommand()
            {
                ExecuteDelegate = o => EngineViewmodel.AddMediaEvent(_event, TStartType.WithParent,
                    TMediaType.Animation, VideoLayer.Animation, true),
                CanExecuteDelegate = o => _event.PlayState == TPlayState.Scheduled
            };
            CommandAddCommandScript = new UICommand
            {
                ExecuteDelegate = o => EngineViewmodel.AddCommandScriptEvent(_event),
                CanExecuteDelegate = o => _event.PlayState == TPlayState.Scheduled
            };
        }

        #region Commands
        public ICommand CommandCut => EngineViewmodel.CommandCutSelected;
        public ICommand CommandCopy => EngineViewmodel.CommandCopySelected;
        public ICommand CommandPaste => EngineViewmodel.CommandPasteSelected;
        public ICommand CommandToggleHold { get; }
        public ICommand CommandToggleEnabled { get; }


        public ICommand CommandToggleLayer { get; }
        public ICommand CommandAddNextRundown { get; }
        public ICommand CommandAddNextMovie { get; }
        public ICommand CommandAddNextEmptyMovie { get; }
        public ICommand CommandAddNextLive { get; }
        public ICommand CommandAddAnimation { get; }
        public ICommand CommandAddCommandScript { get; }


        protected virtual bool CanAddNextMovie(object o)
        {
            return _canAddNextItem(o);
        }

        protected virtual bool CanAddNewLive(object o)
        {
            return _canAddNextItem(o);
        }

        bool _canAddNextItem(object o)
        {
            return _event.PlayState != TPlayState.Played && !_event.IsLoop;
        }

        #endregion // Commands

        public string TimeLeft
        {
            get { return _timeLeft; }
            set { SetField(ref _timeLeft, value); }
        }

        public string EndTime => _event == null || _event.GetSuccessor() != null ? string.Empty : _event.EndTime.ToLocalTime().TimeOfDay.ToSMPTETimecodeString(VideoFormat);

        public bool IsLastEvent => _event != null && _event.GetSuccessor() == null;

        public bool IsStartEvent => _event != null && (_event.StartType == TStartType.Manual || _event.StartType == TStartType.OnFixedTime);

        public bool IsAnimationEnabled => EngineViewmodel.IsAnimationDirAvailable;

        public bool IsPlaying
        {
            get { return _event != null && _event.PlayState == TPlayState.Playing; }
        }

        public bool IsForcedNext
        {
            get { return _event != null && _event.IsForcedNext; }
        }

        public bool HasSubItemOnLayer1
        {
            get { return _hasSubItemsOnLayer(VideoLayer.CG1); }
        }
        public bool HasSubItemOnLayer2
        {
            get { return _hasSubItemsOnLayer(VideoLayer.CG2); }
        }
        public bool HasSubItemOnLayer3
        {
            get { return _hasSubItemsOnLayer(VideoLayer.CG3); }
        }


        public string Layer1SubItemMediaName => _subItemMediaName(VideoLayer.CG1);
        public string Layer2SubItemMediaName => _subItemMediaName(VideoLayer.CG2);
        public string Layer3SubItemMediaName => _subItemMediaName(VideoLayer.CG3);

        public TimeSpan? Offset => _event.Offset;

        public bool IsInvalidInSchedule
        {
            get
            {
                if (_event == null)
                    return false;
                IEvent ne = _event.Next;
                IEvent pe = _event.Prior;
                return !(
                    (ne == null || ne.Prior == _event)
                    && (pe == null || pe.Next == _event)
                    )
                    || _event.EventType == TEventType.Rundown && _event.SubEventsCount > 1;
            }
        }

        public bool IsCGEnabled => _event?.IsCGEnabled == true && _engine?.CGElementsController != null;

        public CGElementViewmodel Logo { get { return  EngineViewmodel.CGElementsControllerViewmodel?.Logos?.FirstOrDefault(l => l.Id == _event.Logo); } }

        public CGElementViewmodel Parental { get { return EngineViewmodel.CGElementsControllerViewmodel?.Parentals?.FirstOrDefault(l => l.Id == _event.Parental); } }

        public CGElementViewmodel Crawl { get { return EngineViewmodel.CGElementsControllerViewmodel?.Crawls?.FirstOrDefault(l => l.Id == _event.Crawl); } }

        public string MediaFileName
        {
            get
            {
                if (_event == null)
                    return string.Empty;
                IMedia media = _event.Media;
                return (media == null) ? ((_event.EventType == TEventType.Movie || _event.EventType == TEventType.StillImage) ? _event.MediaGuid.ToString() : string.Empty) : media.FileName;
            }
        }

        public TMediaErrorInfo MediaErrorInfo
        {
            get
            {
                if (_event == null || _event.EventType == TEventType.Live || _event.EventType == TEventType.Rundown ||
                    _event.EventType == TEventType.Container)
                    return TMediaErrorInfo.NoError;
                // else
                IMedia media = _event.Media;
                if (media == null || media.MediaStatus == TMediaStatus.Deleted || !media.FileExists())
                    return TMediaErrorInfo.Missing;
                //else
                if (media.MediaStatus == TMediaStatus.Available)
                    if (media.MediaType == TMediaType.Still
                        || media.MediaType == TMediaType.Animation
                        || _event.ScheduledTc + _event.Duration <= media.TcStart + media.Duration
                    )
                        return TMediaErrorInfo.NoError;
                return TMediaErrorInfo.TooShort;
            }
        }
        
        public TPlayState PlayState => _event.PlayState;

        public TMediaEmphasis MediaEmphasis => (_media as IPersistentMedia)?.MediaEmphasis ?? TMediaEmphasis.None;

        public string Layer => _event.Layer.ToString();

        public decimal AudioVolume => _event == null || _event.AudioVolume == null ? 0m : (decimal)_event.AudioVolume;

        public TMediaCategory MediaCategory => _media?.MediaCategory ?? TMediaCategory.Uncategorized;

        public bool IsHold => _event.IsHold;

        public bool IsLoop => _event.IsLoop;

        public string ScheduledTime => _event.ScheduledTime.ToLocalTime().TimeOfDay.ToSMPTETimecodeString(VideoFormat);

        public string ScheduledDelay => _event.ScheduledDelay.ToSMPTETimecodeString(VideoFormat);

        public string Duration => _event.Duration.ToSMPTETimecodeString(VideoFormat);

        public virtual bool IsEnabled
        {
            get
            {
                var et = Event.EventType;
                return _event.IsEnabled && (Event.Duration > TimeSpan.Zero || et == TEventType.Animation || et == TEventType.CommandScript);
            }
        }

        public bool IsFixedTimeStart => _event.StartType == TStartType.OnFixedTime;

        public EventPanelViewmodelBase Prior
        {
            get
            {
                int index = _parent.Childrens.IndexOf(this) - 1;
                if (index >= 0 && _parent.Childrens.Count > index)
                    return _parent.Childrens[index];
                return null;
            }
        }

        public EventPanelViewmodelBase Next
        {
            get
            {
                int index = _parent.Childrens.IndexOf(this) +1;
                if (index > 0 && _parent.Childrens.Count > index)
                    return _parent.Childrens[index];
                return null;
            }
        }

        public IMedia Media
        {
            get { return _media; }
            private set
            {
                IMedia oldMedia = _media;
                if (oldMedia != value)
                {
                    if (oldMedia != null)
                        oldMedia.PropertyChanged -= _onMediaPropertyChanged;
                    _media = value;
                    if (value != null)
                    {
                        value.PropertyChanged += _onMediaPropertyChanged;
                        VideoFormat = value.VideoFormat;
                    }
                }
            }
        }


        internal void VerifyIsInvalidInSchedule()
        {
            NotifyPropertyChanged(nameof(IsInvalidInSchedule));
        }
        
        internal override void SetOnTop()
        {
            var p = Parent;
            if (p != null)
                if (p.IsExpanded && View != null)
                {
                    View.SetOnTop();
                }
                else
                    p.SetOnTop();

        }

        protected override void OnSubeventChanged(object o, CollectionOperationEventArgs<IEvent> e)
        {
            base.OnSubeventChanged(o, e);
            switch (e.Item.Layer)
            {
                case VideoLayer.CG1:
                    NotifyPropertyChanged(nameof(HasSubItemOnLayer1));
                    NotifyPropertyChanged(nameof(Layer1SubItemMediaName));
                    break;                
                case VideoLayer.CG2:      
                    NotifyPropertyChanged(nameof(HasSubItemOnLayer2));
                    NotifyPropertyChanged(nameof(Layer2SubItemMediaName));
                    break;               
                case VideoLayer.CG3:     
                    NotifyPropertyChanged(nameof(HasSubItemOnLayer3));
                    NotifyPropertyChanged(nameof(Layer3SubItemMediaName));
                    break;
            }
        }

        protected void _eventPositionChanged(object sender, EventPositionEventArgs e)
        {
            TimeLeft = (e.TimeToFinish == TimeSpan.Zero || _event.PlayState == TPlayState.Scheduled) ? string.Empty : e.TimeToFinish.ToSMPTETimecodeString(VideoFormat);
        }

        protected override void OnEventPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnEventPropertyChanged(sender, e);
            if (e.PropertyName == nameof(IEvent.Duration)
                || e.PropertyName == nameof(IEvent.IsEnabled)
                || e.PropertyName == nameof(IEvent.IsHold)
                || e.PropertyName == nameof(IEvent.EventName)
                || e.PropertyName == nameof(IEvent.IsLoop)
                || e.PropertyName == nameof(IEvent.Offset)
                || e.PropertyName == nameof(IEvent.ScheduledDelay)
                || e.PropertyName == nameof(IEvent.IsForcedNext)
                || e.PropertyName == nameof(IEvent.ScheduledTime)
                || e.PropertyName == nameof(IEvent.IsCGEnabled)
                || e.PropertyName == nameof(IEvent.Crawl)
                || e.PropertyName == nameof(IEvent.Logo)
                || e.PropertyName == nameof(IEvent.Parental)
                || e.PropertyName == nameof(IEvent.EndTime)
                )
                NotifyPropertyChanged(e.PropertyName);
            if (e.PropertyName == nameof(IEvent.ScheduledTc) || e.PropertyName == nameof(IEvent.Duration))
            {
                NotifyPropertyChanged(nameof(IsEnabled));
                NotifyPropertyChanged(nameof(MediaErrorInfo));
            }
            if (e.PropertyName == nameof(IEvent.PlayState))
            {
                NotifyPropertyChanged(e.PropertyName);
                NotifyPropertyChanged(nameof(IsPlaying));
            }
            if (e.PropertyName == nameof(IEvent.StartType))
            {
                NotifyPropertyChanged(nameof(IsStartEvent));
                NotifyPropertyChanged(nameof(IsFixedTimeStart));
            }
            if (e.PropertyName == nameof(IEvent.Media))
            {
                Media = _event.Media;
                NotifyPropertyChanged(nameof(MediaFileName));
                NotifyPropertyChanged(nameof(MediaCategory));
                NotifyPropertyChanged(nameof(MediaEmphasis));
                NotifyPropertyChanged(nameof(VideoFormat));
                NotifyPropertyChanged(nameof(MediaErrorInfo));
            }
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            Media = null;
            _event.PositionChanged -= _eventPositionChanged;
        }

        private void _onMediaPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_event == null || !(sender is IMedia))
                return;
            if (e.PropertyName == nameof(IMedia.MediaStatus))
                NotifyPropertyChanged(nameof(MediaErrorInfo));
            if (e.PropertyName == nameof(IMedia.FileName))
                NotifyPropertyChanged(nameof(MediaFileName));
            if (e.PropertyName == nameof(IMedia.MediaCategory)
                || e.PropertyName == nameof(IPersistentMedia.MediaEmphasis))
                NotifyPropertyChanged(e.PropertyName);
            if (e.PropertyName == nameof(IMedia.VideoFormat))
                VideoFormat = ((IMedia)sender).VideoFormat;
        }

        private string _subItemMediaName(VideoLayer layer)
        {
            if (_event != null)
            {
                IEvent se = _event.SubEvents.FirstOrDefault(e => e.Layer == layer && e.EventType == TEventType.StillImage);
                if (se != null)
                {
                    IMedia m = se.Media;
                    if (m != null)
                        return m.MediaName;
                }
            }
            return string.Empty;
        }

        private bool _hasSubItemsOnLayer(VideoLayer layer)
        {
            return _event.SubEvents.Any(e => e.Layer == layer && e.EventType == TEventType.StillImage);
        }

    }
}
