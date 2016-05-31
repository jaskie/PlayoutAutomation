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
            _engine.EventSaved += _onEventSaved;
            foreach (IEvent se in _engine.RootEvents.ToList())
                _addRootEvent(se);
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            _engine.EventSaved -= _onEventSaved;
        }

        private void _onEventSaved(object o, IEventEventArgs e) // when new event was created
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate ()
            {
                EventPanelViewmodelBase evm = this.Find(e.Event);
                EventPanelViewmodelBase newVm = null;
                if (evm == null)
                {
                    var vp = e.Event.VisualParent;
                    if (vp != null)
                    {
                        var evm_vp = this.Find(vp);
                        if (evm_vp != null)
                        {
                            if (e.Event.EventType == TEventType.Movie || e.Event.EventType == TEventType.Rundown || e.Event.EventType == TEventType.Live
                                || evm_vp.IsExpanded)
                            {
                                evm_vp.IsExpanded = true;
                                if (evm_vp.Find(e.Event) == null) // find again after expand
                                {
                                    if (e.Event.Parent == vp) // StartType = With
                                    {
                                        newVm = evm_vp.CreateChildEventPanelViewmodelForEvent(e.Event);
                                        evm_vp.Childrens.Insert(0, newVm);
                                    }
                                    else // StartType == After
                                    {
                                        var prior = e.Event.Prior;
                                        if (prior != null)
                                        {
                                            var evm_prior = evm_vp.Find(prior);
                                            if (evm_prior != null)
                                            {
                                                var pos = evm_vp.Childrens.IndexOf(evm_prior);
                                                newVm = evm_vp.CreateChildEventPanelViewmodelForEvent(e.Event);
                                                evm_vp.Childrens.Insert(pos + 1, newVm);
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
                        var prior = e.Event.Prior;
                        if (prior != null)
                        {
                            var evm_prior = this.Find(prior);
                            if (evm_prior != null)
                            {
                                var pos = this._childrens.IndexOf(evm_prior);
                                newVm = this.CreateChildEventPanelViewmodelForEvent(e.Event);
                                this._childrens.Insert(pos + 1, newVm);
                            }
                        }
                        else
                            if (e.Event.Parent == null)
                            _addRootEvent(e.Event);
                    }
                }
                if (newVm != null
                    && !(e.Event.EventType == TEventType.StillImage)
                    && e.Event == _engineViewmodel.LastAddedEvent)
                {
                    newVm.IsSelected = true;
                    newVm.IsMultiSelected = true;
                }
            });
        }

        private void _addRootEvent(IEvent e)
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
            }
        }

        public IEnumerable<EventPanelContainerViewmodel> Containers
        {
            get { return _childrens.Where(c => c is EventPanelContainerViewmodel).Select(c=> c as EventPanelContainerViewmodel); }
        }

    }
}
