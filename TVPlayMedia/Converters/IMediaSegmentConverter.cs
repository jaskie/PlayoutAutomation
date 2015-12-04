using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Client.Model;
using TAS.Remoting.Client;
using TAS.Server.Interfaces;

namespace TAS.Client.Converters
{
    class IMediaSegmentConverter: DtoCreationConverter<IMediaSegment>
    {
        public IMediaSegmentConverter(IRemoteClient client) : base(client) { }
        public override IMediaSegment Create(Type objectType)
        {
            return new MediaSegment();
        }
    }
}
