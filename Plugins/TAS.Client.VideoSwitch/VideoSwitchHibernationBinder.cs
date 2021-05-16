using System;
using System.Collections.Generic;
using TAS.Database.Common;
using TAS.Server.VideoSwitch.Model;

namespace TAS.Server.VideoSwitch
{
    public class VideoSwitchHibernationBinder : HibernationBinder
    {
        private VideoSwitchHibernationBinder() : base(new Dictionary<Type, Type> {
            { typeof(SmartVideoHub), typeof(SmartVideoHub) },
            { typeof(Ross), typeof(Ross) },
            { typeof(Nevion), typeof(Nevion) },
            { typeof(Atem), typeof(Atem) },
            { typeof(RouterPort), typeof(RouterPort) }
        })
        { }

        public static VideoSwitchHibernationBinder Current { get; } = new VideoSwitchHibernationBinder();
    }
}
