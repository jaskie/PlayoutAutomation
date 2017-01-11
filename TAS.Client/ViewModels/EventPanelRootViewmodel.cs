using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using TAS.Common;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EventPanelRootViewmodel : EventPanelViewmodelBase
    {
        public EventPanelRootViewmodel(EngineViewmodel engineViewmodel): base(engineViewmodel)
        {
            _engine.EventSaved += _onEngineEventSaved;
            foreach (var se in _engine.GetRootEvents())
                _addRootEvent(se);
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            _engine.EventSaved -= _onEngineEventSaved;
        }

        private void _onEngineEventSaved(object o, IEventEventArgs e) // when new event was created
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate ()
            {
                EventPanelViewmodelBase newVm = _placeEventInRundown(e.Event);
                if (newVm != null
                    && e.Event.EventType != TEventType.StillImage
                    && e.Event == _engineViewmodel.LastAddedEvent)
                {
                    newVm.IsSelected = true;
                    _engineViewmodel.ClearSelection();
                    if (e.Event.EventType == TEventType.Rundown)
                        newVm.IsExpanded = true;
                }
            });
        }

        private EventPanelViewmodelBase _placeEventInRundown(IEvent e)
        {
            EventPanelViewmodelBase newVm = null;
            EventPanelViewmodelBase evm = this.Find(e);
            if (evm == null)
            {
                var vp = e.GetVisualParent();
                if (vp != null)
                {
                    var evm_vp = this.Find(vp);
                    if (evm_vp != null)
                    {
                        var eventType = e.EventType;
                        if (eventType == TEventType.Movie || eventType == TEventType.Rundown || eventType == TEventType.Live
                            || evm_vp.IsExpanded)
                        {
                            if (e == _engineViewmodel.LastAddedEvent || _engineViewmodel.TrackPlayingEvent)
                            {
                                evm_vp.IsExpanded = true;
                                if (evm_vp.Find(e) == null) // find again after expand
                                {
                                    if (e.Parent == vp) // StartType = With
                                    {
                                        newVm = evm_vp.CreateChildEventPanelViewmodelForEvent(e);
                                        evm_vp.Childrens.Insert(0, newVm);
                                    }
                                    else // StartType == After
                                    {
                                        var prior = e.Prior;
                                        if (prior != null)
                                        {
                                            var evm_prior = evm_vp.Find(prior);
                                            if (evm_prior == null)
                                                evm_prior = _placeEventInRundown(prior); // recursion here
                                            if (evm_prior != null)
                                            {
                                                var pos = evm_vp.Childrens.IndexOf(evm_prior);
                                                newVm = evm_vp.CreateChildEventPanelViewmodelForEvent(e);
                                                evm_vp.Childrens.Insert(pos + 1, newVm);
                                            }
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
                    var prior = e.Prior;
                    if (prior != null)
                    {
                        var evm_prior = this.Find(prior);
                        if (evm_prior != null)
                        {
                            var pos = this._childrens.IndexOf(evm_prior);
                            newVm = this.CreateChildEventPanelViewmodelForEvent(e);
                            this._childrens.Insert(pos + 1, newVm);
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
                EngineViewmodel evm = _engineViewmodel;
                var newEvm = this.CreateChildEventPanelViewmodelForEvent(e);
                _childrens.Add(newEvm);
                IEvent ne = e.Next;
                while (ne != null)
                {
                    _childrens.Add(this.CreateChildEventPanelViewmodelForEvent(ne));
                    Debug.WriteLine(ne, "Reading next for");
                    ne = ne.Next;
                }
                if (e.EventType == TEventType.Container)
                    NotifyPropertyChanged("Containers");
                return newEvm;
            }
            return null;
        }

        public IEnumerable<EventPanelContainerViewmodel> Containers
        {
            get { return _childrens.Where(c => c is EventPanelContainerViewmodel).Select(c=> c as EventPanelContainerViewmodel); }
        }

    }
}
