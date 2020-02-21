using System.ComponentModel;
using TAS.Common;
using TAS.Common.Database;
using TAS.Common.Interfaces;

namespace TAS.Client.Config.Model
{
    public class CasparServerChannel: IPlayoutServerChannelProperties
    {
        [Hibernate]
        public int Id { get; set; }

        [Hibernate]
        public string ChannelName { get; set; }

        [DefaultValue(1.0), Hibernate]
        public double MasterVolume { get; set; } = 1;

        [Hibernate]
        public string LiveDevice { get; set; }

        [Hibernate]
        public string PreviewUrl { get; set; }

        [DefaultValue(2), Hibernate]
        public int AudioChannelCount { get; set; } = 2;

        internal object Owner;

        public override string ToString()
        {
            return $"{Owner} - {ChannelName}";
        }
    }
}
