using System;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Server.Common;
using TAS.Server.Common.Interfaces;


namespace TAS.Client.ViewModels
{
    public class EventPanelContainerViewmodel: EventPanelViewmodelBase
    {

        private bool _isVisible;

        public EventPanelContainerViewmodel(IEvent ev, EventPanelViewmodelBase parent): base(ev, parent) {
            if (ev.EventType != TEventType.Container)
                throw new ApplicationException($"Invalid panel type:{GetType()} for event type:{ev.EventType}");
            _isVisible = !HiddenEventsStorage.Contains(ev);

            CommandHide = new UICommand {ExecuteDelegate = o => IsVisible = false,CanExecuteDelegate = o => _isVisible };
            CommandShow = new UICommand {ExecuteDelegate = o => IsVisible = true, CanExecuteDelegate = o => !_isVisible };
            CommandAddSubRundown = new UICommand {ExecuteDelegate = _addSubRundown};
        }

        public ICommand CommandHide { get; }
        public ICommand CommandShow { get; }
        public ICommand CommandPaste => EngineViewmodel.CommandPasteSelected;
        public ICommand CommandAddSubRundown { get; }

        public int ChildrenCount => Event.SubEventsCount;

        public override bool IsVisible
        {
            get { return _isVisible; }
            protected set
            {
                if (SetField(ref _isVisible, value))
                {
                    if (value)
                        HiddenEventsStorage.Remove(Event);
                    else
                        HiddenEventsStorage.Add(Event);
                    if (!value)
                        IsSelected = false;
                    Root.NotifyContainerVisibility();
                }
            }
        }

        protected override void OnSubeventChanged(object o, CollectionOperationEventArgs<IEvent> e)
        {
            base.OnSubeventChanged(o, e);
            NotifyPropertyChanged(nameof(ChildrenCount));
        }

        private void _addSubRundown(object o)
        {
            EngineViewmodel.AddSimpleEvent(Event, TEventType.Rundown, true);
        }
    }
}
