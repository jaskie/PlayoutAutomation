using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EventPanelLiveViewmodel: EventPanelRundownElementViewmodelBase
    {
        public EventPanelLiveViewmodel(IEvent ev, EventPanelViewmodelBase parent) : base(ev, parent) { }

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
    }
}
