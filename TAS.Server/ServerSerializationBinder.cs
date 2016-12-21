using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using TAS.Remoting;

namespace TAS.Server
{
    public class ServerSerializationBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            switch (typeName)
            {
                case "TAS.Client.Model.ServerMedia":
                    return typeof(TAS.Server.ServerMedia);
                case "TAS.Client.Model.IngestMedia":
                    return typeof(TAS.Server.IngestMedia);
                case "TAS.Client.Model.ArchiveMedia":
                    return typeof(TAS.Server.ArchiveMedia);
                case "TAS.Client.Model.ServerDirectory":
                    return typeof(TAS.Server.ServerDirectory);
                case "TAS.Client.Model.IngestDirectory":
                    return typeof(TAS.Server.IngestDirectory);
                case "TAS.Client.Model.ArchiveDirectory":
                    return typeof(TAS.Server.ArchiveDirectory);
                case "TAS.Client.Model.FileManager":
                    return typeof(TAS.Server.FileManager);
                case "TAS.Client.Model.MediaSegment":
                    return typeof(TAS.Server.MediaSegment);
                case "TAS.Client.Model.ConvertOperation":
                    return typeof(TAS.Server.ConvertOperation);
                case "TAS.Client.Model.FileOperation":
                    return typeof(TAS.Server.ConvertOperation);
                case "TAS.Client.Model.Engine":
                    return typeof(TAS.Server.Engine);
                case "TAS.Client.Model.MediaManager":
                    return typeof(TAS.Server.MediaManager);
                case "TAS.Client.Model.CasparServerChannel":
                    return typeof(TAS.Server.CasparServerChannel);
                case "TAS.Client.Model.CasparServer":
                    return typeof(TAS.Server.CasparServer);
                default:
                    if (assemblyName == "System")
                        return Type.GetType(typeName, true);
                    else
                        return Type.GetType(string.Format("{0}, {1}", typeName, assemblyName), true);
            }
        }
        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            typeName = serializedType.FullName;
            assemblyName = serializedType.Assembly.FullName;
        }
    }
}
