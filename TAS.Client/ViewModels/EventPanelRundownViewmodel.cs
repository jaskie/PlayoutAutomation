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
            CommandAddSubMovie = new UICommand() { ExecuteDelegate = _addSubMovie, CanExecuteDelegate = (o) => _event.SubEvents.Count == 0};
            CommandAddSubRundown = new UICommand() { ExecuteDelegate = _addSubRundown, CanExecuteDelegate = (o) => _event.SubEvents.Count == 0 };
        }
        protected override void OnSubeventChanged(object o, CollectionOperationEventArgs<IEvent> e)
        {
            base.OnSubeventChanged(o, e);
            InvalidateRequerySuggested();
        }

        protected override bool canAddNextMovie(object o)
        {
            return _parent is EventPanelRundownViewmodel && base.canAddNextMovie(o);
        }
        protected override bool canAddNewLive(object o)
        {
            return _parent is EventPanelRundownViewmodel && base.canAddNewLive(o);
        }

        private void _addSubRundown(object obj)
        {
            IEvent newEvent = _event.Engine.CreateEvent();
            newEvent.EventType = TEventType.Rundown;
            newEvent.EventName = resources._title_NewRundown;
            newEvent.StartType = TStartType.Manual;
            newEvent.ScheduledTime = DateTime.Now.ToUniversalTime();
            _event.InsertUnder(newEvent);
        }

        private void _addSubMovie(object obj)
        {
            _engineViewmodel.AddMediaEvent(_event, TStartType.With, TMediaType.Movie, VideoLayer.Program, false);
        }

        public ICommand CommandAddSubRundown { get; private set; }
        public ICommand CommandAddSubMovie { get; private set; }
    }
}
