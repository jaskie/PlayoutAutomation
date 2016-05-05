using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Client.Model;
using TAS.Remoting.Client;
using TAS.Server.Interfaces;

namespace TAS.Client.Converters
{
    class IEngineConverter: DtoCreationConverter<IEngine>
    {
        public IEngineConverter(RemoteClient client): base(client) { }
        public override IEngine Create(Type objectType)
        {
            return new Engine();
        }
    }
}
