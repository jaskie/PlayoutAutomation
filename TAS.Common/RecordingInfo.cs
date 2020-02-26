using System.Xml.Serialization;

namespace TAS.Common
{
    public class RecordingInfo
    {     
        public int ServerId { get; set; }
        public int RecorderId { get; set; }
        public int ChannelId { get; set; }
    }
}
