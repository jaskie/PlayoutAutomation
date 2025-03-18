using System;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;


namespace TAS.Client.ViewModels
{
    public class EventPanelContainerViewmodel: EventPanelViewmodelBase
    {

        private bool _isVisible;

        public EventPanelContainerViewmodel(IEvent ev, EventPanelViewmodelBase parent): base(ev, parent) {
            if (ev.EventType != TEventType.Container)
                throw new ApplicationException($"Invalid panel type:{GetType()} for event type:{ev.EventType}");
            _isVisible = !HiddenEventsStorage.Contains(ev);

            CommandHide = new UiCommand(CommandName(nameof(CommandHide)), _ => IsVisible = false, _ => _isVisible);
            CommandShow = new UiCommand(CommandName(nameof(CommandShow)), _ => IsVisible = true, _ => !_isVisible);
            CommandAddSubRundown = new UiCommand(CommandName(nameof(AddSubRundown)), AddSubRundown, _ => Event.HaveRight(EventRight.Create));
        }

        public ICommand CommandHide { get; }
        public ICommand CommandShow { get; }
        public ICommand CommandPaste => EngineViewmodel.CommandPasteSelected;
        public ICommand CommandAddSubRundown { get; }

        public override bool IsVisible
        {
            get => _isVisible;
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
                    EngineViewmodel.RootEventViewModel.NotifyContainerVisibility();
                }
            }
        }

        private void AddSubRundown(object _)
        {
            EngineViewmodel.AddSimpleEvent(Event, TEventType.Rundown, true);
        }
    }
}
