using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using TAS.Common;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.ViewModels
{
    public abstract class EventPanelRundownElementViewmodelBase: EventPanelViewmodelBase
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

        public ICommand CommandCut { get { return _engineViewmodel.CommandCutSelected; } }
        public ICommand CommandCopy { get { return _engineViewmodel.CommandCopySelected; } }
        public ICommand CommandPaste { get { return _engineViewmodel.CommandPasteSelected; } }
        public ICommand CommandToggleHold { get; private set; }



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
            get { return (_event != null && (_event.StartType == TStartType.Manual || _event.StartType == TStartType.OnFixedTime)); }
        }

        public bool IsPlaying
        {
            get { return _event != null && (_event.PlayState == TPlayState.Playing); }
        }

        public bool HasSubItemOnLayer1
        {
            get { return (_event == null) ? false : (_event.EventType == TEventType.StillImage) ? _event.Layer == VideoLayer.CG1 : _event.SubEvents.ToList().Any(e => e.Layer == VideoLayer.CG1 && e.EventType == TEventType.StillImage); }
        }
        public bool HasSubItemOnLayer2
        {
            get { return (_event == null) ? false : (_event.EventType == TEventType.StillImage) ? _event.Layer == VideoLayer.CG2 : _event.SubEvents.ToList().Any(e => e.Layer == VideoLayer.CG2 && e.EventType == TEventType.StillImage); }
        }
        public bool HasSubItemOnLayer3
        {
            get { return (_event == null) ? false : (_event.EventType == TEventType.StillImage) ? _event.Layer == VideoLayer.CG3 : _event.SubEvents.ToList().Any(e => e.Layer == VideoLayer.CG3 && e.EventType == TEventType.StillImage); }
        }

        public string Layer1SubItemMediaName
        {
            get
            {
                if (_event != null)
                {
                    IEvent se = _event.SubEvents.ToList().FirstOrDefault(e => e.Layer == VideoLayer.CG1 && e.EventType == TEventType.StillImage);
                    if (se != null)
                    {
                        IMedia m = se.Media;
                        if (m != null)
                            return m.MediaName;
                    }
                }
                return string.Empty;
            }
        }
        public string Layer2SubItemMediaName
        {
            get
            {
                if (_event != null)
                {
                    IEvent se = _event.SubEvents.ToList().FirstOrDefault(e => e.Layer == VideoLayer.CG2 && e.EventType == TEventType.StillImage);
                    if (se != null)
                    {
                        IMedia m = se.Media;
                        if (m != null)
                            return m.MediaName;
                    }
                }
                return string.Empty;
            }
        }
        public string Layer3SubItemMediaName
        {
            get
            {
                if (_event != null)
                {
                    IEvent se = _event.SubEvents.ToList().FirstOrDefault(e => e.Layer == VideoLayer.CG3 && e.EventType == TEventType.StillImage);
                    if (se != null)
                    {
                        IMedia m = se.Media;
                        if (m != null)
                            return m.MediaName;
                    }
                }
                return string.Empty;
            }
        }

        public bool OffsetVisible { get { return _event != null && _event.RequestedStartTime != null; } }
        public TimeSpan? Offset
        {
            get
            {
                if (_event != null)
                {
                    var rrt = _event.RequestedStartTime;
                    if (rrt != null)
                        return _event.ScheduledTime.ToLocalTime().TimeOfDay - rrt;
                }
                return null;
            }
        }

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
                    || _event.EventType == TEventType.Rundown && _event.SubEvents.Count > 1;
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
                            || media.MediaType == TMediaType.AnimationFlash
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

        public bool IsEnabled
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

        bool _isMultiSelected;
        public bool IsMultiSelected
        {
            get { return _isMultiSelected; }
            set
            {
                if (_isMultiSelected != value)
                {
                    _isMultiSelected = value;
                    NotifyPropertyChanged("IsMultiSelected");
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
                if (p.IsExpanded)
                {
                    View.SetOnTop();
                }
                else
                    p.SetOnTop();

        }

        protected override void _onSubeventChanged(object o, CollectionOperationEventArgs<IEvent> e)
        {
            base._onSubeventChanged(o, e);
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

        protected override void _onPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base._onPropertyChanged(sender, e);
            if (e.PropertyName == "Duration"
                || e.PropertyName == "IsEnabled"
                || e.PropertyName == "IsHold"
                || e.PropertyName == "EventName"
                || e.PropertyName == "IsLoop")
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
                NotifyPropertyChanged("Offset");
            }
            if (e.PropertyName == "StartType")
                NotifyPropertyChanged("IsStartEvent");
            if (e.PropertyName == "RequestedStartTime")
            {
                NotifyPropertyChanged("Offset");
                NotifyPropertyChanged("OffsetVisible");
            }
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
            if (e.PropertyName == "IsEnabled")
                NotifyPropertyChanged("IsVisible");
            EventPanelViewmodelBase parent = _parent;
            if (e.PropertyName == "EventName" && parent != null)
            {
                parent.NotifyPropertyChanged2("Layer1SubItemMediaName");
                parent.NotifyPropertyChanged2("Layer2SubItemMediaName");
                parent.NotifyPropertyChanged2("Layer3SubItemMediaName");
            }

        }


    }
}
