using System.Windows.Input;
using TAS.Client.Common;
using TAS.Server.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EventPanelRundownViewmodel: EventPanelRundownElementViewmodelBase
    {
        public EventPanelRundownViewmodel(IEvent ev, EventPanelViewmodelBase parent) : base(ev, parent)
        {
            CommandAddSubMovie = new UICommand { ExecuteDelegate = _addSubMovie, CanExecuteDelegate = o => _event.SubEventsCount == 0 };
            CommandAddSubRundown = new UICommand { ExecuteDelegate = _addSubRundown, CanExecuteDelegate = o => _event.SubEventsCount == 0 };
            CommandAddSubLive = new UICommand { ExecuteDelegate = _addSubLive, CanExecuteDelegate = o => _event.SubEventsCount == 0 };
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
            return _parent is EventPanelRundownViewmodel && base.CanAddNextMovie(o);
        }

        protected override bool CanAddNewLive(object o)
        {
            return _parent is EventPanelRundownViewmodel && base.CanAddNewLive(o);
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
            EngineViewmodel.AddSimpleEvent(_event, TEventType.Live, true);
        }

        private void _addSubRundown(object obj)
        {
            EngineViewmodel.AddSimpleEvent(_event, TEventType.Rundown, true);
        }

        private void _addSubMovie(object obj)
        {
            EngineViewmodel.AddMediaEvent(_event, TStartType.WithParent, TMediaType.Movie, VideoLayer.Program, false);
        }

    }
}
