using System.Collections.Generic;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Remoting.Model.Media;

namespace TAS.Remoting.Model.MediaOperation
{
    public class ExportOperation: FileOperationBase, IExportOperation
    {

        #pragma warning disable CS0649
        
        [JsonProperty(nameof(IExportOperation.DestProperties))]
        private IMediaProperties _destProperties;

        [JsonProperty(nameof(IExportOperation.DestDirectory))]
        private MediaDirectoryBase _destDirectory;

        [JsonProperty(nameof(IExportOperation.Sources))]
        private List<MediaExportDescription> _sources;
        
        #pragma warning restore 

        public IEnumerable<MediaExportDescription> Sources => _sources;

        public IMediaProperties DestProperties { get => _destProperties; set => Set(value); }

        public IMediaDirectory DestDirectory { get => _destDirectory; set => Set(value); }

    }
}
