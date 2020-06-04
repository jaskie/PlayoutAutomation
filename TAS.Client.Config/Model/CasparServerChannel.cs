using System.ComponentModel;
using TAS.Database.Common;
using TAS.Common.Interfaces.Configurator;

namespace TAS.Client.Config.Model
{
    public class CasparServerChannel: IConfigCasparChannel
    {
        [Hibernate]
        public int Id { get; set; }

        [Hibernate]
        public string ChannelName { get; set; }

        [Hibernate]
        public double MasterVolume { get; set; }

        [Hibernate]
        public string LiveDevice { get; set; }

        [Hibernate]
        public string PreviewUrl { get; set; }

        [DefaultValue(2), Hibernate]
        public int AudioChannelCount { get; set; } = 2;

        public object Owner { get; set; }

        public override string ToString()
        {
            return $"{Owner} - {ChannelName}";
        }
    }
}
