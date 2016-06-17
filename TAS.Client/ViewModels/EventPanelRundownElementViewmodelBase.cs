using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
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

        protected override void OnEventSaved(object sender, EventArgs e)
        {
            base.OnEventSaved(sender, e);
            NotifyPropertyChanged("IsInvalidInSchedule");
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
        public ICommand CommandMoveUp { get; private set; }
        public ICommand CommandMoveDown { get; private set; }


        public ICommand CommandToggleLayer { get; private set; }
        public ICommand CommandAddNextRundown { get; private set; }
        public ICommand CommandAddNextMovie { get; private set; }
        public ICommand CommandAddNextEmptyMovie { get; private set; }
        public ICommand CommandAddNextLive { get; private set; }
        public ICommand CommandAddAnimation { get; private set; }

        protected override void _createCommands()
        {
            base._createCommands();
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
                        _engineViewmodel.AddMediaEvent(_event, TStartType.With, TMediaType.Still, layer, true);
                },
                CanExecuteDelegate = (o) => _event.PlayState == TPlayState.Scheduled || _event.PlayState == TPlayState.Playing || _event.PlayState == TPlayState.Paused
            };
            CommandMoveUp = new UICommand() { ExecuteDelegate = (o) => _event.MoveUp(), CanExecuteDelegate = _canMoveUp };
            CommandMoveDown = new UICommand() { ExecuteDelegate = o => _event.MoveDown(), CanExecuteDelegate = _canMoveDown };
            CommandAddNextRundown = new UICommand()
            {
                ExecuteDelegate = o =>
                {
                    IEvent newEvent = _event.Engine.AddNewEvent(
                        eventType: TEventType.Rundown,
                        eventName: resources._title_NewRundown);
                    _event.InsertAfter(newEvent);
                },
                CanExecuteDelegate = _canAddNextItem
            };
            CommandAddNextEmptyMovie = new UICommand()
            {
                ExecuteDelegate = o =>
                {
                    IEvent newEvent = _event.Engine.AddNewEvent(
                        eventType: TEventType.Movie,
                        eventName: resources._title_EmptyMovie,
                        videoLayer: VideoLayer.Program);
                    _event.InsertAfter(newEvent);
                },
                CanExecuteDelegate = canAddNextMovie
            };
            CommandAddNextLive = new UICommand()
            {
                ExecuteDelegate = o =>
                {
                    IEvent newEvent = _event.Engine.AddNewEvent(
                        eventType: TEventType.Live,
                        eventName: resources._title_NewLive,
                        videoLayer: VideoLayer.Program,
                        duration: new TimeSpan(0, 10, 0));
                    _event.InsertAfter(newEvent);
                },
                CanExecuteDelegate = canAddNewLive
            };
            CommandAddNextMovie = new UICommand()
            {
                ExecuteDelegate = o => _engineViewmodel.AddMediaEvent(_event, TStartType.After, TMediaType.Movie, VideoLayer.Program, false),
                CanExecuteDelegate = canAddNextMovie
            };
            CommandAddAnimation = new UICommand()
            {
                ExecuteDelegate = o => _engineViewmodel.AddMediaEvent(_event, TStartType.With, TMediaType.Animation, VideoLayer.Animation, true)
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

        bool _canMoveUp(object o)
        {
            IEvent prior = _event.Prior;
            return prior != null && prior.PlayState == TPlayState.Scheduled && _event.PlayState == TPlayState.Scheduled && !IsLoop
                && (prior.StartType == TStartType.After || !IsHold);
        }

        bool _canMoveDown(object o)
        {
            IEvent next = _event.Next;
            return next != null && next.PlayState == TPlayState.Scheduled && _event.PlayState == TPlayState.Scheduled && !next.IsLoop
                && (_event.StartType == TStartType.After || !next.IsHold);
        }
        #endregion // Commands

        private string _timeLeft = string.Empty;
        public string TimeLeft
        {
            get { return _timeLeft; }
            set { SetField(ref _timeLeft, value, "TimeLeft"); }
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
            get { return _event != null && (_event.StartType == TStartType.Manual || _event.StartType == TStartType.OnFixedTime); }
        }

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

        public bool GPICanTrigger { get { return _event != null && _event.GPI.CanTrigger; } }
        public TLogo GPILogo { get { return _event == null ? TLogo.NoLogo : _event.GPI.Logo; } }
        public TCrawl GPICrawl { get { return _event == null ? TCrawl.NoCrawl : _event.GPI.Crawl; } }
        public TParental GPIParental { get { return _event == null ? TParental.None : _event.GPI.Parental; } }

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

        public string Duration
        {
            get { return _event.Duration.ToSMPTETimecodeString(_frameRate); }
        }

        public virtual bool IsEnabled
        {
            get { return _event.IsEnabled && Event.Duration > TimeSpan.Zero; }
        }

        public EventPanelViewmodelBase Next
        {
            get
            {
                IEvent ne = _event.Next;
                return ne == null ? null : _parent.Childrens.FirstOrDefault(evm => evm.Event == ne);
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
                if (e.PropertyName == "MediaStatus")
                    NotifyPropertyChanged("MediaErrorInfo");
                if (e.PropertyName == "FileName")
                    NotifyPropertyChanged("MediaFileName");
                if (e.PropertyName == "MediaCategory"
                    || e.PropertyName == "VideoFormat"
                    || e.PropertyName == "MediaEmphasis"
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
                    NotifyPropertyChanged("HasSubItemOnLayer1");
                    NotifyPropertyChanged("Layer1SubItemMediaName");
                    break;
                case VideoLayer.CG2:
                    NotifyPropertyChanged("HasSubItemOnLayer2");
                    NotifyPropertyChanged("Layer2SubItemMediaName");
                    break;
                case VideoLayer.CG3:
                    NotifyPropertyChanged("HasSubItemOnLayer3");
                    NotifyPropertyChanged("Layer3SubItemMediaName");
                    break;
            }
        }

        protected void _eventPositionChanged(object sender, EventPositionEventArgs e)
        {
            TimeLeft = (e.TimeToFinish == TimeSpan.Zero || _event.PlayState == TPlayState.Scheduled) ? string.Empty : e.TimeToFinish.ToSMPTETimecodeString(_frameRate);
        }

        protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(sender, e);
            if (e.PropertyName == "Duration"
                || e.PropertyName == "IsEnabled"
                || e.PropertyName == "IsHold"
                || e.PropertyName == "EventName"
                || e.PropertyName == "IsLoop"
                || e.PropertyName == "Offset"
                || e.PropertyName == "IsForcedNext")
                NotifyPropertyChanged(e.PropertyName);
            if (e.PropertyName == "ScheduledTC" || e.PropertyName == "Duration")
            {
                NotifyPropertyChanged("IsEnabled");
                NotifyPropertyChanged("EndTime");
                NotifyPropertyChanged("MediaErrorInfo");
            }
            if (e.PropertyName == "PlayState")
            {
                NotifyPropertyChanged(e.PropertyName);
                NotifyPropertyChanged("ScheduledTime");
                NotifyPropertyChanged("EndTime");
                NotifyPropertyChanged("IsPlaying");
            }
            if (e.PropertyName == "ScheduledTime")
            {
                NotifyPropertyChanged(e.PropertyName);
                NotifyPropertyChanged("EndTime");
            }
            if (e.PropertyName == "StartType")
                NotifyPropertyChanged("IsStartEvent");
            if (e.PropertyName == "Media")
            {
                Media = _event.Media;
                NotifyPropertyChanged("MediaFileName");
                NotifyPropertyChanged("MediaCategory");
                NotifyPropertyChanged("MediaEmphasis");
                NotifyPropertyChanged("VideoFormat");
                NotifyPropertyChanged("MediaErrorInfo");
            }
            if (e.PropertyName == "GPI")
            {
                NotifyPropertyChanged("GPICanTrigger");
                NotifyPropertyChanged("GPICrawl");
                NotifyPropertyChanged("GPILogo");
                NotifyPropertyChanged("GPIParental");
            }
        }




    }
}
