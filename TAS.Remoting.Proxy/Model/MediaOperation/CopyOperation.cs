using jNet.RPC;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Remoting.Model.Media;

namespace TAS.Remoting.Model.MediaOperation
{
    public class CopyOperation: FileOperationBase, ICopyOperation
    {
        #pragma warning disable CS0649

        [DtoMember(nameof(ICopyOperation.DestDirectory))]
        private MediaDirectoryBase _destDirectory;

        [DtoMember(nameof(ICopyOperation.Source))]
        private MediaBase _source;

        #pragma warning restore

        public IMediaDirectory DestDirectory { get => _destDirectory; set => Set(value); }

        public IMedia Source { get => _source; set => Set(value); }

    }
}
