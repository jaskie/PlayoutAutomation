using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Client.Model;
using TAS.Remoting.Client;
using TAS.Server.Interfaces;

namespace TAS.Client.Converters
{
    class IMediaConverter : DtoCreationConverter<IMedia>
    {
        public IMediaConverter(IRemoteClient client): base(client){ }
        public override IMedia Create(Type objectType)
        {
            if (objectType.IsInterface)
            {
                if (objectType == typeof(IServerMedia))
                    return new ServerMedia();
                if (objectType == typeof(IArchiveMedia))
                    return new ArchiveMedia();
                if (objectType == typeof(IIngestMedia))
                    return new IngestMedia();
            }
            return (IMedia)Activator.CreateInstance(objectType);
        }
    }
}
