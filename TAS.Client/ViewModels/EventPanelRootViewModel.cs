//#undef DEBUG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EventPanelRootViewModel : EventPanelViewModelBase
    {
        public EventPanelRootViewModel(EngineViewModel engineViewModel) : base(engineViewModel)
        {
            Engine.EventLocated += _onEngineEventLocated;
            Engine.EventDeleted += _engine_EventDeleted;
            AddRootEvents();
        }
        
        public IEnumerable<EventPanelViewModelBase> HiddenContainers
        {
            get { return Childrens.Where(c => (c as EventPanelContainerViewModel)?.IsVisible == false); }
        }

        public bool IsAnyContainerHidden => HiddenContainers.Any();

        internal void NotifyContainerVisibility()
        {
            NotifyPropertyChanged(nameof(IsAnyContainerHidden));
            NotifyPropertyChanged(nameof(HiddenContainers));
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            Engine.EventLocated -= _onEngineEventLocated;
            Engine.EventDeleted -= _engine_EventDeleted;
        }

        private async void AddRootEvents()
        {
            try
            {
                var root = await Task.Run(() => Engine.GetRootEvents());
                foreach (var se in root)
                    AddRootEvent(se);
                NotifyContainerVisibility();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        private void _engine_EventDeleted(object sender, EventEventArgs e)
        {
            OnUiThread(() =>
            {
                var evm = Find(e.Event, true);
                evm?.Dispose();
            });
        }

        private void _onEngineEventLocated(object o, EventEventArgs e) // when new event was created
        {
            Debug.WriteLine(e.Event?.EventName, "EventLocated notified");
            if (e.Event == null)
                return;
            OnUiThread(() =>
            {
                var evm = Find(e.Event, true);
                if (evm != null)
                {
                    evm.UpdateLocation();
                }
                else
                {
                    EventPanelViewModelBase vm = PlaceEventInRundown(e.Event, false);
                    if (vm != null
                        && e.Event.EventType != TEventType.StillImage
                        && e.Event == EngineViewModel.LastAddedEvent)
                    {
                        vm.IsSelected = true;
                        EngineViewModel.ClearSelection();
                        if (e.Event.EventType == TEventType.Rundown)
                            vm.IsExpanded = true;
                    }
                    (vm as EventPanelRundownElementViewModelBase)?.VerifyIsInvalidInSchedule();
                }
            });
        }


        private EventPanelViewModelBase PlaceEventInRundown(IEvent e, bool show)
        {
            EventPanelViewModelBase newVm = null;
            EventPanelViewModelBase evm = Find(e, true);
            if (evm == null)
            {
                var vp = e.GetVisualParent();
                if (vp != null)
                {
                    var evmVp = Find(vp, true);
                    if (evmVp != null)
                    {
                        var eventType = e.EventType;
                        if (eventType == TEventType.Movie || eventType == TEventType.Rundown || eventType == TEventType.Live
                            || evmVp.IsExpanded)
                        {
                            if (evmVp.IsExpanded || show || e == EngineViewModel.LastAddedEvent)
                            {
                                evmVp.IsExpanded = true;
                                if (evmVp.Find(e, true) == null) // find again after expand
                                {
                                    if (e.GetParent() == vp) // StartType = With
                                    {
                                        newVm = evmVp.CreateChildEventPanelViewModelForEvent(e);
                                        evmVp.Childrens.Insert(0, newVm);
                                    }
                                    else // StartType == After
                                    {
                                        var prior = e.GetPrior();
                                        if (prior != null)
                                        {
                                            var evmPrior = evmVp.Find(prior, true);
                                            if (evmPrior == null)
                                                evmPrior = PlaceEventInRundown(prior, true); // recurrence here
                                            if (evmPrior != null)
                                            {
                                                var pos = evmVp.Childrens.IndexOf(evmPrior);
                                                newVm = evmVp.CreateChildEventPanelViewModelForEvent(e);
                                                evmVp.Childrens.Insert(pos + 1, newVm);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (!evmVp.HasDummyChild)
                                evmVp.Childrens.Add(DummyChild);
                        }
                    }
                }
                else //vp == null
                {
                    var prior = e.GetPrior();
                    if (prior != null)
                    {
                        var evmPrior = Find(prior, true);
                        if (evmPrior != null)
                        {
                            var pos = Childrens.IndexOf(evmPrior);
                            newVm = CreateChildEventPanelViewModelForEvent(e);
                            Childrens.Insert(pos + 1, newVm);
                        }
                    }
                    else
                        if (e.StartType == TStartType.Manual || e.EventType == TEventType.Container)
                        newVm = AddRootEvent(e);
                }
            }
            return newVm;
        }
        

        private EventPanelViewModelBase AddRootEvent(IEvent e)
        {
            if (!e.IsDeleted)
            {
                var newEvm = CreateChildEventPanelViewModelForEvent(e);
                Childrens.Add(newEvm);
                IEvent ne = e.GetNext();
                while (ne != null)
                {
                    Childrens.Add(CreateChildEventPanelViewModelForEvent(ne));
                    Debug.WriteLine(ne, "Reading next for");
                    ne = ne.GetNext();
                }
                return newEvm;
            }
            return null;
        }
        
        
    }
}
