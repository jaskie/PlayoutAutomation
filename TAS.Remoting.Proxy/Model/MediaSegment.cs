using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TAS.Remoting.Client;
using TAS.Server.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class MediaSegment : ProxyBase, IMediaSegment
    {
        public string SegmentName { get { return Get<string>(); } set { Set(value); } }
        public TimeSpan TcIn { get { return Get<TimeSpan>(); } set { Set(value); } }
        public TimeSpan TcOut { get { return Get<TimeSpan>(); } set { Set(value); } }

        public void Delete()
        {
            Invoke();
        }

        public void Save()
        {
            Invoke();
        }

        protected override void OnEventNotification(WebSocketMessage e) { }

    }
}
