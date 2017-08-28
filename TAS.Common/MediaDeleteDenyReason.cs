using System.Runtime.Serialization;
using TAS.Common.Interfaces;

namespace TAS.Common
{
    [DataContract]
    public struct MediaDeleteDenyReason
    {
        public static MediaDeleteDenyReason NoDeny = new MediaDeleteDenyReason { Reason = MediaDeleteDenyReasonEnum.NoDeny };
        public enum MediaDeleteDenyReasonEnum
        {
            NoDeny,
            MediaInFutureSchedule,
            Protected,
            Unknown,
        }
        [DataMember]
        public MediaDeleteDenyReasonEnum Reason;
        [IgnoreDataMember]
        public IEventProperties Event;
        [DataMember]
        public IMedia Media;
    }
}
