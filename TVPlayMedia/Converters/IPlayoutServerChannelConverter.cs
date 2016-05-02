using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Client.Model;
using TAS.Remoting.Client;
using TAS.Server.Interfaces;

namespace TAS.Client.Converters
{
    class IPlayoutServerChannelConverter : DtoCreationConverter<IPlayoutServerChannel>
    {
        public IPlayoutServerChannelConverter(RemoteClient client) : base(client) { }
        public override IPlayoutServerChannel Create(Type objectType)
        {
            return new PlayoutServerChannel();
        }
    }
}
