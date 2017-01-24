using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Remoting.Model
{
    public class PersistentMedia : Media, IPersistentMedia
    {
        public TMediaEmphasis MediaEmphasis { get { return Get<TMediaEmphasis>(); } set { Set(value); } }

        public string IdAux { get { return Get<string>(); } set { Set(value); } }
        [JsonProperty(nameof(IPersistentMedia.MediaSegments))]
        private ObservableSynchronizedCollection<MediaSegment> _mediaSegments { get { return Get<ObservableSynchronizedCollection<MediaSegment>>(); }  set { SetLocalValue(value); } }
        [JsonIgnore]
        public ObservableSynchronizedCollection<IMediaSegment> MediaSegments { get { return new ObservableSynchronizedCollection<IMediaSegment>(new object(), _mediaSegments); } }

        public DateTime KillDate { get { return Get<DateTime>(); } set { Set(value); } }

        public UInt64 IdProgramme { get { return Get<UInt64>(); } set { Set(value); } }

        public UInt64 IdPersistentMedia { get; set; }

        public bool IsModified { get; set; }

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
