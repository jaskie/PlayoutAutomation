using System.Linq;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class EventPanelLiveViewModel: EventPanelRundownElementViewModelBase
    {
        private string _videoSwitchPortName = string.Empty;
        public EventPanelLiveViewModel(IEvent ev, EventPanelViewModelBase parent) : base(ev, parent) 
        {
            VideoSwitchPortName = EngineViewModel.VideoSwitch?.Sources?.FirstOrDefault(p => p.PortId == Event.VideoSwitchPort)?.PortName;
            Event.PropertyChanged += Event_PropertyChanged;
        }

        private void Event_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(Event.VideoSwitchPort):
                    VideoSwitchPortName = EngineViewModel.VideoSwitch?.Sources?.FirstOrDefault(p => p.PortId == Event.VideoSwitchPort)?.PortName;
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
