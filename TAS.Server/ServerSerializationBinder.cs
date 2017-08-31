using System;
using System.Linq;
using TAS.Remoting;
using TAS.Server.Media;
using Newtonsoft.Json.Serialization;

namespace TAS.Server
{
    public class ServerSerializationBinder : ISerializationBinder
    {
        public Type BindToType(string assemblyName, string typeName)
        {
            switch (typeName)
            {
                case "TAS.Client.Model.ServerMedia":
                    return typeof(ServerMedia);
                case "TAS.Client.Model.IngestMedia":
                    return typeof(IngestMedia);
                case "TAS.Client.Model.ArchiveMedia":
                    return typeof(ArchiveMedia);
                case "TAS.Client.Model.ServerDirectory":
                    return typeof(ServerDirectory);
                case "TAS.Client.Model.IngestDirectory":
                    return typeof(IngestDirectory);
                case "TAS.Client.Model.ArchiveDirectory":
                    return typeof(ArchiveDirectory);
                case "TAS.Client.Model.FileManager":
                    return typeof(FileManager);
                case "TAS.Client.Model.MediaSegment":
                    return typeof(MediaSegment);
                case "TAS.Client.Model.ConvertOperation":
                    return typeof(IngestOperation);
                case "TAS.Client.Model.FileOperation":
                    return typeof(IngestOperation);
                case "TAS.Client.Model.Engine":
                    return typeof(Engine);
                case "TAS.Client.Model.MediaManager":
                    return typeof(MediaManager);
                case "TAS.Client.Model.CasparServerChannel":
                    return typeof(CasparServerChannel);
                case "TAS.Client.Model.CasparServer":
                    return typeof(CasparServer);
                default:
                    if (assemblyName == "System")
                        return Type.GetType(typeName, true);
                    else
                        return Type.GetType($"{typeName}, {assemblyName}", true);
            }
        }
        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            var attribute =  serializedType.GetCustomAttributes(typeof(TypeNameOverrideAttribute), true).FirstOrDefault() as TypeNameOverrideAttribute;
            if (attribute != null)
            {
                typeName = attribute.TypeName;
                assemblyName = attribute.AssemblyName;
            }
            else
            {
                typeName = serializedType.FullName;
                assemblyName = serializedType.Assembly.FullName;
            }
        }
    }
}
