using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace TAS.Client
{
    public class TypeNameBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            switch (typeName)
            {
                case "ServerMedia":
                    return typeof(Model.ServerMedia);
                case "IngestMedia":
                    return typeof(Model.IngestMedia);
                case "ArchiveMedia":
                    return typeof(Model.ArchiveMedia);

                case "ServerDirectory":
                    return typeof(Model.ServerDirectory);
                case "IngestDirectory":
                    return typeof(Model.IngestDirectory);
                case "ArchiveDirectory":
                    return typeof(Model.ArchiveDirectory);
                case "FileManager":
                    return typeof(Model.FileManager);
                case "MediaSegment":
                    return typeof(Model.MediaSegment);
                case "ConvertOperation":
                    return typeof(Model.ConvertOperation);
                case "FileOperation":
                    return typeof(Model.ConvertOperation);


                case "Engine":
                    return typeof(Model.Engine);
                case "MediaManager":
                    return typeof(Model.MediaManager);
                case "CasparServerChannel":
                    return typeof(Model.PlayoutServerChannel);
                case "CasparServer":
                    return typeof(Model.PlayoutServer);

                case "TAS.Server.Interfaces.IIngestDirectory":
                    return typeof(Model.IngestDirectory);
                case "System.ComponentModel.PropertyChangedEventArgs":
                    return typeof(System.ComponentModel.PropertyChangedEventArgs);
                case "System.Collections.Generic.List`1[[TAS.Server.Interfaces.IIngestDirectory, TAS.Server.Common]]":
                    return typeof(List<Model.IngestDirectory>);
                default:
                    if (assemblyName == "System")
                        return Type.GetType(typeName, true);
                    else
                        return Type.GetType(string.Format("{0}, {1}", typeName, assemblyName), true);
            }
        }
        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            base.BindToName(serializedType, out assemblyName, out typeName);
        }
    }
}
