using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.Model
{
    public class PersistentMedia : Media, IPersistentMedia
    {
        public TMediaEmphasis MediaEmphasis { get { return Get<TMediaEmphasis>(); } set { Set(value); } }

        public string IdAux { get { return Get<string>(); } set { Set(value); } }

        public ObservableSynchronizedCollection<IMediaSegment> MediaSegments { get { return Get<ObservableSynchronizedCollection<IMediaSegment>>(); }  set { Set(value); }  }

        public DateTime KillDate { get { return Get<DateTime>(); } set { Set(value); } }

        public UInt64 IdProgramme { get { return Get<UInt64>(); } set { Set(value); } }

        public UInt64 IdPersistentMedia { get; set; }

        public bool Modified { get; set; }

        public bool Protected { get { return Get<bool>(); } set { Set(value); } }

        public IMediaSegment CreateSegment()
        {
            return Query<IMediaSegment>();
        }

        public bool Save()
        {
            return Query<bool>();
        }
    }
}
