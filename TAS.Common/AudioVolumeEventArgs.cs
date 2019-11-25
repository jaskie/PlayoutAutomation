using System;

namespace TAS.Common
{
    public class AudioVolumeEventArgs : EventArgs
    {
        public AudioVolumeEventArgs(double audioVolume)
        {
            AudioVolume = audioVolume;
        }

        [Newtonsoft.Json.JsonProperty]
        public double AudioVolume { get; private set; }
    }
}
