using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.Config.Model
{
    public class CasparServerChannel: IPlayoutServerChannelProperties
    {
        public CasparServerChannel()
        {
            MasterVolume = 1;
        }
        public int Id { get; set; }
        public string ChannelName { get; set; }
        public double MasterVolume { get; set; } 
        public string LiveDevice { get; set; }
        public string PreviewUrl { get; set; }

        public TVideoFormat VideoFormat { get; set; }
        
        internal object Owner;
        public override string ToString()
        {
            return $"{Owner} - {ChannelName}";
        }
    }
}
