using jNet.RPC;
using System;
using System.Threading.Tasks;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Server.Media;

namespace TAS.Server.MediaOperation
{
    public class DeleteOperation : FileOperationBase, IDeleteOperation
    {

        private IMedia _source;

        [DtoMember]
        public IMedia Source { get => _source; set => SetField(ref _source, value); }

        
        protected override void OnOperationStatusChanged()
        {
        }

        protected override async Task<bool> InternalExecute()
        {
            StartTime = DateTime.UtcNow;
            if (!(Source is MediaBase source))
                return false;
            return await Task.Run(() =>
            {
                if (!source.Delete()) return false;
                (source.Directory as MediaDirectoryBase)?.RefreshVolumeInfo();
                return true;
            });
        }

        public override string ToString()
        {
            return $"Delete: {Source}";
        }
    }
}
