using Newtonsoft.Json;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model.MediaOperation
{
    public class MoveOperation : FileOperationBase, IMoveOperation
    {
#pragma warning disable CS0649

        [JsonProperty(nameof(IMoveOperation.DestProperties))]
        private IMediaProperties _destProperties;

        [JsonProperty(nameof(IMoveOperation.DestDirectory))]
        private MediaDirectoryBase _destDirectory;

        [JsonProperty(nameof(IMoveOperation.Source))]
        private MediaBase _source;

#pragma warning restore

        public IMediaProperties DestProperties { get => _destProperties; set => Set(value); }

        public IMediaDirectory DestDirectory { get => _destDirectory; set => Set(value); }

        public IMedia Source { get => _source; set => Set(value); }
        
    }
}
