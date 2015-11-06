using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.Model
{
    public class ServerMedia : Media, IServerMedia
    {
        public TMediaEmphasis MediaEmphasis
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public ObservableSynchronizedCollection<IMediaSegment> MediaSegments
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IMedia OriginalMedia
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public IMediaSegment CreateSegment()
        {
            throw new NotImplementedException();
        }

        public bool Save()
        {
            throw new NotImplementedException();
        }
    }
}
