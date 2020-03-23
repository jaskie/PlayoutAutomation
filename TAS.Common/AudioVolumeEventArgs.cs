using System;

namespace TAS.Common
{
    public class AudioVolumeEventArgs : EventArgs
    {
        public AudioVolumeEventArgs(double audioVolume)
        {
            AudioVolume = audioVolume;
        }

        public double AudioVolume { get; }
    }
}
