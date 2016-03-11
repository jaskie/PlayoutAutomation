using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Client.Model;
using TAS.Remoting.Client;
using TAS.Server.Interfaces;

namespace TAS.Client.Converters
{
    class IMediaDirectoryConverter : DtoCreationConverter<IMediaDirectory>
    {
        public IMediaDirectoryConverter(IRemoteClient client) : base(client) { }
        public override IMediaDirectory Create(Type objectType)
        {
            if (objectType.IsInterface)
            {
                if (objectType == typeof(IServerDirectory))
                    return new Model.ServerDirectory();
                if (objectType == typeof(IIngestDirectory))
                    return new Model.IngestDirectory();
                if (objectType == typeof(IArchiveDirectory))
                    return new Model.ArchiveDirectory();
                if (objectType == typeof(IAnimationDirectory))
                    return new Model.AnimationDirectory();
            }
            return (IMediaDirectory)Activator.CreateInstance(objectType);
        }
    }
}
