using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EventPanelRundownViewmodel: EventPanelRundownElementViewmodelBase
    {
        public EventPanelRundownViewmodel(IEvent ev, EventPanelViewmodelBase parent) : base(ev, parent)
        {
            CommandAddSubMovie = new UiCommand(_addSubMovie, _canAddSubEvent);
            CommandAddSubRundown = new UiCommand(_addSubRundown, _canAddSubEvent);
            CommandAddSubLive = new UiCommand(_addSubLive, _canAddSubEvent);
        }

        public ICommand CommandAddSubRundown { get; }
        public ICommand CommandAddSubMovie { get; }
        public ICommand CommandAddSubLive { get; }


        protected override void OnSubeventChanged(object o, CollectionOperationEventArgs<IEvent> e)
        {
            base.OnSubeventChanged(o, e);
            InvalidateRequerySuggested();
        }

        protected override bool CanAddNextMovie(object o)
        {
            return Parent is EventPanelRundownViewmodel && base.CanAddNextMovie(o);
        }

        protected override bool CanAddNewLive(object o)
        {
            return Parent is EventPanelRundownViewmodel && base.CanAddNewLive(o);
        }

        protected override void OnDispose()
        {
            if (IsSelected)
            {
                var p = Prior;
                if (p != null)
                    p.IsSelected = true;
            }
            base.OnDispose();
        }

        private void _addSubLive(object obj)
        {
            EngineViewmodel.AddSimpleEvent(Event, TEventType.Live, true);
        }

        private void _addSubRundown(object obj)
        {
            EngineViewmodel.AddSimpleEvent(Event, TEventType.Rundown, true);
        }

        private void _addSubMovie(object obj)
        {
            EngineViewmodel.AddMediaEvent(Event, TStartType.WithParent, TMediaType.Movie, VideoLayer.Program, false);
        }

        private bool _canAddSubEvent(object o)
        {
            return Event.SubEventsCount == 0 && Engine.HaveRight(EngineRight.Rundown);
        }

    }
}
