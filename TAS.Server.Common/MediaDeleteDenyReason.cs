using Infralution.Localization.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Common
{
    public struct MediaDeleteDeny
    {
        public static MediaDeleteDeny NoDeny = new MediaDeleteDeny() { Reason = MediaDeleteDenyReason.NoDeny };

        public enum MediaDeleteDenyReason
        {
            NoDeny,
            MediaInFutureSchedule,
            Unknown,
        }
        public MediaDeleteDenyReason Reason;
        public IEventProperties Event;
        public IMedia Media;
    }


    class MediaDeleteDenyReasonEnumConverter : ResourceEnumConverter
    {
        public MediaDeleteDenyReasonEnumConverter()
            : base(typeof(MediaDeleteDeny.MediaDeleteDenyReason), TAS.Server.Common.Properties.Resources.ResourceManager)
        { }
    }
}
