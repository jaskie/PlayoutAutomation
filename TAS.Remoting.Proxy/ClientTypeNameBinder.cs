using Newtonsoft.Json.Serialization;
using System;

namespace TAS.Remoting
{
    public class ClientTypeNameBinder : ISerializationBinder
    {
        private ClientTypeNameBinder() { }
        public static ClientTypeNameBinder Current { get; } = new ClientTypeNameBinder();

        public Type BindToType(string assemblyName, string typeName)
        {
            switch (typeName)
            {
                case "TAS.Server.Media.ServerMedia":
                    return typeof(Model.Media.ServerMedia);
                case "TAS.Server.Media.IngestMedia":
                    return typeof(Model.Media.IngestMedia);
                case "TAS.Server.XDCAM.XdcamMedia":
                    return typeof(Model.Media.XDCAMMedia);
                case "TAS.Server.Media.ArchiveMedia":
                    return typeof(Model.Media.ArchiveMedia);
                case "TAS.Server.Media.AnimatedMedia":
                    return typeof(Model.Media.AnimatedMedia);
                case "TAS.Server.Media.ServerDirectory":
                    return typeof(Model.Media.ServerDirectory);
                case "TAS.Server.Media.IngestDirectory":
                    return typeof(Model.Media.IngestDirectory);
                case "TAS.Server.Media.ArchiveDirectory":
                    return typeof(Model.Media.ArchiveDirectory);
                case "TAS.Server.Media.AnimationDirectory":
                    return typeof(Model.Media.AnimationDirectory);
                case "TAS.Server.Media.MediaSearchProvider":
                    return typeof(Model.Media.MediaSearchProvider);
                case "TAS.Server.Media.MediaSegment":
                    return typeof(Model.MediaSegment);
                case "TAS.Server.Media.MediaSegments":
                    return typeof(Model.MediaSegments);
                case "TAS.Server.FileManager":
                    return typeof(Model.FileManager);
                case "TAS.Server.CasparRecorder":
                    return typeof(Model.Recorder);
                case "TAS.Server.MediaOperation.CopyOperation":
                    return typeof(Model.MediaOperation.CopyOperation);
                case "TAS.Server.MediaOperation.MoveOperation":
                    return typeof(Model.MediaOperation.MoveOperation);
                case "TAS.Server.MediaOperation.DeleteOperation":
                    return typeof(Model.MediaOperation.DeleteOperation);
                case "TAS.Server.MediaOperation.IngestOperation":
                    return typeof(Model.MediaOperation.IngestOperation);
                case "TAS.Server.MediaOperation.ExportOperation":
                    return typeof(Model.MediaOperation.ExportOperation);
                case "TAS.Server.MediaOperation.LoudnessOperation":
                    return typeof(Model.MediaOperation.LoudnessOperation);
                case "TAS.Server.Engine":
                    return typeof(Model.Engine);
                case "TAS.Server.Preview":
                    return typeof(Model.Preview);
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
                case "TAS.Server.CgElementsController":
                    return typeof(Model.CGElementsController);
                case "TAS.Server.RouterController":
                    return typeof(Model.Router);
                case "TAS.Server.RouterPort":
                    return typeof(Model.RouterPort);
                case "TAS.Server.CGElement":
                    return typeof(Model.CGElement);
                case "TAS.Server.Security.AuthenticationService":
                    return typeof(Model.Security.AuthenticationService);
                case "TAS.Server.Security.User":
                    return typeof(Model.Security.User);
                case "TAS.Server.Security.Group":
                    return typeof(Model.Security.Group);
                case "TAS.Server.Security.EventAclRight":
                    return typeof(Model.Security.EventAclRight);
                case "TAS.Server.Security.EngineAclRight":
                    return typeof(Model.Security.EngineAclRight);
                default:
                        return Type.GetType($"{typeName}, {assemblyName}", true);
            }
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            typeName = serializedType.FullName;
            assemblyName = serializedType.Assembly.FullName;
        }

    }
}
