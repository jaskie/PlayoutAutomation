//#undef DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EventPanelRootViewmodel : EventPanelViewmodelBase
    {
        public EventPanelRootViewmodel(EngineViewmodel engineViewmodel) : base(engineViewmodel)
        {
            Engine.EventLocated += _onEngineEventLocated;
            Engine.EventDeleted += _engine_EventDeleted;
            foreach (var se in Engine.GetRootEvents())
                _addRootEvent(se);
        }

        public IEnumerable<EventPanelViewmodelBase> HiddenContainers
        {
            get { return _childrens.Where(c => (c as EventPanelContainerViewmodel)?.IsVisible == false); }
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

        private void _engine_EventDeleted(object sender, EventEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                EventPanelViewmodelBase evm = Find(e.Event);
                evm?.Dispose();
            });
        }

        private void _onEngineEventLocated(object o, EventEventArgs e) // when new event was created
        {
            Debug.WriteLine(e.Event?.EventName, "EventLocated notified");
            if (e.Event == null)
                return;
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                EventPanelViewmodelBase vm = _placeEventInRundown(e.Event, false);
                if (vm != null
                    && e.Event.EventType != TEventType.StillImage
                    && e.Event == EngineViewmodel.LastAddedEvent)
                {
                    vm.IsSelected = true;
                    EngineViewmodel.ClearSelection();
                    if (e.Event.EventType == TEventType.Rundown)
                        vm.IsExpanded = true;
                }
                (vm as EventPanelRundownElementViewmodelBase)?.VerifyIsInvalidInSchedule();
            });
        }

        private EventPanelViewmodelBase _placeEventInRundown(IEvent e, bool show)
        {
            EventPanelViewmodelBase newVm = null;
            EventPanelViewmodelBase evm = Find(e);
            if (evm == null)
            {
                var vp = e.GetVisualParent();
                if (vp != null)
                {
                    var evmVp = Find(vp);
                    if (evmVp != null)
                    {
                        var eventType = e.EventType;
                        if (eventType == TEventType.Movie || eventType == TEventType.Rundown || eventType == TEventType.Live
                            || evmVp.IsExpanded)
                        {
                            if (evmVp.IsExpanded || show || e == EngineViewmodel.LastAddedEvent)
                            {
                                evmVp.IsExpanded = true;
                                if (evmVp.Find(e) == null) // find again after expand
                                {
                                    if (e.Parent == vp) // StartType = With
                                    {
                                        newVm = evmVp.CreateChildEventPanelViewmodelForEvent(e);
                                        evmVp.Childrens.Insert(0, newVm);
                                    }
                                    else // StartType == After
                                    {
                                        var prior = e.Prior;
                                        if (prior != null)
                                        {
                                            var evmPrior = evmVp.Find(prior);
                                            if (evmPrior == null)
                                                evmPrior = _placeEventInRundown(prior, true); // recurrence here
                                            if (evmPrior != null)
                                            {
                                                var pos = evmVp.Childrens.IndexOf(evmPrior);
                                                newVm = evmVp.CreateChildEventPanelViewmodelForEvent(e);
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
                    var prior = e.Prior;
                    if (prior != null)
                    {
                        var evmPrior = Find(prior);
                        if (evmPrior != null)
                        {
                            var pos = _childrens.IndexOf(evmPrior);
                            newVm = CreateChildEventPanelViewmodelForEvent(e);
                            _childrens.Insert(pos + 1, newVm);
                        }
                    }
                    else
                        if (e.StartType == TStartType.Manual || e.EventType == TEventType.Container)
                        newVm = _addRootEvent(e);
                }
            }
            return newVm;
        }

        private EventPanelViewmodelBase _addRootEvent(IEvent e)
        {
            if (!e.IsDeleted)
            {
                var newEvm = CreateChildEventPanelViewmodelForEvent(e);
                _childrens.Add(newEvm);
                IEvent ne = e.Next;
                while (ne != null)
                {
                    _childrens.Add(CreateChildEventPanelViewmodelForEvent(ne));
                    Debug.WriteLine(ne, "Reading next for");
                    ne = ne.Next;
                }
                return newEvm;
            }
            return null;
        }
        
        
    }
}
