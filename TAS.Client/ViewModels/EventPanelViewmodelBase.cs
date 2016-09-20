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
using TAS.Client.Common;
using TAS.Server.Interfaces;
using TAS.Server.Common;
using resources = TAS.Client.Common.Properties.Resources;


namespace TAS.Client.ViewModels
{
    public abstract class EventPanelViewmodelBase : ViewmodelBase
    {
        protected readonly IEvent _event;
        protected readonly IEngine _engine;
        readonly int _level;
        protected EventPanelViewmodelBase _parent;
        protected readonly EventPanelRootViewmodel _root;
        protected readonly EngineViewmodel _engineViewmodel;
        protected readonly RationalNumber _frameRate;
        protected readonly ObservableCollection<EventPanelViewmodelBase> _childrens = new ObservableCollection<EventPanelViewmodelBase>();
        protected static readonly EventPanelViewmodelBase DummyChild = new EventPanelDummyViewmodel();
        public ICommand CommandDelete { get; private set; }


        /// <summary>
        /// Constructor for root event
        /// </summary>
        /// <param name="engineViewmodel"></param>
        public EventPanelViewmodelBase(EngineViewmodel engineViewmodel) : base()
        {
            _engineViewmodel = engineViewmodel;
            _engine = engineViewmodel.Engine;
            _level = 0;
            _isExpanded = true;
            _frameRate = engineViewmodel.FrameRate;
            _root = (EventPanelRootViewmodel)this;
        }

        /// <summary>
        /// Constructor for child events
        /// </summary>
        /// <param name="aEvent"></param>
        /// <param name="parent"></param>
        protected EventPanelViewmodelBase(IEvent aEvent, EventPanelViewmodelBase parent) : base()
        {
            if (aEvent == null) // dummy child
                return;
            _engine = aEvent.Engine;
            _frameRate = _engine.FrameRate;
            _event = aEvent;
            if (parent != null)
            {
                _parent = parent;
                _root = parent._root;
                _engineViewmodel = parent._engineViewmodel;
                _level = (_parent == null) ? 0 : _parent._level + 1;
                if (aEvent.SubEventsCount > 0)
                    _childrens.Add(DummyChild);
            }
            _event.PropertyChanged += OnEventPropertyChanged;
            _event.Deleted += _eventDeleted;
            _event.SubEventChanged += OnSubeventChanged;
            _event.Relocated += OnRelocated;
            _event.Saved += OnEventSaved;
            _createCommands();
        }

        protected virtual void OnEventSaved(object sender, EventArgs e)
        {
            
        }

        protected override void OnDispose()
        {
            if (_parent != null)
            {
                _parent._childrens.Remove(this);
                _parent = null;
            }
            ClearChildrens();
            if (_event != null)
            {
                _event.PropertyChanged -= OnEventPropertyChanged;
                _event.Deleted -= _eventDeleted;
                _event.SubEventChanged -= OnSubeventChanged;
                _event.Relocated -= OnRelocated;
                _event.Saved -= OnEventSaved;
                _engineViewmodel?.RemoveSelected(this);
                IsMultiSelected = false;
            }
            Debug.WriteLine(this, "EventPanelViewmodel Disposed");
        }

        protected virtual void _createCommands()
        {
            CommandDelete = new UICommand
            {
                ExecuteDelegate = o =>
                {
                    if (_event != null && MessageBox.Show(resources._query_DeleteItem, resources._caption_Confirmation, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                        _event.Delete();
                },
                CanExecuteDelegate = o => _event != null && _event.AllowDelete()
            };
        }

        internal EventPanelViewmodelBase CreateChildEventPanelViewmodelForEvent(IEvent ev)
        {
            switch (ev.EventType)
            {
                case TEventType.Rundown:
                    return new EventPanelRundownViewmodel(ev, this);
                case TEventType.Container:
                    return new EventPanelContainerViewmodel(ev, this);
                case TEventType.Movie:
                    return new EventPanelMovieViewmodel(ev, this);
                case TEventType.Live:
                    return new EventPanelLiveViewmodel(ev, this);
                case TEventType.StillImage:
                    return new EventPanelStillViewmodel(ev, this);
                case TEventType.Animation:
                    return new EventPanelAnimationViewmodel(ev, this);
                case TEventType.CommandScript:
                    return new EventPanelCommandScriptViewmodel(ev, this);
                default:
                    throw new ApplicationException(string.Format("Invalid event type {0} to create panel", ev.EventType));
            }
        }

        protected virtual void OnRelocated(object sender, EventArgs e)
        {
            if (_parent != null)
                Application.Current.Dispatcher.BeginInvoke((Action)_updateLocation);
        }

        private void _eventDeleted(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate ()
            {
                Dispose();
            });
        }

        protected virtual void OnEventPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IEvent.EventName))
                NotifyPropertyChanged(e.PropertyName);
        }

        protected virtual void OnSubeventChanged(object o, CollectionOperationEventArgs<IEvent> e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate()
            {
                if (e.Operation == TCollectionOperation.Remove && HasDummyChild && _event.SubEventsCount == 0)
                    Childrens.Remove(DummyChild);
            });
        }


        public ObservableCollection<EventPanelViewmodelBase> Childrens
        {
            get { return _childrens; }
        }

        public bool HasDummyChild
        {
            get { return _childrens.Contains(DummyChild); }
        }

        protected void LoadChildrens()
        {
            UiServices.SetBusyState();
            foreach (IEvent se in _event.SubEvents)
            {
                _childrens.Add(CreateChildEventPanelViewmodelForEvent(se));
                IEvent ne = se.Next;
                while (ne != null)
                {
                    _childrens.Add(CreateChildEventPanelViewmodelForEvent(ne));
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
                    UiServices.SetBusyState();
                    foreach (var c in _childrens.ToList())
                        c.Dispose();
                    if (Event.SubEventsCount > 0)
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
                if (SetField(ref _isExpanded, value, nameof(IsExpanded)))
                {
                    // Lazy load the child items, if necessary.
                    if (value && this.HasDummyChild)
                    {
                        this.Childrens.Remove(DummyChild);
                        this.LoadChildrens();
                    }
                    if (!value)
                        ClearChildrens();
                }
            }
        }

        bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (SetField(ref _isSelected, value, nameof(IsSelected)))
                {
                    if (value)
                    {
                        _engineViewmodel.Selected = this;
                        BringIntoView();
                    }
                    InvalidateRequerySuggested();
                }
            }
        }

        bool _isMultiSelected;
        public bool IsMultiSelected
        {
            get { return _isMultiSelected; }
            set { SetField(ref _isMultiSelected, value, nameof(IsMultiSelected)); }
        }

        public virtual bool IsVisible { get { return true; } set { } }

        public EventPanelViewmodelBase Parent
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
            get { return _event == null ? string.Empty : _event.EventName; }
        }

        public object EventType
        {
            get { return _event == null ? null : (object)_event.EventType; }
        }

        public UInt64 IdRundownEvent { get { return (_event == null) ? 0 : _event.IdRundownEvent; } }

        public bool HasSubItems
        {
            get { return (_event == null || _event.EventType == TEventType.Live || _event.EventType == TEventType.Movie) ? false : _event.SubEvents.Any(e => e.EventType == TEventType.StillImage); }
        }

        public EventPanelViewmodelBase Find(IEvent aEvent)
        {
            if (aEvent == null)
                return null;
            foreach (EventPanelViewmodelBase m in _childrens)
            {
                if (m._event == aEvent)
                    return m;
                var ret = m.Find(aEvent);
                if (ret != null)
                    return ret;
            }
            return null;
        }

        private void _updateLocation()
        {
            if (_event != null)
            {
                IEvent prior = _event.Prior;
                IEvent parent = _event.Parent;
                IEvent next = _event.Next;
                IEvent visualParent = _event.VisualParent;
                if (prior != null)
                {
                    int index = _parent._childrens.IndexOf(this);
                    if (visualParent != _parent._event
                        || index <= 0
                        || _parent._childrens[index - 1]._event != prior)
                    {
                        EventPanelViewmodelBase priorVm = _root.Find(prior);
                        if (priorVm != null)
                        {
                            EventPanelViewmodelBase newParent = priorVm._parent;
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
                        EventPanelViewmodelBase nextVm = _root.Find(next);
                        if (nextVm != null)
                        {
                            EventPanelViewmodelBase newParent = nextVm._parent;
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
                    EventPanelViewmodelBase parentVm = _root.Find(parent);
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
                this.BringIntoView();
            }
        }
        

        public bool Contains(IEvent  aEvent)
        {
            foreach (EventPanelViewmodelBase m in _childrens)
            {
                if (m._event == aEvent)
                    return true;
                if (m.Contains(aEvent))
                    return true;
            }
            return false;
        }

  
        public IEvent Event { get { return _event; } }

        public override string ToString()
        {
            return _event == null ? "null" : _event.ToString();
        }

        public Views.EventPanelView View;

        protected EventPanelViewmodelBase _rootOwner
        {
            get
            {
                var result = this;
                while (result.Parent is EventPanelRundownElementViewmodelBase || result.Parent is EventPanelContainerViewmodel)
                    result = result.Parent;
                return result;
            }
        }

        public string RootOwnerName { get { return _rootOwner.EventName; } }

        internal virtual void SetOnTop() { }

        internal void BringIntoView()
        {
            var p = Parent;
            if (p != null)
                if (p.IsExpanded)
                {
                    View?.BringIntoView();
                }
                else
                    p.BringIntoView();
        }
    }
}
