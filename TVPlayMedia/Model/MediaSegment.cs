using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Client.Model
{
    class MediaSegment : ProxyBase, IMediaSegment
    {
        public Guid MediaGuid { get { return Get<Guid>(); }  set { Set(value); } }
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
    }
}
