using System;
using System.ComponentModel;
using System.Linq;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;
using resources = TAS.Client.Common.Properties.Resources;


namespace TAS.Client.ViewModels
{
    public abstract class EventPanelViewmodelBase : ViewmodelBase
    {
        protected readonly IEngine Engine;
        protected readonly EventPanelRootViewmodel Root;
        protected readonly EngineViewmodel EngineViewmodel;
        protected readonly ObservableCollection<EventPanelViewmodelBase> _childrens = new ObservableCollection<EventPanelViewmodelBase>();
        protected static readonly EventPanelViewmodelBase DummyChild = new EventPanelDummyViewmodel();
        private EventPanelViewmodelBase _parent;

        private TVideoFormat _videoFormat;
        bool _isExpanded;
        bool _isSelected;
        bool _isMultiSelected;

        /// <summary>
        /// Constructor for root event
        /// </summary>
        /// <param name="engineViewmodel"></param>
        protected EventPanelViewmodelBase(EngineViewmodel engineViewmodel)
        {
            EngineViewmodel = engineViewmodel;
            Engine = engineViewmodel.Engine;
            Level = 0;
            _isExpanded = true;
            _videoFormat = engineViewmodel.VideoFormat;
            Root = (EventPanelRootViewmodel)this;
        }

        /// <summary>
        /// Constructor for child events
        /// </summary>
        /// <param name="aEvent"></param>
        /// <param name="parent"></param>
        protected EventPanelViewmodelBase(IEvent aEvent, EventPanelViewmodelBase parent)
        {
            if (aEvent == null) // dummy child
                return;
            Engine = aEvent.Engine;
            _videoFormat = Engine.VideoFormat;
            Event = aEvent;
            if (parent != null)
            {
                _parent = parent;
                Root = parent.Root;
                EngineViewmodel = parent.EngineViewmodel;
                Level = (_parent == null) ? 0 : _parent.Level + 1;
                if (aEvent.SubEventsCount > 0)
                    _childrens.Add(DummyChild);
            }
            Event.PropertyChanged += OnEventPropertyChanged;
            Event.SubEventChanged += OnSubeventChanged;
            Event.Relocated += OnRelocated;
        }

        protected override void OnDispose()
        {
            if (_parent != null)
            {
                _parent._childrens.Remove(this);
                _parent = null;
            }
            ClearChildrens();
            if (Event != null)
            {
                Event.PropertyChanged -= OnEventPropertyChanged;
                Event.SubEventChanged -= OnSubeventChanged;
                Event.Relocated -= OnRelocated;
                EngineViewmodel?.RemoveMultiSelected(this);
                IsMultiSelected = false;
            }
            Debug.WriteLine(this, "EventPanelViewmodel Disposed");
        }

        public ObservableCollection<EventPanelViewmodelBase> Childrens => _childrens;

        public bool HasDummyChild => _childrens.Contains(DummyChild);

        public int Level { get; }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (SetField(ref _isExpanded, value))
                {
                    // Lazy load the child items, if necessary.
                    if (value && HasDummyChild)
                    {
                        Childrens.Remove(DummyChild);
                        LoadChildrens();
                    }
                    if (!value)
                        ClearChildrens();
                }
            }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (SetField(ref _isSelected, value))
                {
                    if (value)
                    {
                        EngineViewmodel.SelectedEvent = this;
                        BringIntoView();
                    }
                    InvalidateRequerySuggested();
                }
            }
        }

        public bool IsMultiSelected
        {
            get { return _isMultiSelected; }
            set { SetField(ref _isMultiSelected, value); }
        }

        public virtual bool IsVisible { get { return true; } protected set { } }

        public virtual TVideoFormat VideoFormat
        {
            get { return _videoFormat; }
            protected set { SetField(ref _videoFormat, value); }
        }

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
                    if (Event.Prior != null)
                        _parent.Childrens.Insert(_parent._childrens.IndexOf(_parent._childrens.FirstOrDefault(evm => evm.Event == Event.Prior))+1, this);
                    else
                        _parent._childrens.Insert(0, this);
                }
            }
        }
        
        public string EventName => Event?.EventName;

        public TEventType? EventType => Event?.EventType;

        public EventPanelViewmodelBase Find(IEvent aEvent)
        {
            if (aEvent == null)
                return null;
            foreach (EventPanelViewmodelBase m in _childrens)
            {
                if (m.Event == aEvent)
                    return m;
                var ret = m.Find(aEvent);
                if (ret != null)
                    return ret;
            }
            return null;
        }
        
        public bool Contains(IEvent  aEvent)
        {
            foreach (EventPanelViewmodelBase m in _childrens)
            {
                if (m.Event == aEvent)
                    return true;
                if (m.Contains(aEvent))
                    return true;
            }
            return false;
        }

        public IEvent Event { get; }
        
        public string RootOwnerName => RootOwner.EventName;

        public override string ToString()
        {
            return $"{Infralution.Localization.Wpf.ResourceEnumConverter.ConvertToString(EventType)} - {EventName}";
        }

        public Views.EventPanelView View;

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
                    throw new ApplicationException($"Invalid event type {ev.EventType} to create panel");
            }
        }

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

        internal bool Focus()
        {
            DependencyObject current = View;
            while (current != null)
            {
                if (current is System.Windows.Controls.TreeViewItem)
                    return (current as UIElement)?.Focus() == true;
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return false;
        }


        protected virtual void OnRelocated(object sender, EventArgs e)
        {
            if (_parent != null)
                Application.Current.Dispatcher.BeginInvoke((Action)_updateLocation);
        }

        protected virtual void OnEventPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IEvent.EventName))
                NotifyPropertyChanged(e.PropertyName);
        }

        protected virtual void OnSubeventChanged(object o, CollectionOperationEventArgs<IEvent> e)
        {
            Debug.WriteLine(e.Item, $"OnSubEventChanged {e.Operation}");
            Application.Current.Dispatcher.BeginInvoke((Action)delegate 
            {
                if (e.Operation == CollectionOperation.Remove && !IsExpanded && HasDummyChild && Event.SubEventsCount == 0)
                    Childrens.Remove(DummyChild);
                if (e.Operation == CollectionOperation.Add && !IsExpanded && !HasDummyChild && Event.SubEventsCount > 0)
                    Childrens.Add(DummyChild);
            });
        }

        protected EventPanelViewmodelBase RootOwner
        {
            get
            {
                var result = this;
                while (result.Parent is EventPanelRundownElementViewmodelBase || result.Parent is EventPanelContainerViewmodel)
                    result = result.Parent;
                return result;
            }
        }

        protected void LoadChildrens()
        {
            UiServices.SetBusyState();
            foreach (IEvent se in Event.SubEvents)
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
            if (!_childrens.Any())
                return;
            if (!HasDummyChild)
            {
                UiServices.SetBusyState();
                foreach (var c in _childrens.ToList())
                    c.Dispose();
                if (Event.SubEventsCount > 0)
                    _childrens.Add(DummyChild);
            }
        }

        private void _updateLocation()
        {
            if (Event != null)
            {
                IEvent prior = Event.Prior;
                IEvent parent = Event.Parent;
                IEvent next = Event.Next;
                IEvent visualParent = Event.GetVisualParent();
                if (prior != null)
                {
                    int index = _parent._childrens.IndexOf(this);
                    if (visualParent != _parent.Event
                        || index <= 0
                        || _parent._childrens[index - 1].Event != prior)
                    {
                        EventPanelViewmodelBase priorVm = Root.Find(prior);
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
                    if (visualParent != _parent.Event
                        || index <= 0
                        || _parent._childrens[index].Event != next)
                    {
                        EventPanelViewmodelBase nextVm = Root.Find(next);
                        if (nextVm != null)
                        {
                            EventPanelViewmodelBase newParent = nextVm._parent;
                            if (_parent == newParent)
                            {
                                int nextIndex = newParent._childrens.IndexOf(nextVm);
                                if (index >= nextIndex)
                                    newParent._childrens.Move(index, nextIndex);
                                else
                                    newParent._childrens.Move(index, nextIndex - 1);
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
                    Root._childrens.Add(this);
                    _parent = Root;
                }
                else
                {
                    EventPanelViewmodelBase parentVm = Root.Find(parent);
                    if (parentVm != null)
                    {
                        if (parentVm == _parent)
                            parentVm.Childrens.Move(parentVm.Childrens.IndexOf(this), 0);
                        else
                            Parent = parentVm;
                    }
                }
                BringIntoView();
            }
        }

    }
}
