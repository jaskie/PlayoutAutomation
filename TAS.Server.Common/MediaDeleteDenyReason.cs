using Infralution.Localization.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Common
{
    [DataContract]
    public struct MediaDeleteDenyReason
    {
        public static MediaDeleteDenyReason NoDeny = new MediaDeleteDenyReason() { Reason = MediaDeleteDenyReasonEnum.NoDeny };
        public enum MediaDeleteDenyReasonEnum
        {
            NoDeny,
            MediaInFutureSchedule,
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
