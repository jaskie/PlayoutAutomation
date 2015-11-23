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

        public ObservableSynchronizedCollection<IMediaSegment> MediaSegments
        {
            get
            {
                return new ObservableSynchronizedCollection<IMediaSegment>();
            }
        }

        public IMediaSegment CreateSegment()
        {
            throw new NotImplementedException();
        }

        public bool Save()
        {
            return Query<bool>();
        }
    }
}
