using System;
using TAS.Remoting.Client;
using TAS.Common.Interfaces;

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

        public IMediaSegments Owner { get; }
        public ulong Id { get; set; }

        public void Save()
        {
            Invoke();
        }

        protected override void OnEventNotification(WebSocketMessage e) { }

    }
}
