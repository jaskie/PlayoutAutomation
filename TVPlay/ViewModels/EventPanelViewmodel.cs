using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Data;
using System.Collections;
using System.Diagnostics;
using System.Windows;
using System.Runtime.Remoting.Messaging;
using TAS.Common;
using TAS.Server;
using TAS.Client.Common;


namespace TAS.Client.ViewModels
{
    public class EventPanelViewmodel : ViewmodelBase
    {
        readonly Event _event;
        readonly Engine _engine;
        readonly int _level;
        EventPanelViewmodel _parent;
        readonly EventPanelViewmodel _root;
        readonly EngineViewmodel _engineViewmodel;
        private readonly RationalNumber _frameRate;
        ObservableCollection<EventPanelViewmodel> _childrens = new ObservableCollection<EventPanelViewmodel>();
        static readonly EventPanelViewmodel DummyChild = new EventPanelViewmodel((Event) null, null);

        public ICommand CommandCut { get { return _engineViewmodel.CommandCutSelected; } }
        public ICommand CommandCopy { get { return _engineViewmodel.CommandCopySelected; } }
        public ICommand CommandPaste { get { return _engineViewmodel.CommandPasteSelected; } }
        public ICommand CommandToggleHold { get; private set; }
        public ICommand CommandHide { get; private set; }
        public ICommand CommandShow { get; private set; }
        
        
        
        public EventPanelViewmodel(Server.Engine engine, EngineViewmodel engineViewmodel)
        {
            _engine = engine;
            engine.EventSaved += _onEventSaved;
            _engineViewmodel = engineViewmodel;
            _root = this;
            _level = 0;
            _isExpanded = true;
            _frameRate = engine.FrameRate;
            foreach (Event se in engine.RootEvents.ToList())
                _addRootEvent(se);
        }

        private EventPanelViewmodel(Event aEvent, EventPanelViewmodel parent)
        {
            if (aEvent == null) // dummy child
                return;
            _frameRate = aEvent.Engine.FormatDescription.FrameRate;
            _event = aEvent;
            _parent = parent;
            _root = parent._root;
            _engineViewmodel = parent._engineViewmodel;
            _level = (_parent == null) ? 0 : _parent._level + 1;
            if (aEvent.SubEvents.Count() > 0)
                _childrens.Add(DummyChild);
            _event.PropertyChanged += _onPropertyChanged;
            _event.Deleted += _eventDeleted;
            _event.SubEventChanged += _onSubeventChanged;
            _event.Relocated += _onRelocated;
            Media = aEvent.Media;
            _engine = _event.Engine;
            _createCommands();
        }
#if DEBUG
        ~EventPanelViewmodel ()
        {
            Debug.WriteLine(this, "EventPanelViewmodel Finalized");
        }
#endif // DEBUG

        protected override void OnDispose()
        {
            IsMultiSelected = false;
            ClearChildrens();
            if (_parent != null)
            {
                _parent._childrens.Remove(this);
                _parent = null;
            }
            if (_event != null)
            {
                _engine.EventSaved -= _onEventSaved;
                _event.PropertyChanged -= _onPropertyChanged;
                _event.Deleted -= _eventDeleted;
                _event.SubEventChanged -= _onSubeventChanged;
                _event.Relocated -= _onRelocated;
                Media = null; // unregister media propertychanged event
            }
            Debug.WriteLine(this, "EventPanelViewmodel Disposed");
        }

        protected void _createCommands()
        {
            CommandHide = new UICommand()
            {
                ExecuteDelegate = o =>
                {
                    _event.Enabled = false;
                    _event.Save();
                },
                CanExecuteDelegate = o => _event._eventType == TEventType.Container && _event.Enabled == true
            };
            CommandShow = new UICommand()
            {
                ExecuteDelegate = o =>
                    {
                        _event.Enabled = true;
                        _event.Save();
                    },
                CanExecuteDelegate = o => _event._eventType == TEventType.Container && _event.Enabled == false
            };
        }

        private void _addRootEvent(Event e)
        {
            if (!e.IsDeleted)
            {
                EngineViewmodel evm = _engineViewmodel;
                var newEvm = new EventPanelViewmodel(e, _root);
                _childrens.Add(newEvm);
                Event ne = e.Next;
                while (ne != null)
                {
                    _childrens.Add(new EventPanelViewmodel(ne, _root));
                    Debug.WriteLine(ne, "Reading next for");
                    ne = ne.Next;
                }
            }
        }

        private void _onEventSaved(object o, EventArgs e) // when new event was created
        {
                Application.Current.Dispatcher.BeginInvoke((Action<bool>)delegate(bool onUIThread)
                    {
                        var evm = _root.Find(o as Event);
                        EventPanelViewmodel newVm = null;
                        if (evm == null)
                        {
                            var vp = (o as Event).VisualParent;
                            if (vp != null)
                            {
                                var evm_vp = _root.Find(vp);
                                if (evm_vp != null)
                                {
                                    if ((o as Event).EventType == TEventType.Movie || (o as Event).EventType == TEventType.Rundown || (o as Event).EventType == TEventType.Live
                                        || evm_vp.IsExpanded)
                                    {
                                        evm_vp.IsExpanded = true;
                                        if (evm_vp.Find(o as Event) == null) // find again after expand
                                        {
                                            if ((o as Event).Parent == vp) // StartType = With
                                            {
                                                newVm = new EventPanelViewmodel(o as Event, evm_vp);
                                                evm_vp._childrens.Insert(0, newVm);
                                            }
                                            else // StartType == After
                                            {
                                                var prior = (o as Event).Prior;
                                                if (prior != null)
                                                {
                                                    var evm_prior = evm_vp.Find(prior);
                                                    if (evm_prior != null)
                                                    {
                                                        var pos = evm_vp._childrens.IndexOf(evm_prior);
                                                        newVm = new EventPanelViewmodel(o as Event, evm_vp);
                                                        evm_vp._childrens.Insert(pos + 1, newVm);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (!evm_vp.HasDummyChild)
                                            evm_vp.Childrens.Add(DummyChild);
                                    }
                                }
                            }
                            else //vp == null
                            {
                                var prior = (o as Event).Prior;
                                if (prior != null)
                                {
                                    var evm_prior = _root.Find(prior);
                                    if (evm_prior != null)
                                    {
                                        var pos = _root._childrens.IndexOf(evm_prior);
                                        newVm = new EventPanelViewmodel(o as Event, _root);
                                        _root._childrens.Insert(pos + 1, newVm);
                                    }
                                }
                                else
                                    if ((o as Event).Parent == null)
                                        _addRootEvent(o as Event);
                            }
                        }
                        else //evm != null
                        {
                            evm.NotifyPropertyChanged("IsInvalidInSchedule");
                        }
                        if (onUIThread
                            && newVm != null
                            && !((o as Event).EventType == TEventType.AnimationFlash || (o as Event).EventType == TEventType.StillImage))
                        {
                            newVm.IsSelected = true;
                        }
                    }, System.Windows.Threading.Dispatcher.FromThread(System.Threading.Thread.CurrentThread) != null); // current thread is uiThread
        }

        private void _onRelocated(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)_updateLocation);
            NotifyPropertyChanged("IsInvalidInSchedule");
        }

        private void _eventDeleted(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate()
            {
                IsMultiSelected = false;
                IsSelected = false;
                Dispose();
            }, null);
        }

        private void _onPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Position")
                NotifyPropertyChanged("TimeLeft");
            else
            {
                if (e.PropertyName == "Duration"
                    || e.PropertyName == "Enabled"
                    || e.PropertyName == "Hold"
                    || e.PropertyName == "EventName")
                    NotifyPropertyChanged(e.PropertyName);
                if (e.PropertyName == "ScheduledTC" || e.PropertyName == "Duration")
                {
                    NotifyPropertyChanged("Enabled");
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
                if (e.PropertyName == "Enabled")
                    NotifyPropertyChanged("IsVisible");
                EventPanelViewmodel parent = _parent;
                if (e.PropertyName == "EventName" && parent != null)
                {
                    parent.NotifyPropertyChanged("Layer1SubItemMediaName");
                    parent.NotifyPropertyChanged("Layer2SubItemMediaName");
                    parent.NotifyPropertyChanged("Layer3SubItemMediaName");
                }
            }
        }

        private void _onSubeventChanged(object o, CollectionOperationEventArgs<Event> e)
        {
            if (((o as Event).EventType == TEventType.Live || (o as Event).EventType == TEventType.Movie)
                && (e.Item.EventType == TEventType.StillImage || e.Item.EventType == TEventType.AnimationFlash))
            {

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
            Application.Current.Dispatcher.BeginInvoke((Action)delegate()
            {
                if (e.Operation == TCollectionOperation.Remove && HasDummyChild && _event.SubEvents.Count == 0)
                    Childrens.Remove(DummyChild);
            });
        }

        private Media _media;
        public Media Media
        {
            get { return _media; }
            private set
            {
                Media oldMedia = _media;
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

        public ObservableCollection<EventPanelViewmodel> Childrens
        {
            get { return _childrens; }
        }

        public bool HasDummyChild
        {
            get { return _childrens.Contains(DummyChild); }
        }

        protected void LoadChildrens()
        {
            foreach (Event se in _event.SubEvents)
            {
                _childrens.Add(new EventPanelViewmodel(se, this));
                Event ne = se.Next;
                while (ne != null)
                {
                    _childrens.Add(new EventPanelViewmodel(ne, this));
                    ne = ne.Next;
                }
            }
        }
        protected void ClearChildrens()
        {
            if (this._childrens.Count() > 0)
            {
                if (!HasDummyChild)
                {
                    foreach (var c in _childrens.ToList())
                        c.Dispose();
                    if (Event.SubEvents.Count() > 0)
                        _childrens.Add(DummyChild);
                }
            }
        }


        public int Level { get { return _level; } }

        bool _isExpanded;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;


                    // Expand all the way up to the root.
                    if (value && _parent != null)
                        _parent.IsExpanded = true;

                    // Lazy load the child items, if necessary.
                    if (value && this.HasDummyChild)
                    {
                        this.Childrens.Remove(DummyChild);
                        this.LoadChildrens();
                    }

                    if (!value)
                        ClearChildrens();
                    NotifyPropertyChanged("IsExpanded");
                }
            }
        }

        bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    if (value && _engineViewmodel != null)
                        _engineViewmodel.Selected = this;

                    NotifyPropertyChanged("IsSelected");
                    NotifyPropertyChanged("CommandCut");
                    NotifyPropertyChanged("CommandCopy");
                    NotifyPropertyChanged("CommandPaste");
                }
            }
        }

        bool _isMultiSelected;
        public bool IsMultiSelected
        {
            get { return _isMultiSelected; }
            set {
                if (_isMultiSelected != value)
                {
                    _isMultiSelected = value;
                    NotifyPropertyChanged("IsMultiSelected");
                }
            }
        }

        public EventPanelViewmodel Parent
        {
            get { return _parent; }
            set
            {
                if (value != _parent)
                {
                    _parent._childrens.Remove(this);
                    _parent = value;
                    if (_parent != null)
                    if (_event.Prior != null)
                        _parent.Childrens.Insert(_parent._childrens.IndexOf(_parent._childrens.FirstOrDefault(evm => evm._event == _event.Prior))+1, this);
                    else
                        _parent._childrens.Insert(0, this);
                }
            }
        }
        
        public string EventName
        {
            get { return (_event == null) ? string.Empty : _event.EventName; }
        }

        public string ScheduledTime
        {
            get { return (_event == null) ? string.Empty : _event.ScheduledTime.ToLocalTime().TimeOfDay.ToSMPTETimecodeString(_frameRate); }
        }

        public string Duration
        {
            get { return (_event == null) ? string.Empty: _event.Duration.ToSMPTETimecodeString(_frameRate); }
        }

        public bool Enabled
        {
            get { return (_event == null) ? true : (_event.Enabled && Event.Duration > TimeSpan.Zero) || _event.EventType == TEventType.Container; }
        }

        public bool IsVisible
        {
            get { return (_event == null) ? true : _event.Enabled || _event.EventType != TEventType.Container; }
        }

        public bool Hold
        {
            get { return (_event == null) ? true : _event.Hold; }
        }

        public bool IsNotContainer { get { return (_event == null) ? false : _event.EventType != TEventType.Container; } }

        public string Layer
        {
            get { return (_event == null) ? string.Empty : _event.Layer.ToString(); }
        }

        public decimal AudioVolume { get { return (_event == null || _event.AudioVolume == null) ? 0m : (decimal)_event.AudioVolume; } }

        public TMediaCategory MediaCategory
        {
            get
            {
                Media media = _media;
                if (media == null)
                    return TMediaCategory.Uncategorized;
                else
                    return media.MediaCategory;
            }
        }

        public TMediaEmphasis MediaEmphasis
        {
            get
            {
                Media media = _media;
                if (media == null || !(media is PersistentMedia))
                    return TMediaEmphasis.None;
                else
                    return (media as PersistentMedia).MediaEmphasis;
            }
        }
        
        public TVideoFormat VideoFormat
        {
            get
            {
                Media media = _media;
                if (media == null)
                    return TVideoFormat.Other;
                else
                    return media.VideoFormat;
            }
        }

        public UInt64 IdRundownEvent { get { return (_event == null) ? 0 : _event.IdRundownEvent; } }

        public TPlayState PlayState { get { return (_event == null) ? TPlayState.Scheduled : _event.PlayState; } }

        public TMediaErrorInfo MediaErrorInfo
        {
            get
            {
                if (_event == null || _event.EventType == TEventType.Live || _event.EventType == TEventType.Rundown || _event.EventType == TEventType.Container)
                    return TMediaErrorInfo.NoError;
                else
                {
                    Media media = _event.ServerMediaPGM;
                    if (media == null || media.MediaStatus == TMediaStatus.Deleted)
                        return TMediaErrorInfo.Missing;
                    else
                        if (media.MediaStatus == TMediaStatus.Available)
                            if (   media.MediaType == TMediaType.Still
                                || media.MediaType == TMediaType.AnimationFlash
                                || _event.ScheduledTC + _event.Duration <= media.TCStart + media.Duration
                                )
                                return TMediaErrorInfo.NoError;
                    return TMediaErrorInfo.TooShort;
                }
            }
        }

        public TEventType EventType { get { return (_event == null) ? TEventType.Rundown : _event.EventType; } }

        public string MediaFileName
        {
            get {
                if (_event == null)
                    return string.Empty;
                Media media = _event.ServerMediaPGM;
                return (media == null) ? ((_event.EventType == TEventType.Movie || _event.EventType == TEventType.StillImage)? _event.MediaGuid.ToString() :string.Empty) : media.FileName; }
        }
        
        public string TimeLeft
        {
            get { return (_event == null || _event.Position == 0 || _event.TimeLeft == TimeSpan.Zero) ? string.Empty : _event.TimeLeft.ToSMPTETimecodeString(_frameRate); }
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
            get { return (_event != null && (_event.StartType == TStartType.Manual || _event.StartType== TStartType.OnFixedTime)); }
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
            get { return (_event == null) ? false : (_event.EventType == TEventType.StillImage)? _event.Layer == VideoLayer.CG3: _event.SubEvents.ToList().Any(e => e.Layer == VideoLayer.CG3 && e.EventType == TEventType.StillImage); }
        }

        public string Layer1SubItemMediaName
        {
            get
            {
                if (_event != null)
                {
                    Event se = _event.SubEvents.ToList().FirstOrDefault(e => e.Layer == VideoLayer.CG1 && e.EventType == TEventType.StillImage);
                    if (se != null)
                    {
                        Media m = se.Media;
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
                    Event se = _event.SubEvents.ToList().FirstOrDefault(e => e.Layer == VideoLayer.CG2 && e.EventType == TEventType.StillImage);
                    if (se != null)
                    {
                        Media m = se.Media;
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
                    Event se = _event.SubEvents.ToList().FirstOrDefault(e => e.Layer == VideoLayer.CG3 && e.EventType == TEventType.StillImage);
                    if (se != null)
                    {
                        Media m = se.Media;
                        if (m != null)
                            return m.MediaName;
                    }
                }
                return string.Empty;
            }
        }

        public bool HasSubItems
        {
            get { return (_event == null || _event.EventType == TEventType.Live || _event.EventType == TEventType.Movie) ? false : _event.SubEvents.ToList().Any(e => e.EventType == TEventType.StillImage); }
        }


        public bool LayerButtonsEnabled
        {
            get { return IsClipOrRundownOrLive && _event.PlayState == TPlayState.Scheduled; }
        }

        public bool IsClipOrRundownOrLive
        {
            get { return (_event == null) ? false : (_event.EventType == TEventType.Movie || _event.EventType == TEventType.Live || _event.EventType == TEventType.Rundown); }
        }

        public bool IsRundown
        {
            get { return (_event == null) ? false : _event.EventType == TEventType.Rundown; }
        }

        public bool IsInvalidInSchedule
        {
            get
            {
                if (_event == null)
                    return false;
                Event ne = _event.Next;
                Event pe = _event.Prior;
                return !(
                    (ne == null || ne.Prior == _event)
                    && (pe == null || pe.Next == _event)
                    )
                    || _event.EventType == TEventType.Rundown && _event.SubEvents.Count>1;
            }
        }

        public bool IsRootContainer { get { return _event != null && _event.EventType == TEventType.Container; } }

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

        public bool GPICanTrigger { get { return _event != null && _event._gPI.CanTrigger; } }
        public TLogo GPILogo { get { return _event == null ? TLogo.NoLogo : _event._gPI.Logo; } }
        public TCrawl GPICrawl { get { return _event == null ? TCrawl.NoCrawl : _event._gPI.Crawl; } }
        public TParental GPIParental { get { return _event == null ? TParental.None : _event._gPI.Parental ; } }

        public bool OffsetVisible { get { return _event != null && _event.RequestedStartTime != null; } }

        public EventPanelViewmodel Find(Event aEvent, bool expandParents = false)
        {
            if (expandParents && !IsExpanded)
                IsExpanded = true;
            foreach (EventPanelViewmodel m in _childrens)
            {
                if (m._event == aEvent)
                    return m;
                if (m.Contains(aEvent))
                    return m.Find(aEvent, expandParents);
            }
            return null;
        }

        private void _updateLocation()
        {
            if (_event != null)
            {
                Event prior = _event.Prior;
                Event parent = _event.Parent;
                Event next = _event.Next;
                Event visualParent = _event.VisualParent;
                if (prior != null)
                {
                    int index = _parent._childrens.IndexOf(this);
                    if (visualParent != _parent._event
                        || index <= 0
                        || _parent._childrens[index - 1]._event != prior)
                    {
                        EventPanelViewmodel priorVm = _root.Find(prior);
                        if (priorVm != null)
                        {
                            EventPanelViewmodel newParent = priorVm._parent;
                            if (_parent == newParent)
                            {
                                int priorIndex = newParent._childrens.IndexOf(priorVm);
                                if (index >= priorIndex)
                                    newParent._childrens.Move(index, priorIndex + 1);
                                else
                                    newParent._childrens.Move(index, priorIndex);
                            }
                            else
                            {
                                _parent._childrens.Remove(this);
                                if (!newParent.HasDummyChild)
                                    newParent._childrens.Insert(newParent._childrens.IndexOf(priorVm) + 1, this);
                                _parent = newParent;
                            }
                        }
                    }
                }
                else
                if (parent == null && next != null)
                {
                    int index = _parent._childrens.IndexOf(this);
                    if (visualParent != _parent._event
                        || index <= 0
                        || _parent._childrens[index]._event != next)
                    {
                        EventPanelViewmodel nextVm = _root.Find(next);
                        if (nextVm != null)
                        {
                            EventPanelViewmodel newParent = nextVm._parent;
                            if (_parent == newParent)
                            {
                                int nextIndex = newParent._childrens.IndexOf(nextVm);
                                if (index >= nextIndex)
                                    newParent._childrens.Move(index, nextIndex);
                                else
                                    newParent._childrens.Move(index, nextIndex -1);
                            }
                            else
                            {
                                _parent._childrens.Remove(this);
                                if (!newParent.HasDummyChild)
                                    newParent._childrens.Insert(newParent._childrens.IndexOf(nextVm), this);
                                _parent = newParent;
                            }
                        }
                    }
                }
                else
                if (parent == null)
                {
                    _parent._childrens.Remove(this);
                    _root._childrens.Add(this);
                    _parent = _root;
                }
                else
                {
                    EventPanelViewmodel parentVm = _root.Find(parent);
                    if (parentVm != null)
                    {
                        if (parentVm == _parent)
                        {
                            if (prior == null)
                                parentVm.Childrens.Move(parentVm.Childrens.IndexOf(this), 0);
                        }
                        else
                            Parent = parentVm;
                    }
                }
                this._bringIntoView();
            }
        }
        
        private void _onMediaPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((_event != null)
                && (sender is Media))
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

        public bool Contains(Event  aEvent)
        {
            foreach (EventPanelViewmodel m in _childrens)
            {
                if (m._event == aEvent)
                    return true;
                if (m.Contains(aEvent))
                    return true;
            }
            return false;
        }

  
        public Event Event { get { return _event; } }

        public override string ToString()
        {
            return _event == null ? "null" : _event.ToString();
        }

        public EventPanelViewmodel Next
        {
            get
            {
                Event ne = _event.Next;
                return ne == null ? null : _parent._childrens.FirstOrDefault(evm => evm._event == ne);
            }
        }

        public EventPanelView View { get; set; }

        EventPanelViewmodel _rootOwner
        {
            get
            {
                var result = this;
                while (result.Parent != null && result.Parent._event != null)
                    result = result.Parent;
                return result;
            }
        }

        public string RootOwnerName { get { return _rootOwner.EventName; } }

        internal void SetOnTop()
        {
            var p = Parent;
            if (p != null)
                if (p.IsExpanded)
                {
                    var v = View;
                    if (v != null && _rootOwner.IsVisible)
                        v.SetOnTop();
                }
                else
                    if (p.Enabled) // container can be disabled
                        p.SetOnTop();
        }

        protected void _bringIntoView()
        {
            var p = Parent;
            if (p != null)
                if (p.IsExpanded)
                {
                    var v = View;
                    if (v != null && _rootOwner.IsVisible)
                        v.BringIntoView();
                }
                else
                    if (p.Enabled) // container can be disabled
                        p._bringIntoView();
        }
    }
}
