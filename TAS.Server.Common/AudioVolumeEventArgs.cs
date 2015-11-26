using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server.Common
{
    public class AudioVolumeEventArgs : EventArgs
    {
        public AudioVolumeEventArgs(decimal volume)
        {
            AudioVolume = volume;
        }
        public decimal AudioVolume { get; private set; }
    }
}
