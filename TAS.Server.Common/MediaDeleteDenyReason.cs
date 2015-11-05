using Infralution.Localization.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Common
{
    public struct MediaDeleteDenyReason
    {
        public static MediaDeleteDenyReason NoDeny = new MediaDeleteDenyReason() { Reason = MediaDeleteDenyReasonEnum.NoDeny };
        public enum MediaDeleteDenyReasonEnum
        {
            NoDeny,
            MediaInFutureSchedule,
            Unknown,
        }
        public MediaDeleteDenyReasonEnum Reason;
        public IEventProperties Event;
        public IMedia Media;
    }
}
