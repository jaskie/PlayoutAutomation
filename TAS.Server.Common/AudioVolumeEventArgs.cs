using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server.Common
{
    public class AudioVolumeEventArgs : EventArgs
    {
        public AudioVolumeEventArgs(decimal audioVolume)
        {
            AudioVolume = audioVolume;
        }
        public decimal AudioVolume { get; }
    }
}
