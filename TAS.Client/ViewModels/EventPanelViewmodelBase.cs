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
    [DebuggerDisplay("{" + nameof(EventName) + "}")]
    public abstract class EventPanelViewmodelBase : ViewmodelBase
    {
        protected readonly IEngine Engine;
        protected readonly EngineViewmodel EngineViewmodel;
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
                    Childrens.Add(DummyChild);
            }
            Event.PropertyChanged += OnEventPropertyChanged;
        }

        protected override void OnDispose()
        {
            if (_parent != null)
            {
                _parent.Childrens.Remove(this);
                _parent = null;
            }
            ClearChildrens();
            if (Event != null)
            {
                Event.PropertyChanged -= OnEventPropertyChanged;
                EngineViewmodel?.RemoveMultiSelected(this);
                IsMultiSelected = false;
            }
            Debug.WriteLine(this, "EventPanelViewmodel Disposed");
        }

        protected EventPanelRootViewmodel Root { get; }

        public ObservableCollection<EventPanelViewmodelBase> Childrens { get; } = new ObservableCollection<EventPanelViewmodelBase>();

        public bool HasDummyChild => Childrens.Contains(DummyChild);

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
                    _parent.Childrens.Remove(this);
                    if (_parent.Childrens.Count == 0)
                        _parent.IsExpanded = false;
                    _parent = value;
                    if (_parent != null)
                        if (_parent.IsExpanded)
                            if (Event.Prior != null)
                                _parent.Childrens.Insert(
                                    _parent.Childrens.IndexOf(
                                        _parent.Childrens.FirstOrDefault(evm => evm.Event == Event.Prior)) + 1, this);
                            else
                                _parent.Childrens.Insert(0, this);
                }
            }
        }
        
        public string EventName => Event?.EventName;

        public TEventType? EventType => Event?.EventType;

        public EventPanelViewmodelBase Find(IEvent aEvent)
        {
            if (aEvent == null)
                return null;
            foreach (EventPanelViewmodelBase m in Childrens)
            {
                if (m.Event == aEvent)
                    return m;
                var ret = m.Find(aEvent);
                if (ret != null)
                    return ret;
            }
            return null;
        }

        protected internal virtual void UpdateLocation()
        {
            var ev = Event;
            if (ev == null)
                return;
            IEvent prior = ev.Prior;
            IEvent parent = ev.Parent;
            IEvent next = ev.Next;
            IEvent visualParent = ev.GetVisualParent();
            if (prior != null)
            {
                int index = Parent.Childrens.IndexOf(this);
                if (visualParent != Parent.Event
                    || index <= 0
                    || Parent.Childrens[index - 1].Event != prior)
                {
                    EventPanelViewmodelBase priorVm = Root.Find(prior);
                    if (priorVm != null)
                    {
                        EventPanelViewmodelBase newParent = priorVm.Parent;
                        if (Parent == newParent)
                        {
                            int priorIndex = newParent.Childrens.IndexOf(priorVm);
                            if (index >= priorIndex)
                                newParent.Childrens.Move(index, priorIndex + 1);
                            else
                                newParent.Childrens.Move(index, priorIndex);
                        }
                        else
                        {
                            Parent.Childrens.Remove(this);
                            if (!newParent.HasDummyChild)
                                newParent.Childrens.Insert(newParent.Childrens.IndexOf(priorVm) + 1, this);
                            Parent = newParent;
                        }
                    }
                }
            }
            else if (parent == null && next != null)
            {
                int index = Parent.Childrens.IndexOf(this);
                if (visualParent != Parent.Event
                    || index <= 0
                    || Parent.Childrens[index].Event != next)
                {
                    EventPanelViewmodelBase nextVm = Root.Find(next);
                    if (nextVm != null)
                    {
                        EventPanelViewmodelBase newParent = nextVm.Parent;
                        if (Parent == newParent)
                        {
                            int nextIndex = newParent.Childrens.IndexOf(nextVm);
                            if (index >= nextIndex)
                                newParent.Childrens.Move(index, nextIndex);
                            else
                                newParent.Childrens.Move(index, nextIndex - 1);
                        }
                        else
                        {
                            Parent.Childrens.Remove(this);
                            if (!newParent.HasDummyChild)
                                newParent.Childrens.Insert(newParent.Childrens.IndexOf(nextVm), this);
                            Parent = newParent;
                        }
                    }
                }
            }
            else if (parent == null)
            {
                Parent.Childrens.Remove(this);
                Root.Childrens.Add(this);
                Parent = Root;
            }
            else
            {
                EventPanelViewmodelBase parentVm = Root.Find(parent);
                if (parentVm != null)
                {
                    if (parentVm == Parent)
                        parentVm.Childrens.Move(parentVm.Childrens.IndexOf(this), 0);
                    else
                        Parent = parentVm;
                }
            }
            BringIntoView();
        }

        public bool Contains(IEvent  aEvent)
        {
            foreach (EventPanelViewmodelBase m in Childrens)
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


        protected virtual void OnEventPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IEvent.EventName))
                NotifyPropertyChanged(e.PropertyName);
            if (e.PropertyName == nameof(IEvent.SubEventsCount))
            {
                Application.Current.Dispatcher.BeginInvoke((Action) delegate
                {
                    if (IsExpanded)
                        return;
                    if (((IEvent) sender).SubEventsCount == 0)
                        Childrens.Remove(DummyChild);
                    else if (!HasDummyChild)
                        Childrens.Add(DummyChild);
                });
            }
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
                Childrens.Add(CreateChildEventPanelViewmodelForEvent(se));
                IEvent ne = se.Next;
                while (ne != null)
                {
                    Childrens.Add(CreateChildEventPanelViewmodelForEvent(ne));
                    ne = ne.Next;
                }
            }
        }

        protected void ClearChildrens()
        {
            if (!Childrens.Any())
                return;
            if (!HasDummyChild)
            {
                UiServices.SetBusyState();
                foreach (var c in Childrens.ToList())
                    c.Dispose();
                if (Event.SubEventsCount > 0)
                    Childrens.Add(DummyChild);
            }
        }


    }
}
