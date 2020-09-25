using System.Linq;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EventPanelLiveViewmodel: EventPanelRundownElementViewmodelBase
    {
        private string _videoSwitchPortName = string.Empty;
        public EventPanelLiveViewmodel(IEvent ev, EventPanelViewmodelBase parent) : base(ev, parent) 
        {
            VideoSwitchPortName = EngineViewmodel.Router?.Sources?.FirstOrDefault(p => p.PortId == Event.RouterPort)?.PortName;
            Event.PropertyChanged += Event_PropertyChanged;
        }

        private void Event_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(Event.RouterPort):
                    VideoSwitchPortName = EngineViewmodel.Router?.Sources?.FirstOrDefault(p => p.PortId == Event.RouterPort)?.PortName;
                    break;
            }
        }

        protected override void OnDispose()
        {
            Event.PropertyChanged -= Event_PropertyChanged;
            if (IsSelected)
            {
                var p = Prior;
                if (p != null)
                    p.IsSelected = true;
            }
            base.OnDispose();            
        }

        public string VideoSwitchPortName { get => _videoSwitchPortName; private set => SetField(ref _videoSwitchPortName, value); }
    }
}
