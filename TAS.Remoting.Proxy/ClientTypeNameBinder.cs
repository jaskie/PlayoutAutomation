using System;
using System.Linq;

namespace TAS.Remoting
{
    public class ClientTypeNameBinder : Newtonsoft.Json.Serialization.ISerializationBinder
    {
        public Type BindToType(string assemblyName, string typeName)
        {
            switch (typeName)
            {
                case "TAS.Server.Media.ServerMedia":
                    return typeof(Model.ServerMedia);
                case "TAS.Server.Media.IngestMedia":
                    return typeof(Model.IngestMedia);
                case "TAS.Server.Media.ArchiveMedia":
                    return typeof(Model.ArchiveMedia);
                case "TAS.Server.Media.AnimatedMedia":
                    return typeof(Model.AnimatedMedia);
                case "TAS.Server.Media.ServerDirectory":
                    return typeof(Model.ServerDirectory);
                case "TAS.Server.Media.IngestDirectory":
                    return typeof(Model.IngestDirectory);
                case "TAS.Server.Media.ArchiveDirectory":
                    return typeof(Model.ArchiveDirectory);
                case "TAS.Server.Media.AnimationDirectory":
                    return typeof(Model.AnimationDirectory);
                case "TAS.Server.Media.MediaSegment":
                    return typeof(Model.MediaSegment);
                case "TAS.Server.Media.MediaSegments":
                    return typeof(Model.MediaSegments);
                case "TAS.Server.FileManager":
                    return typeof(Model.FileManager);
                case "TAS.Server.CasparRecorder":
                    return typeof(Model.Recorder);
                case "TAS.Server.ConvertOperation":
                    return typeof(Model.IngestOperation);
                case "TAS.Server.FileOperation":
                    return typeof(Model.FileOperation);
                case "TAS.Server.LoudnessOperation":
                    return typeof(Model.LoudnessOperation);
                case "TAS.Server.ExportOperation":
                    return typeof(Model.FileOperation);
                case "TAS.Server.Engine":
                    return typeof(Model.Engine);
                case "TAS.Server.Event":
                    return typeof(Model.Event);
                case "TAS.Server.CommandScriptEvent":
                    return typeof(Model.CommandScriptEvent);
                case "TAS.Server.AnimatedEvent":
                    return typeof(Model.AnimatedEvent);
                case "TAS.Server.MediaManager":
                    return typeof(Model.MediaManager);
                case "TAS.Server.CasparServerChannel":
                    return typeof(Model.PlayoutServerChannel);
                case "TAS.Server.CasparServer":
                    return typeof(Model.PlayoutServer);
                case "TAS.Server.CGElementsController":
                    return typeof(Model.CGElementsController);
                case "TAS.Server.CGElement":
                    return typeof(Model.CGElement);
                case "TAS.Server.Security.AuthenticationService":
                    return typeof(Model.Security.AuthenticationService);
                case "TAS.Server.Security.User":
                    return typeof(Model.Security.User);
                case "TAS.Server.Security.Group":
                    return typeof(Model.Security.Group);
                case "TAS.Server.Security.EventAclItem":
                    return typeof(Model.Security.EventAclItem);
                default:
                        return Type.GetType($"{typeName}, {assemblyName}", true);
            }
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            var attribute = serializedType.GetCustomAttributes(typeof(TypeNameOverrideAttribute), true).FirstOrDefault() as TypeNameOverrideAttribute;
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
