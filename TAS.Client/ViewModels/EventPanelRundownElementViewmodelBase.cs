using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TAS.Client.Common;
using TAS.Common;
using TAS.Server.Common;
using TAS.Server.Interfaces;
using resources = TAS.Client.Common.Properties.Resources;

namespace TAS.Client.ViewModels
{
    public abstract class EventPanelRundownElementViewmodelBase : EventPanelViewmodelBase
    {
        public EventPanelRundownElementViewmodelBase(IEvent ev, EventPanelViewmodelBase parent) : base(ev, parent) {
            Media = ev.Media;
            ev.PositionChanged += _eventPositionChanged;
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            Media = null;
            _event.PositionChanged -= _eventPositionChanged;
        }

        #region Commands
        public ICommand CommandCut { get { return _engineViewmodel.CommandCutSelected; } }
        public ICommand CommandCopy { get { return _engineViewmodel.CommandCopySelected; } }
        public ICommand CommandPaste { get { return _engineViewmodel.CommandPasteSelected; } }
        public ICommand CommandToggleHold { get; private set; }
        public ICommand CommandToggleEnabled { get; private set; }


        public ICommand CommandToggleLayer { get; private set; }
        public ICommand CommandAddNextRundown { get; private set; }
        public ICommand CommandAddNextMovie { get; private set; }
        public ICommand CommandAddNextEmptyMovie { get; private set; }
        public ICommand CommandAddNextLive { get; private set; }
        public ICommand CommandAddAnimation { get; private set; }
        public ICommand CommandAddCommandScript { get; private set; }

        protected override void CreateCommands()
        {
            base.CreateCommands();
            CommandToggleHold = new UICommand()
            {
                ExecuteDelegate = (o) =>
                {
                    _event.IsHold = !_event.IsHold;
                    _event.Save();
                },
                CanExecuteDelegate = (o) => _event.PlayState == TPlayState.Scheduled && _event.StartType == TStartType.After
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
                    VideoLayer layer = (VideoLayer)sbyte.Parse((string)l);
                    if (_hasSubItemsOnLayer(layer))
                    {
                        var layerEvent = _event.SubEvents.FirstOrDefault(e => e.Layer == layer);
                        if (layerEvent != null)
                            layerEvent.Delete();
                    }
                    else
                        _engineViewmodel.AddMediaEvent(_event, TStartType.WithParent, TMediaType.Still, layer, true);
                },
                CanExecuteDelegate = (o) => _event.PlayState == TPlayState.Scheduled || _event.PlayState == TPlayState.Playing || _event.PlayState == TPlayState.Paused
            };
            CommandAddNextRundown = new UICommand()
            {
                ExecuteDelegate = o => _engineViewmodel.AddSimpleEvent(_event, TEventType.Rundown, false),
                CanExecuteDelegate = _canAddNextItem
            };
            CommandAddNextEmptyMovie = new UICommand()
            {
                ExecuteDelegate = o => _engineViewmodel.AddSimpleEvent(_event, TEventType.Movie, false),
                CanExecuteDelegate = canAddNextMovie
            };
            CommandAddNextLive = new UICommand()
            {
                ExecuteDelegate = o => _engineViewmodel.AddSimpleEvent(_event, TEventType.Live, false),
                CanExecuteDelegate = canAddNewLive
            };
            CommandAddNextMovie = new UICommand()
            {
                ExecuteDelegate = o => _engineViewmodel.AddMediaEvent(_event, TStartType.After, TMediaType.Movie, VideoLayer.Program, false),
                CanExecuteDelegate = canAddNextMovie
            };
            CommandAddAnimation = new UICommand()
            {
                ExecuteDelegate = o => _engineViewmodel.AddMediaEvent(_event, TStartType.WithParent, TMediaType.Animation, VideoLayer.Animation, true),
                CanExecuteDelegate = o => _event.PlayState == TPlayState.Scheduled
            };
            CommandAddCommandScript = new UICommand
            {
                ExecuteDelegate = o => _engineViewmodel.AddCommandScriptEvent(_event),
                CanExecuteDelegate = o => _event.PlayState == TPlayState.Scheduled
            };
        }

        protected virtual bool canAddNextMovie(object o)
        {
            return _canAddNextItem(o);
        }

        protected virtual bool canAddNewLive(object o)
        {
            return _canAddNextItem(o);
        }

        bool _canAddNextItem(object o)
        {
            return _event.PlayState != TPlayState.Played && !_event.IsLoop;
        }

        #endregion // Commands

        private string _timeLeft = string.Empty;
        public string TimeLeft
        {
            get { return _timeLeft; }
            set { SetField(ref _timeLeft, value, nameof(TimeLeft)); }
        }

        public string EndTime
        {
            get { return (_event == null || _event.GetSuccessor() != null) ? string.Empty : _event.EndTime.ToLocalTime().TimeOfDay.ToSMPTETimecodeString(_frameRate); }
        }

        public bool IsLastEvent
        {
            get { return (_event != null && _event.GetSuccessor() == null); }
        }

        public bool IsStartEvent
        {
            get
            {
                return _event != null && (_event.StartType == TStartType.Manual || _event.StartType == TStartType.OnFixedTime);
            }
        }

        public bool IsAnimationEnabled { get { return _engineViewmodel.IsAnimationDirAvailable; } }

        public bool IsPlaying
        {
            get { return _event != null && _event.PlayState == TPlayState.Playing; }
        }

        public bool IsForcedNext
        {
            get { return _event != null && _event.IsForcedNext; }
        }

        private bool _hasSubItemsOnLayer(VideoLayer layer)
        {
            return _event.SubEvents.Any(e => e.Layer == layer && e.EventType == TEventType.StillImage);
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

        public string Layer1SubItemMediaName { get { return _subItemMediaName(VideoLayer.CG1); } }
        public string Layer2SubItemMediaName { get { return _subItemMediaName(VideoLayer.CG2); } }
        public string Layer3SubItemMediaName { get { return _subItemMediaName(VideoLayer.CG3); } }
        
        public TimeSpan? Offset { get { return _event.Offset; } }

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

        public bool IsCGEnabled { get { return _event?.IsCGEnabled == true && _engine?.CGElementsController != null; } }
        public CGElementViewmodel Logo { get { return  _engineViewmodel.CGElementsControllerViewmodel?.Logos?.FirstOrDefault(l => l.Id == _event.Logo); } }
        public CGElementViewmodel Parental { get { return _engineViewmodel.CGElementsControllerViewmodel?.Parentals?.FirstOrDefault(l => l.Id == _event.Parental); } }
        public CGElementViewmodel Crawl { get { return _engineViewmodel.CGElementsControllerViewmodel?.Crawls?.FirstOrDefault(l => l.Id == _event.Crawl); } }

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
                if (_event == null || _event.EventType == TEventType.Live || _event.EventType == TEventType.Rundown || _event.EventType == TEventType.Container)
                    return TMediaErrorInfo.NoError;
                else
                {
                    IMedia media = _event.Media;
                    if (media == null || media.MediaStatus == TMediaStatus.Deleted)
                        return TMediaErrorInfo.Missing;
                    else
                        if (media.MediaStatus == TMediaStatus.Available)
                        if (media.MediaType == TMediaType.Still
                            || media.MediaType == TMediaType.Animation
                            || _event.ScheduledTc + _event.Duration <= media.TcStart + media.Duration
                            )
                            return TMediaErrorInfo.NoError;
                    return TMediaErrorInfo.TooShort;
                }
            }
        }

        public TPlayState PlayState { get { return _event.PlayState; } }

        public TMediaEmphasis MediaEmphasis
        {
            get
            {
                IMedia media = _event.Media;
                if (media == null || !(media is IPersistentMedia))
                    return TMediaEmphasis.None;
                else
                    return (media as IPersistentMedia).MediaEmphasis;
            }
        }

        public TVideoFormat VideoFormat
        {
            get
            {
                IMedia media = _event.Media;
                if (media == null)
                    return TVideoFormat.Other;
                else
                    return media.VideoFormat;
            }
        }

        public string Layer
        {
            get { return _event.Layer.ToString(); }
        }

        public decimal AudioVolume { get { return (_event == null || _event.AudioVolume == null) ? 0m : (decimal)_event.AudioVolume; } }

        public TMediaCategory MediaCategory
        {
            get
            {
                IMedia media = _media;
                if (media == null)
                    return TMediaCategory.Uncategorized;
                else
                    return media.MediaCategory;
            }
        }

        public bool IsHold
        {
            get { return _event.IsHold; }
        }

        public bool IsLoop
        {
            get { return _event.IsLoop; }
        }

        public string ScheduledTime
        {
            get { return _event.ScheduledTime.ToLocalTime().TimeOfDay.ToSMPTETimecodeString(_frameRate); }
        }

        public string ScheduledDelay
        {
            get { return _event.ScheduledDelay.ToSMPTETimecodeString(_frameRate); }
        }

        public string Duration
        {
            get { return _event.Duration.ToSMPTETimecodeString(_frameRate); }
        }

        public virtual bool IsEnabled
        {
            get
            {
                var et = Event.EventType;
                return _event.IsEnabled && (Event.Duration > TimeSpan.Zero || et == TEventType.Animation || et == TEventType.CommandScript);
            }
        }

        public bool IsFixedTimeStart { get { return _event.StartType == TStartType.OnFixedTime; } }

        public EventPanelViewmodelBase Prior
        {
            get
            {
                int index = _parent.Childrens.IndexOf(this) - 1;
                if (index >= 0 && _parent.Childrens.Count > index)
                    return _parent.Childrens[index];
                else return null;
            }
        }

        public EventPanelViewmodelBase Next
        {
            get
            {
                int index = _parent.Childrens.IndexOf(this) +1;
                if (index > 0 && _parent.Childrens.Count > index)
                    return _parent.Childrens[index];
                else return null;
            }
        }

        private IMedia _media;
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
                        value.PropertyChanged += _onMediaPropertyChanged;
                }
            }
        }

        private void _onMediaPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((_event != null)
                && (sender is IMedia))
            {
                if (e.PropertyName == nameof(IMedia.MediaStatus))
                    NotifyPropertyChanged(nameof(MediaErrorInfo));
                if (e.PropertyName == nameof(IMedia.FileName))
                    NotifyPropertyChanged(nameof(MediaFileName));
                if (e.PropertyName == nameof(IMedia.MediaCategory)
                    || e.PropertyName == nameof(IMedia.VideoFormat)
                    || e.PropertyName == nameof(IPersistentMedia.MediaEmphasis)
                    )
                    NotifyPropertyChanged(e.PropertyName);
            }
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
            TimeLeft = (e.TimeToFinish == TimeSpan.Zero || _event.PlayState == TPlayState.Scheduled) ? string.Empty : e.TimeToFinish.ToSMPTETimecodeString(_frameRate);
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
    }
}
