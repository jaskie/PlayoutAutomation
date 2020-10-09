using System;

namespace TAS.Server.VideoSwitch.Model
{
    public class MixEffectEventArgs : EventArgs
    {
        public int ProgramInput { get; }
        public MixEffectEventArgs(int programInput)
        {
            ProgramInput = programInput;
        }
    }
}
