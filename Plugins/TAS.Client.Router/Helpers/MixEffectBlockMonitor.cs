using BMDSwitcherAPI;
using System;

namespace TAS.Server.VideoSwitch.Helpers
{
    internal class MixEffectBlockMonitor : IBMDSwitcherMixEffectBlockCallback
    {
        public event EventHandler ProgramInputChanged;
        public void Notify(_BMDSwitcherMixEffectBlockEventType eventType)
        {
            switch(eventType)
            {
                case _BMDSwitcherMixEffectBlockEventType.bmdSwitcherMixEffectBlockEventTypeProgramInputChanged:
                    ProgramInputChanged?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }
    }
}
