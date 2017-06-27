using TAS.Server.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EventPanelAnimationViewmodel: EventPanelRundownElementViewmodelBase
    {
        public EventPanelAnimationViewmodel(IEvent ev, EventPanelViewmodelBase parent): base(ev, parent) { }
        public override bool IsEnabled => Event.IsEnabled;
    }
}
