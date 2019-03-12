using Newtonsoft.Json;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model.MediaOperation
{
    public class CopyOperation: FileOperationBase, ICopyOperation
    {
        #pragma warning disable CS0649

        [JsonProperty(nameof(ICopyOperation.DestProperties))]
        private IMediaProperties _destProperties;

        [JsonProperty(nameof(ICopyOperation.DestDirectory))]
        private MediaDirectoryBase _destDirectory;

        [JsonProperty(nameof(ICopyOperation.Source))]
        private MediaBase _source;

        #pragma warning restore

        public IMediaProperties DestProperties { get => _destProperties; set => Set(value); }

        public IMediaDirectory DestDirectory { get => _destDirectory; set => Set(value); }

        public IMedia Source { get => _source; set => Set(value); }


    }
}
