using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Client.Model;
using TAS.Remoting.Client;
using TAS.Server.Interfaces;

namespace TAS.Client.Converters
{
    class MediaManagerConverter: DtoCreationConverter<MediaManager>
    {
        public MediaManagerConverter(IRemoteClient client): base(client) { }
        public override MediaManager Create(Type objectType)
        {
            return new MediaManager();
        }
    }
}
