using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Client.Model;
using TAS.Remoting.Client;
using TAS.Server.Interfaces;

namespace TAS.Client.Converters
{
    class IPlayoutServerConverter : DtoCreationConverter<IPlayoutServer>
    {
        public IPlayoutServerConverter(RemoteClient client) : base(client) { }
        public override IPlayoutServer Create(Type objectType)
        {
            return new PlayoutServer();
        }
    }
}
