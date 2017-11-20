using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using TAS.Common;
using TAS.Remoting;

namespace TAS.Remoting
{
    public class ClientTypeNameBinder : Newtonsoft.Json.Serialization.ISerializationBinder
    {
        public Type BindToType(string assemblyName, string typeName)
        {
            switch (typeName)
            {
                case "TAS.Server.ServerMedia":
                    return typeof(Model.ServerMedia);
                case "TAS.Server.IngestMedia":
                    return typeof(Model.IngestMedia);
                case "TAS.Server.XDCAM.XDCAMMedia":
                    return typeof(Model.XDCAMMedia);
                case "TAS.Server.ArchiveMedia":
                    return typeof(Model.ArchiveMedia);
                case "TAS.Server.AnimatedMedia":
                    return typeof(Model.AnimatedMedia);
                case "TAS.Server.ServerDirectory":
                    return typeof(Model.ServerDirectory);
                case "TAS.Server.IngestDirectory":
                    return typeof(Model.IngestDirectory);
                case "TAS.Server.ArchiveDirectory":
                    return typeof(Model.ArchiveDirectory);
                case "TAS.Server.AnimationDirectory":
                    return typeof(Model.AnimationDirectory);
                case "TAS.Server.FileManager":
                    return typeof(Model.FileManager);
                case "TAS.Server.CasparRecorder":
                    return typeof(Model.Recorder);
                case "TAS.Server.MediaSegment":
                    return typeof(Model.MediaSegment);
                case "TAS.Server.MediaSegments":
                    return typeof(Model.MediaSegments);
                case "TAS.Server.ConvertOperation":
                    return typeof(Model.ConvertOperation);
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
                default:
                        return Type.GetType(string.Format("{0}, {1}", typeName, assemblyName), true);
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
