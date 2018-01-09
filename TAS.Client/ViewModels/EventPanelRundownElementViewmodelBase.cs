using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public abstract class EventPanelRundownElementViewmodelBase : EventPanelViewmodelBase
    {
        private TimeSpan _timeLeft;
        private IMedia _media;

        protected EventPanelRundownElementViewmodelBase(IEvent ev, EventPanelViewmodelBase parent) : base(ev, parent)
        {
            Media = ev.Media;
            ev.PositionChanged += EventPositionChanged;
            ev.SubEventChanged += OnSubeventChanged;

            CommandToggleHold = new UICommand
            {
                ExecuteDelegate = o =>
                {
                    Event.IsHold = !Event.IsHold;
                    Event.Save();
                },
                CanExecuteDelegate = _canToggleHold
            };
            CommandToggleEnabled = new UICommand
            {
                ExecuteDelegate = o =>
                {
                    Event.IsEnabled = !Event.IsEnabled;
                    Event.Save();
                },
                CanExecuteDelegate = o => Event.PlayState == TPlayState.Scheduled && Event.HaveRight(EventRight.Modify)
            };
            CommandToggleLayer = new UICommand
            {
                ExecuteDelegate = l =>
                {
                    VideoLayer layer = (VideoLayer) sbyte.Parse((string) l);
                    if (_hasSubItemsOnLayer(layer))
                    {
                        var layerEvent = Event.SubEvents.FirstOrDefault(e => e.Layer == layer);
                        layerEvent?.Delete();
                    }
                    else
                        EngineViewmodel.AddMediaEvent(Event, TStartType.WithParent, TMediaType.Still, layer, true);
                },
                CanExecuteDelegate = _canToggleLayer
            };
            CommandAddNextRundown = new UICommand
            {
                ExecuteDelegate = o => EngineViewmodel.AddSimpleEvent(Event, TEventType.Rundown, false),
                CanExecuteDelegate = _canAddNextItem
            };
            CommandAddNextEmptyMovie = new UICommand
            {
                ExecuteDelegate = o => EngineViewmodel.AddSimpleEvent(Event, TEventType.Movie, false),
                CanExecuteDelegate = CanAddNextMovie
            };
            CommandAddNextLive = new UICommand
            {
                ExecuteDelegate = o => EngineViewmodel.AddSimpleEvent(Event, TEventType.Live, false),
                CanExecuteDelegate = CanAddNewLive
            };
            CommandAddNextMovie = new UICommand
            {
                ExecuteDelegate = o => EngineViewmodel.AddMediaEvent(Event, TStartType.After, TMediaType.Movie,
                    VideoLayer.Program, false),
                CanExecuteDelegate = CanAddNextMovie
            };
            CommandAddAnimation = new UICommand
            {
                ExecuteDelegate = o => EngineViewmodel.AddMediaEvent(Event, TStartType.WithParent,
                    TMediaType.Animation, VideoLayer.Animation, true),
                CanExecuteDelegate = o => Event.PlayState == TPlayState.Scheduled
            };
            CommandAddCommandScript = new UICommand
            {
                ExecuteDelegate = o => EngineViewmodel.AddCommandScriptEvent(Event),
                CanExecuteDelegate = o => Event.PlayState == TPlayState.Scheduled
            };
        }

        private bool _canToggleLayer(object obj)
        {
            return (Event.PlayState == TPlayState.Scheduled || Event.PlayState == TPlayState.Playing || Event.PlayState == TPlayState.Paused)
                   && Event.HaveRight(EventRight.Modify);
        }

        private bool _canToggleHold(object o)
        {
            return Event.PlayState == TPlayState.Scheduled
                   && Event.StartType == TStartType.After
                   && Event.HaveRight(EventRight.Modify);
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
            return Event.HaveRight(EventRight.Create)
                   && Event.PlayState != TPlayState.Played 
                   && !Event.IsLoop;
        }

        #endregion // Commands

        public TimeSpan TimeLeft 
        {
            get => _timeLeft;
            set => SetField(ref _timeLeft, value);
        }

        public string EndTime => Event == null || Event.GetSuccessor() != null ? string.Empty : Event.EndTime.ToLocalTime().TimeOfDay.ToSMPTETimecodeString(VideoFormat);

        public bool IsLastEvent => Event != null && Event.GetSuccessor() == null;

        public bool IsStartEvent => Event != null && (Event.StartType == TStartType.Manual || Event.StartType == TStartType.OnFixedTime);

        public bool IsAnimationEnabled => EngineViewmodel.IsAnimationDirAvailable;

        public bool IsPlaying
        {
            get { return Event != null && Event.PlayState == TPlayState.Playing; }
        }

        public bool IsForcedNext
        {
            get { return Event != null && Event.IsForcedNext; }
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

        public TimeSpan? Offset => Event.Offset;

        public bool IsInvalidInSchedule
        {
            get
            {
                if (Event == null)
                    return false;
                IEvent ne = Event.Next;
                IEvent pe = Event.Prior;
                return !(
                    (ne == null || ne.Prior == Event)
                    && (pe == null || pe.Next == Event)
                    )
                    || Event.EventType == TEventType.Rundown && Event.SubEventsCount > 1;
            }
        }

        public bool IsCGEnabled => Event?.IsCGEnabled == true && Engine?.CGElementsController != null;

        public CGElementViewmodel Logo { get { return  EngineViewmodel.CGElementsControllerViewmodel?.Logos?.FirstOrDefault(l => l.Id == Event.Logo); } }

        public CGElementViewmodel Parental { get { return EngineViewmodel.CGElementsControllerViewmodel?.Parentals?.FirstOrDefault(l => l.Id == Event.Parental); } }

        public CGElementViewmodel Crawl { get { return EngineViewmodel.CGElementsControllerViewmodel?.Crawls?.FirstOrDefault(l => l.Id == Event.Crawl); } }

        public string MediaFileName
        {
            get
            {
                if (Event == null)
                    return string.Empty;
                IMedia media = Event.Media;
                return (media == null) ? ((Event.EventType == TEventType.Movie || Event.EventType == TEventType.StillImage) ? Event.MediaGuid.ToString() : string.Empty) : media.FileName;
            }
        }

        public TMediaErrorInfo MediaErrorInfo
        {
            get
            {
                if (Event == null || Event.EventType == TEventType.Live || Event.EventType == TEventType.Rundown ||
                    Event.EventType == TEventType.Container)
                    return TMediaErrorInfo.NoError;
                // else
                IMedia media = Event.Media;
                if (media == null || media.MediaStatus == TMediaStatus.Deleted || !media.FileExists())
                    return TMediaErrorInfo.Missing;
                //else
                if (media.MediaStatus == TMediaStatus.Available)
                    if (media.MediaType == TMediaType.Still
                        || media.MediaType == TMediaType.Animation
                        || Event.ScheduledTc + Event.Duration <= media.TcStart + media.Duration
                    )
                        return TMediaErrorInfo.NoError;
                return TMediaErrorInfo.TooShort;
            }
        }
        
        public TPlayState PlayState => Event.PlayState;

        public TMediaEmphasis MediaEmphasis => (_media as IPersistentMedia)?.MediaEmphasis ?? TMediaEmphasis.None;

        public string Layer => Event.Layer.ToString();

        public double AudioVolume => Event == null ? 0 : Event.AudioVolume.GetValueOrDefault();

        public TMediaCategory MediaCategory => _media?.MediaCategory ?? TMediaCategory.Uncategorized;

        public bool IsHold => Event.IsHold;

        public bool IsLoop => Event.IsLoop;

        public string ScheduledTime => Event.ScheduledTime.ToLocalTime().TimeOfDay.ToSMPTETimecodeString(VideoFormat);

        public string ScheduledDelay => Event.ScheduledDelay.ToSMPTETimecodeString(VideoFormat);

        public string Duration => Event.Duration.ToSMPTETimecodeString(VideoFormat);

        public virtual bool IsEnabled
        {
            get
            {
                var et = Event.EventType;
                return Event.IsEnabled && (Event.Duration > TimeSpan.Zero || et == TEventType.Animation || et == TEventType.CommandScript);
            }
        }

        public bool IsFixedTimeStart => Event.StartType == TStartType.OnFixedTime;

        public EventPanelViewmodelBase Prior
        {
            get
            {
                int index = Parent.Childrens.IndexOf(this) - 1;
                if (index >= 0 && Parent.Childrens.Count > index)
                    return Parent.Childrens[index];
                return null;
            }
        }

        public EventPanelViewmodelBase Next
        {
            get
            {
                int index = Parent.Childrens.IndexOf(this) +1;
                if (index > 0 && Parent.Childrens.Count > index)
                    return Parent.Childrens[index];
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
        protected virtual void OnSubeventChanged(object o, CollectionOperationEventArgs<IEvent> e)
        {
            if (e.Item.EventType == TEventType.StillImage)
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

        protected void EventPositionChanged(object sender, EventPositionEventArgs e)
        {
            TimeLeft = e.TimeToFinish;
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
                Media = Event.Media;
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
            Event.PositionChanged -= EventPositionChanged;
            Event.SubEventChanged -= OnSubeventChanged;
        }

        private void _onMediaPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Event == null || !(sender is IMedia))
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
            var se = Event?.SubEvents.FirstOrDefault(e => e.Layer == layer && e.EventType == TEventType.StillImage);
            var m = se?.Media;
            return m?.MediaName ?? string.Empty;
        }

        private bool _hasSubItemsOnLayer(VideoLayer layer)
        {
            return Event.SubEvents.Any(e => e.Layer == layer && e.EventType == TEventType.StillImage);
        }

    }
}
