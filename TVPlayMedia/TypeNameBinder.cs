using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using TAS.Common;

namespace TAS.Client
{
    public class TypeNameBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            switch (typeName)
            {
                case "TAS.Server.ServerMedia":
                    return typeof(Model.ServerMedia);
                case "TAS.Server.IngestMedia":
                    return typeof(Model.IngestMedia);
                case "TAS.Server.ArchiveMedia":
                    return typeof(Model.ArchiveMedia);
                case "TAS.Server.ServerDirectory":
                    return typeof(Model.ServerDirectory);
                case "TAS.Server.IngestDirectory":
                    return typeof(Model.IngestDirectory);
                case "TAS.Server.ArchiveDirectory":
                    return typeof(Model.ArchiveDirectory);
                case "TAS.Server.FileManager":
                    return typeof(Model.FileManager);
                case "TAS.Server.MediaSegment":
                    return typeof(Model.MediaSegment);
                case "TAS.Server.ConvertOperation":
                    return typeof(Model.ConvertOperation);
                case "TAS.Server.FileOperation":
                    return typeof(Model.FileOperation);
                case "TAS.Server.LoudnessOperation":
                    return typeof(Model.LoudnessOperation);

                case "TAS.Server.Engine":
                    return typeof(Model.Engine);
                case "TAS.Server.MediaManager":
                    return typeof(Model.MediaManager);
                case "TAS.Server.CasparServerChannel":
                    return typeof(Model.PlayoutServerChannel);
                case "TAS.Server.CasparServer":
                    return typeof(Model.PlayoutServer);

                case "Interfaces.IIngestDirectory":
                    return typeof(Model.IngestDirectory);
                case "System.ComponentModel.PropertyChangedEventArgs":
                    return typeof(System.ComponentModel.PropertyChangedEventArgs);
                case "TAS.Common.VideoFormatDescription":
                    return typeof(VideoFormatDescription);

                default:
                    if (assemblyName == "System")
                        return Type.GetType(typeName, true);
                    else
                        return Type.GetType(string.Format("{0}, {1}", typeName, assemblyName), true);
            }
        }
    }
}
