using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Client.Model;
using TAS.Remoting.Client;
using TAS.Server.Interfaces;

namespace TAS.Client.Converters
{
    class IMediaManagerConverter: DtoCreationConverter<IMediaManager>
    {
        public IMediaManagerConverter(IRemoteClient client): base(client) { }
        public override IMediaManager Create(Type objectType)
        {
            return new MediaManager();
        }
    }
}
