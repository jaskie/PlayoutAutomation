using System;

namespace TAS.Common
{
    public class AudioVolumeEventArgs : EventArgs
    {
        public AudioVolumeEventArgs(decimal audioVolume)
        {
            AudioVolume = audioVolume;
        }

        [Newtonsoft.Json.JsonProperty]
        public decimal AudioVolume { get; private set; }
    }
}
