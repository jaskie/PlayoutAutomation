using BMDSwitcherAPI;
using System;

namespace TAS.Server.VideoSwitch.Helpers
{
    internal class AtemMonitor : IBMDSwitcherMixEffectBlockCallback, IBMDSwitcherCallback
    {
        public event EventHandler ProgramInputChanged;
        public event EventHandler ConnectionChanged;
        public void Notify(_BMDSwitcherMixEffectBlockEventType eventType)
        {
            switch(eventType)
            {                
                case _BMDSwitcherMixEffectBlockEventType.bmdSwitcherMixEffectBlockEventTypeProgramInputChanged:
                    ProgramInputChanged?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }

        public void Notify(_BMDSwitcherEventType eventType, _BMDSwitcherVideoMode coreVideoMode)
        {
            switch(eventType)
            {
                case _BMDSwitcherEventType.bmdSwitcherEventTypeDisconnected:
                    ConnectionChanged?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }
    }
}
