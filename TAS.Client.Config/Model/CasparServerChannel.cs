using System.ComponentModel;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.Config.Model
{
    public class CasparServerChannel: IPlayoutServerChannelProperties
    {
        public int Id { get; set; }

        public string ChannelName { get; set; }

        [DefaultValue(1.0)]
        public double MasterVolume { get; set; } = 1;

        public string LiveDevice { get; set; }

        public string PreviewUrl { get; set; }

        [DefaultValue(2)]
        public int AudioChannelCount { get; set; } = 2;

        public TVideoFormat VideoFormat { get; set; }
        
        internal object Owner;

        public override string ToString()
        {
            return $"{Owner} - {ChannelName}";
        }
    }
}
