using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Client.Model;
using TAS.Remoting.Client;
using TAS.Server.Interfaces;

namespace TAS.Client.Converters
{
    class IFileManagerConverter : DtoCreationConverter<IFileManager>
    {
        public IFileManagerConverter(RemoteClient client) : base(client) { }
        public override IFileManager Create(Type objectType)
        {
            return new FileManager();
        }
    }
}
