using System.ComponentModel;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EventPanelRundownViewModel: EventPanelRundownElementViewModelBase
    {
        public EventPanelRundownViewModel(IEvent ev, EventPanelViewModelBase parent) : base(ev, parent)
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
            return Parent is EventPanelRundownViewModel && base.CanAddNextMovie(o);
        }

        protected override bool CanAddNewLive(object o)
        {
            return Parent is EventPanelRundownViewModel && base.CanAddNewLive(o);
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

        protected override void OnEventPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnEventPropertyChanged(sender, e);
            if (e.PropertyName == nameof(IEvent.CurrentUserRights))
                InvalidateRequerySuggested();

        }

        private void _addSubLive(object obj)
        {
            EngineViewModel.AddSimpleEvent(Event, TEventType.Live, VideoLayer.Program, true);
        }

        private void _addSubRundown(object obj)
        {
            EngineViewModel.AddSimpleEvent(Event, TEventType.Rundown, VideoLayer.None, true);
        }

        private void _addSubMovie(object obj)
        {
            EngineViewModel.AddMediaEvent(Event, TStartType.WithParent, new[] { TMediaType.Movie }, VideoLayer.Program, false);
        }

        private bool _canAddSubEvent(object o)
        {
            return Event.SubEventsCount == 0 && Event.HaveRight(EventRight.Create);
        }

    }
}
