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
    public abstract class EventPanelViewModelBase : ViewModelBase
    {
        protected readonly IEngine Engine;
        protected readonly EngineViewModel EngineViewModel;
        protected static readonly EventPanelViewModelBase DummyChild = new EventPanelDummyViewModel();
        private EventPanelViewModelBase _parent;

        private TVideoFormat _videoFormat;
        bool _isExpanded;
        bool _isSelected;
        bool _isMultiSelected;

        /// <summary>
        /// Constructor for root event
        /// </summary>
        /// <param name="engineViewModel"></param>
        protected EventPanelViewModelBase(EngineViewModel engineViewModel)
        {
            EngineViewModel = engineViewModel;
            Engine = engineViewModel.Engine;
            Level = 0;
            _isExpanded = true;
            _videoFormat = engineViewModel.VideoFormat;
            Root = (EventPanelRootViewModel)this;
        }

        /// <summary>
        /// Constructor for child events
        /// </summary>
        /// <param name="aEvent"></param>
        /// <param name="parent"></param>
        protected EventPanelViewModelBase(IEvent aEvent, EventPanelViewModelBase parent)
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
                EngineViewModel = parent.EngineViewModel;
                Level = parent.Level + 1;
                if (aEvent.SubEventsCount > 0)
                    Childrens.Add(DummyChild);
            }
            Event.PropertyChanged += OnEventPropertyChanged;
        }

        protected override void OnDispose()
        {
            if (_parent != null)
            {
                _parent.RemoveChild(this);
                _parent = null;
            }
            ClearChildrens();
            if (Event != null)
            {
                Event.PropertyChanged -= OnEventPropertyChanged;
                EngineViewModel.RemoveEventPanel(this);
                IsMultiSelected = false;
            }
            Debug.WriteLine(this, "EventPanelViewModel Disposed");
        }

        protected EventPanelRootViewModel Root { get; }

        public ObservableCollection<EventPanelViewModelBase> Childrens { get; } = new ObservableCollection<EventPanelViewModelBase>();

        public bool HasDummyChild => Childrens.Contains(DummyChild);

        public int Level { get; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (!SetField(ref _isExpanded, value))
                    return;
                if (value && HasDummyChild)
                {
                    Childrens.Remove(DummyChild);
                    LoadChildrens();
                }
                if (!value)
                    ClearChildrens();
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (!SetField(ref _isSelected, value))
                    return;
                if (value)
                {
                    EngineViewModel.SelectedEventPanel = this;
                    BringIntoView();
                }
                InvalidateRequerySuggested();
            }
        }

        public bool IsMultiSelected
        {
            get => _isMultiSelected;
            set => SetField(ref _isMultiSelected, value);
        }

        public virtual bool IsVisible { get { return true; } protected set { } }

        public virtual TVideoFormat VideoFormat
        {
            get => _videoFormat;
            protected set => SetField(ref _videoFormat, value);
        }

        public EventPanelViewModelBase Parent
        {
            get => _parent;
            private set => SetField(ref _parent, value);
        }
        
        public string EventName => Event?.EventName;

        public TEventType? EventType => Event?.EventType;

        public EventPanelViewModelBase Find(IEvent aEvent, bool searchOnNextLevels)
        {
            if (aEvent == null)
                return null;
            foreach (var m in Childrens)
            {
                if (m.Event == aEvent)
                    return m;
                if (searchOnNextLevels)
                {
                    var ret = m.Find(aEvent, true);
                    if (ret != null)
                        return ret;
                }
            }
            return null;
        }

        private void RemoveChild(EventPanelViewModelBase item)
        {
            if (!IsExpanded)
                return;
            Childrens.Remove(item);
            if (!(this is EventPanelRootViewModel) && Childrens.Count == 0)
                IsExpanded = false;
        }

        protected internal virtual void UpdateLocation()
        {
            var ev = Event;
            if (ev == null)
                return;
            var prior = ev.GetPrior();
            var parent = ev.GetParent();
            var next = ev.GetNext();
            var visualParent = ev.GetVisualParent();
            if (prior != null)
            {
                var index = Parent.Childrens.IndexOf(this);
                if (visualParent != Parent.Event
                    || index <= 0
                    || Parent.Childrens[index - 1].Event != prior)
                {
                    var priorVm = Root.Find(prior, true);
                    if (priorVm != null)
                    {
                        var newParent = priorVm.Parent;
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
                            Parent.RemoveChild(this);
                            if (!newParent.HasDummyChild)
                                newParent.Childrens.Insert(newParent.Childrens.IndexOf(priorVm) + 1, this);
                            Parent = newParent;
                        }
                    }
                    else
                    {
                        Parent.RemoveChild(this);
                    }
                }
            }
            else if (parent == null && next != null)
            {
                var index = Parent.Childrens.IndexOf(this);
                if (visualParent != Parent.Event
                    || index <= 0
                    || Parent.Childrens[index].Event != next)
                {
                    var nextVm = Root.Find(next, true);
                    if (nextVm != null)
                    {
                        var newParent = nextVm.Parent;
                        if (Parent == newParent)
                        {
                            var nextIndex = newParent.Childrens.IndexOf(nextVm);
                            if (index >= nextIndex)
                                newParent.Childrens.Move(index, nextIndex);
                            else
                                newParent.Childrens.Move(index, nextIndex - 1);
                        }
                        else
                        {
                            Parent.RemoveChild(this);
                            if (!newParent.HasDummyChild)
                                newParent.Childrens.Insert(newParent.Childrens.IndexOf(nextVm), this);
                            Parent = newParent;
                        }
                    }
                }
            }
            else if (parent == null)
            {
                Parent.RemoveChild(this);
                Root.Childrens.Add(this);
                Parent = Root;
            }
            else
            {
                var newParent = Root.Find(parent, true);
                if (newParent != null)
                {
                    if (newParent == Parent)
                        newParent.Childrens.Move(newParent.Childrens.IndexOf(this), 0);
                    else
                    {
                        Parent.RemoveChild(this);
                        if (newParent.IsExpanded)
                            newParent.Childrens.Insert(0, this);
                        Parent = newParent;
                    }
                }
            }
            BringIntoView();
        }

        public IEvent Event { get; }
        
        public string RootOwnerName => RootOwner.EventName;

        public override string ToString()
        {
            return $"{Infralution.Localization.Wpf.ResourceEnumConverter.ConvertToString(EventType)} - {EventName}";
        }

        public Views.EventPanelView View;

        internal EventPanelViewModelBase CreateChildEventPanelViewModelForEvent(IEvent ev)
        {
            switch (ev.EventType)
            {
                case TEventType.Rundown:
                    return new EventPanelRundownViewModel(ev, this);
                case TEventType.Container:
                    return new EventPanelContainerViewModel(ev, this);
                case TEventType.Movie:
                    return new EventPanelMovieViewModel(ev, this);
                case TEventType.Live:
                    return new EventPanelLiveViewModel(ev, this);
                case TEventType.StillImage:
                    return new EventPanelSecondaryEventViewModel(ev, this);
                case TEventType.Animation:
                    return new EventPanelAnimationViewModel(ev, this);
                case TEventType.CommandScript:
                    return new EventPanelCommandScriptViewModel(ev, this);
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
                if (current is System.Windows.Controls.TreeViewItem item)
                    return item.Focus();
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
                OnUiThread(() => 
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

        protected EventPanelViewModelBase RootOwner
        {
            get
            {
                var result = this;
                while (result.Parent is EventPanelRundownElementViewModelBase || result.Parent is EventPanelContainerViewModel)
                    result = result.Parent;
                return result;
            }
        }

        protected void LoadChildrens()
        {
            UiServices.Current.SetBusyState();
            foreach (var se in Event.GetSubEvents())
            {
                Childrens.Add(CreateChildEventPanelViewModelForEvent(se));
                var ne = se.GetNext();
                while (ne != null)
                {
                    Childrens.Add(CreateChildEventPanelViewModelForEvent(ne));
                    ne = ne.GetNext();
                }
            }
        }

        protected void ClearChildrens()
        {
            if (!Childrens.Any())
                return;
            if (HasDummyChild)
                return;
            UiServices.Current.SetBusyState();
            foreach (var c in Childrens.ToList())
                c.Dispose();
            Childrens.Clear();
            if (Event.SubEventsCount > 0)
                Childrens.Add(DummyChild);
        }


    }
}
