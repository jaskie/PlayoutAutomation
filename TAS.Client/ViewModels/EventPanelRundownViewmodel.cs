using System.ComponentModel;
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
            CommandAddSubMovie = new UiCommand(CommandName(nameof(AddSubMovie)), AddSubMovie, CanAddSubEvent);
            CommandAddSubRundown = new UiCommand(CommandName(nameof(AddSubRundown)), AddSubRundown, CanAddSubEvent);
            CommandAddSubLive = new UiCommand(CommandName(nameof(AddSubLive)), AddSubLive, CanAddSubEvent);
        }

        public ICommand CommandAddSubRundown { get; }
        public ICommand CommandAddSubMovie { get; }
        public ICommand CommandAddSubLive { get; }


        protected override void OnSubeventChanged(object o, CollectionOperationEventArgs<IEvent> e)
        {
            base.OnSubeventChanged(o, e);
            InvalidateRequerySuggested();
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

        protected override bool CanAddNextMovie(object o) => Parent is EventPanelRundownViewmodel && base.CanAddNextMovie(o);

        protected override bool CanAddNewLive(object o) => Parent is EventPanelRundownViewmodel && base.CanAddNewLive(o);

        protected override void OnEventPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnEventPropertyChanged(sender, e);
            if (e.PropertyName == nameof(IEvent.CurrentUserRights))
                InvalidateRequerySuggested();

        }

        private void AddSubLive(object _) => EngineViewmodel.AddSimpleEvent(Event, TEventType.Live, true);

        private void AddSubRundown(object _) => EngineViewmodel.AddSimpleEvent(Event, TEventType.Rundown, true);

        private void AddSubMovie(object _) => EngineViewmodel.AddMediaEvent(Event, TStartType.WithParent, TMediaType.Movie, VideoLayer.Program, false);

        private bool CanAddSubEvent(object _) => Event.SubEventsCount == 0 && Event.HaveRight(EventRight.Create);

    }
}
