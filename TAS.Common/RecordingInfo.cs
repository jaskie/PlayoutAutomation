using System.Xml.Serialization;

namespace TAS.Common
{
    public class RecordingInfo
    {     
        [XmlElement]
        public int ServerId { get; set; }
        [XmlElement]
        public bool IsRecordingScheduled { get; set; }
        [XmlElement]
        public int RecorderId { get; set; }
        [XmlElement]
        public int ChannelId { get; set; }

        public RecordingInfo()
        {

        }
        public RecordingInfo(int recorderId, int channelId, string filename, bool isRecorderScheduled)
        {
            RecorderId = recorderId;
            ChannelId = channelId;            
            IsRecordingScheduled = isRecorderScheduled;
        }
    }
}
