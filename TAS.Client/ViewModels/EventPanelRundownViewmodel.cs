using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;
using TAS.Server.Common;
using TAS.Server.Interfaces;
using resources = TAS.Client.Common.Properties.Resources;

namespace TAS.Client.ViewModels
{
    public class EventPanelRundownViewmodel: EventPanelRundownElementViewmodelBase
    {
        public EventPanelRundownViewmodel(IEvent ev, EventPanelViewmodelBase parent) : base(ev, parent)
        {
            CommandAddSubMovie = new UICommand() { ExecuteDelegate = _addSubMovie, CanExecuteDelegate = (o) => _event.SubEvents.Count == 0 };
            CommandAddSubRundown = new UICommand() { ExecuteDelegate = _addSubRundown, CanExecuteDelegate = (o) => _event.SubEvents.Count == 0 };
        }
        protected override void OnSubeventChanged(object o, CollectionOperationEventArgs<IEvent> e)
        {
            base.OnSubeventChanged(o, e);
            InvalidateRequerySuggested();
        }

        private void _addSubRundown(object obj)
        {
            IEvent ev = _event;
            if (ev != null)
            {
                IEvent newEvent = ev.Engine.CreateEvent();
                newEvent.EventType = TEventType.Rundown;
                newEvent.EventName = resources._title_NewRundown;
                newEvent.StartType = TStartType.Manual;
                newEvent.ScheduledTime = DateTime.Now.ToUniversalTime();
                ev.InsertUnder(newEvent);
            }
        }

        private void _addSubMovie(object obj)
        {
            IEvent ev = _event;
            var svm =  _engineViewmodel.MediaSearchViewModel;
            if (ev != null && svm == null)
            {
                svm = new MediaSearchViewmodel(_engineViewmodel.Engine, _event.Engine.MediaManager, TMediaType.Movie, false, null);
                svm.BaseEvent = ev;
                svm.NewEventStartType = TStartType.With;
                svm.MediaChoosen += _searchMediaChoosen;
                svm.SearchWindowClosed += _searchWindowClosed;
                svm.ExecuteAction = new Action<MediaSearchEventArgs>((e) =>
                {
                    if (e.Media != null)
                    {
                        IEvent newEvent = ev.Engine.CreateEvent();
                        newEvent.EventType = TEventType.Movie;
                        newEvent.Media = e.Media;
                        newEvent.EventName = e.MediaName;
                        newEvent.ScheduledTc = e.TCIn;
                        newEvent.Duration = e.Duration;
                        newEvent.Layer = VideoLayer.Program;
                        newEvent.GPI = _setGPI(e.Media);

                        //newEvent.Save();
                        if (svm.NewEventStartType == TStartType.After)
                            svm.BaseEvent.InsertAfter(newEvent);
                        if (svm.NewEventStartType == TStartType.With)
                            svm.BaseEvent.InsertUnder(newEvent);
                        ev = newEvent;
                        svm.NewEventStartType = TStartType.After;
                    }
                });
                _engineViewmodel.MediaSearchViewModel = svm;
            }

        }

        public ICommand CommandAddSubRundown { get; private set; }
        public ICommand CommandAddSubMovie { get; private set; }
    }
}
