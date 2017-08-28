using System;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class PersistentMedia : Media, IPersistentMedia
    {
        public TMediaEmphasis MediaEmphasis { get { return Get<TMediaEmphasis>(); } set { Set(value); } }

        public string IdAux { get { return Get<string>(); } set { Set(value); } }

        public DateTime KillDate { get { return Get<DateTime>(); } set { Set(value); } }

        public ulong IdProgramme { get { return Get<ulong>(); } set { Set(value); } }

        public ulong IdPersistentMedia { get; set; }

        public bool IsModified { get; set; }

        public bool Protected { get { return Get<bool>(); } set { Set(value); } }

        public IMediaSegments MediaSegments { get { return Get<MediaSegments>(); } set { SetLocalValue(value); } }

        public bool Save()
        {
            return Query<bool>();
        }
    }
}
