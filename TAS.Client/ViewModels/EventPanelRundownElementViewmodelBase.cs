using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;

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

            CommandToggleHold = new UiCommand
            (
                CommandName(nameof(CommandToggleHold)),
                _ =>
                {
                    Event.IsHold = !Event.IsHold;
                    Event.Save();
                },
                CanToggleHold
            );
            CommandToggleEnabled = new UiCommand
            (
                CommandName(nameof(CommandToggleEnabled)),
                async _ =>
                {
                    await Task.Run(() => Event.IsEnabled = !Event.IsEnabled);
                    Event.Save();
                },
                _ => Event.PlayState == TPlayState.Scheduled && Event.HaveRight(EventRight.Modify)
            );
            CommandToggleLayer = new UiCommand
            (
                CommandName(nameof(CommandToggleLayer)),
                l =>
                {
                    if (!(l is string layerName) || !Enum.TryParse(layerName, true, out VideoLayer layer))
                        return;
                    if (HasSubItemsOnLayer(layer))
                    {
                        var layerEvent = Event.GetSubEvents().FirstOrDefault(e => e.Layer == layer);
                        layerEvent?.Delete();
                    }
                    else
                        EngineViewmodel.AddMediaEvent(Event, TStartType.WithParent, TMediaType.Still, layer, true);
                },
                CanToggleLayer
            );
            CommandAddNextRundown = new UiCommand
            (
                CommandName(nameof(CommandAddNextRundown)),
                _ => EngineViewmodel.AddSimpleEvent(Event, TEventType.Rundown, false),
                _ =>_canAddNextItem()
            );
            CommandAddNextEmptyMovie = new UiCommand
            (
                CommandName(nameof(CommandAddNextEmptyMovie)),
                _ => EngineViewmodel.AddSimpleEvent(Event, TEventType.Movie, false),
                CanAddNextMovie
            );
            CommandAddNextLive = new UiCommand
            (
                CommandName(nameof(CommandAddNextLive)),
                _ => EngineViewmodel.AddSimpleEvent(Event, TEventType.Live, false),
                CanAddNewLive
            );
            CommandAddNextMovie = new UiCommand
            (
                CommandName(nameof(CommandAddNextMovie)),
                _ => EngineViewmodel.AddMediaEvent(Event, TStartType.After, TMediaType.Movie,
                    VideoLayer.Program, false),
                CanAddNextMovie
            );
            CommandAddAnimation = new UiCommand
            (
                CommandName(nameof(CommandAddAnimation)),
                _ => EngineViewmodel.AddMediaEvent(Event, TStartType.WithParent,
                    TMediaType.Animation, VideoLayer.Animation, true),
                _ => Event.PlayState == TPlayState.Scheduled && Event.HaveRight(EventRight.Modify)
            );
            CommandAddCommandScript = new UiCommand
            (
                CommandName(nameof(CommandAddCommandScript)),
                _ => EngineViewmodel.AddCommandScriptEvent(Event),
                _ => Event.PlayState == TPlayState.Scheduled && Event.HaveRight(EventRight.Modify)
            );
        }

        private bool CanToggleLayer(object _)
        {
            return (Event.PlayState == TPlayState.Scheduled || Event.PlayState == TPlayState.Playing || Event.PlayState == TPlayState.Paused)
                   && Event.HaveRight(EventRight.Modify);
        }

        private bool CanToggleHold(object _)
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


        protected virtual bool CanAddNextMovie(object _)
        {
            return _canAddNextItem();
        }

        protected virtual bool CanAddNewLive(object _)
        {
            return _canAddNextItem();
        }

        bool _canAddNextItem()
        {
            return Event.PlayState != TPlayState.Played 
                   && !Event.IsLoop
                   && Event.HaveRight(EventRight.Create);
        }

        #endregion // Commands

        public TimeSpan TimeLeft 
        {
            get => _timeLeft;
            set => SetField(ref _timeLeft, value);
        }

        public string EndTime => Event == null || Event.GetSuccessor() != null ? string.Empty : Event.EndTime.ToLocalTime().TimeOfDay.ToSmpteTimecodeString(VideoFormat);

        public bool IsLastEvent => Event != null && Event.GetSuccessor() == null;

        public bool IsStartEvent => Event != null && (Event.StartType == TStartType.Manual || Event.StartType == TStartType.OnFixedTime);

        public bool IsAnimationEnabled => EngineViewmodel.IsAnimationDirAvailable;

        public bool IsPlaying => Event != null && Event.PlayState == TPlayState.Playing;

        public bool IsForcedNext => Event != null && Event.IsForcedNext;

        public bool HasSubItemOnLayer1 => HasSubItemsOnLayer(VideoLayer.CG1);

        public bool HasSubItemOnLayer2 => HasSubItemsOnLayer(VideoLayer.CG2);

        public bool HasSubItemOnLayer3 => HasSubItemsOnLayer(VideoLayer.CG3);


        public string Layer1SubItemMediaName => SubItemMediaName(VideoLayer.CG1);
        public string Layer2SubItemMediaName => SubItemMediaName(VideoLayer.CG2);
        public string Layer3SubItemMediaName => SubItemMediaName(VideoLayer.CG3);

        public TimeSpan? Offset => Event.Offset;

        public bool IsInvalidInSchedule
        {
            get
            {
                if (Event == null)
                    return false;
                IEvent ne = Event.GetNext();
                IEvent pe = Event.GetPrior();
                return !(
                    (ne == null || ne.GetPrior() == Event)
                    && (pe == null || pe.GetNext() == Event)
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
                var media = Media;
                return media == null ? ((Event.EventType == TEventType.Movie || Event.EventType == TEventType.StillImage) ? Event.MediaGuid.ToString() : string.Empty) : media.FileName;
            }
        }
        
        public TPlayState PlayState => Event.PlayState;

        public TMediaEmphasis MediaEmphasis => (Media as IPersistentMedia)?.MediaEmphasis ?? TMediaEmphasis.None;

        public string Layer => Event.Layer.ToString();

        public double AudioVolume => Event == null ? 0 : Event.AudioVolume.GetValueOrDefault();

        public TMediaCategory MediaCategory => Media?.MediaCategory ?? TMediaCategory.Uncategorized;

        public bool IsHold => Event.IsHold;

        public bool IsLoop => Event.IsLoop;

        public string ScheduledTime => Event.ScheduledTime.ToLocalTime().TimeOfDay.ToSmpteTimecodeString(VideoFormat);

        public string ScheduledDelay => Event.ScheduledDelay.ToSmpteTimecodeString(VideoFormat);

        public string Duration => Event.Duration.ToSmpteTimecodeString(VideoFormat);

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

        public virtual IMedia Media
        {
            get => _media;
            protected set
            {
                var oldMedia = _media;
                if (!SetField(ref _media, value))
                    return;
                if (oldMedia != null)
                    oldMedia.PropertyChanged -= OnMediaPropertyChanged;
                _media = value;
                if (value != null)
                {
                    value.PropertyChanged += OnMediaPropertyChanged;
                    VideoFormat = value.VideoFormat;
                }
                NotifyPropertyChanged(nameof(MediaFileName));
                NotifyPropertyChanged(nameof(MediaCategory));
                NotifyPropertyChanged(nameof(MediaEmphasis));
            }
        }


        internal void VerifyIsInvalidInSchedule()
        {
            NotifyPropertyChanged(nameof(IsInvalidInSchedule));
        }
        
        internal override void SetOnTop()
        {
            var p = Parent;
            if (p == null)
                return;
            if (p.IsExpanded && View != null)
            {
                View?.SetOnTop();
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
            OnUiThread(() =>
            {
                TimeLeft = e.TimeToFinish;
            });
        }

        protected override void OnEventPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnEventPropertyChanged(sender, e);
            switch (e.PropertyName)
            {
                case nameof(IEvent.IsEnabled):
                case nameof(IEvent.IsHold):
                case nameof(IEvent.EventName):
                case nameof(IEvent.IsLoop):
                case nameof(IEvent.Offset):
                case nameof(IEvent.ScheduledDelay):
                case nameof(IEvent.IsForcedNext):
                case nameof(IEvent.ScheduledTime):
                case nameof(IEvent.IsCGEnabled):
                case nameof(IEvent.Crawl):
                case nameof(IEvent.Logo):
                case nameof(IEvent.Parental):
                case nameof(IEvent.EndTime):
                    NotifyPropertyChanged(e.PropertyName);
                    break;
                case nameof(IEvent.Duration):
                    NotifyPropertyChanged(nameof(Duration));
                    NotifyPropertyChanged(nameof(IsEnabled));
                    break;
                case nameof(IEvent.PlayState):
                    NotifyPropertyChanged(nameof(PlayState));
                    NotifyPropertyChanged(nameof(IsPlaying));
                    break;
                case nameof(IEvent.StartType):
                    NotifyPropertyChanged(nameof(IsStartEvent));
                    NotifyPropertyChanged(nameof(IsFixedTimeStart));
                    break;
                case nameof(IEvent.Media):
                    Media = Event.Media;
                    break;
            }
        }
        protected override void OnDispose()
        {
            base.OnDispose();
            Media = null;
            Event.PositionChanged -= EventPositionChanged;
            Event.SubEventChanged -= OnSubeventChanged;
        }

        protected virtual void OnMediaPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Event == null || !(sender is IMedia))
                return;
            if (e.PropertyName == nameof(IMedia.FileName))
                NotifyPropertyChanged(nameof(MediaFileName));
            if (e.PropertyName == nameof(IMedia.MediaCategory)
                || e.PropertyName == nameof(IPersistentMedia.MediaEmphasis))
                NotifyPropertyChanged(e.PropertyName);
            if (e.PropertyName == nameof(IMedia.VideoFormat))
                VideoFormat = ((IMedia)sender).VideoFormat;
        }

        private string SubItemMediaName(VideoLayer layer)
        {
            var se = Event?.GetSubEvents().FirstOrDefault(e => e.Layer == layer && e.EventType == TEventType.StillImage);
            var m = se?.Media;
            return m?.MediaName ?? string.Empty;
        }

        private bool HasSubItemsOnLayer(VideoLayer layer)
        {
            return Event.GetSubEvents().Any(e => e.Layer == layer && e.EventType == TEventType.StillImage);
        }

    }
}
