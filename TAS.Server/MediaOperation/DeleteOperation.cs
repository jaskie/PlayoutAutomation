using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Server.Media;

namespace TAS.Server.MediaOperation
{
    public class DeleteOperation : FileOperationBase, IDeleteOperation
    {

        private IMedia _source;

        internal DeleteOperation(FileManager ownerFileManager): base(ownerFileManager)
        {
        }
       
        [JsonProperty]
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
                if (!Source.Delete()) return false;
                ((MediaDirectoryBase) Source.Directory).RefreshVolumeInfo();
                return true;
            });
        }
        
    }
}
