using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Server.Media;

namespace TAS.Server.MediaOperation
{
    public class MoveOperation : FileOperationBase, IMoveOperation
    {

        private readonly object _destMediaLock = new object();

        private IMedia _sourceMedia;
        private IMediaProperties _destMediaProperties;

        internal MoveOperation(FileManager ownerFileManager): base(ownerFileManager)
        {
        }
       
        [JsonProperty]
        public IMediaProperties DestProperties { get => _destMediaProperties; set => SetField(ref _destMediaProperties, value); }

        [JsonProperty]
        public IMediaDirectory DestDirectory { get; set; }

        [JsonProperty]
        public IMedia Source { get => _sourceMedia; set => SetField(ref _sourceMedia, value); }

        internal MediaBase Dest { get; set; }

        
        protected override void OnOperationStatusChanged()
        {
        }

        protected virtual void CreateDestMediaIfNotExists()
        {
            lock (_destMediaLock)
            {
                if (Dest != null)
                    return;
                if (!(DestDirectory is MediaDirectoryBase mediaDirectory))
                    throw new ApplicationException($"Cannot create destination media on {DestDirectory}");
                Dest = (MediaBase) mediaDirectory.CreateMedia(DestProperties ?? Source);
            }
        }

        protected override async Task<bool> InternalExecute()
        {
            StartTime = DateTime.UtcNow;
            if (!(Source is MediaBase source))
                return false;
            if (!File.Exists(source.FullPath) || !Directory.Exists(DestDirectory.Folder))
                return false;
            CreateDestMediaIfNotExists();
            if (Dest.FileExists())
                if (File.GetLastWriteTimeUtc(source.FullPath).Equals(File.GetLastWriteTimeUtc(Dest.FullPath))
                    && File.GetCreationTimeUtc(source.FullPath).Equals(File.GetCreationTimeUtc(Dest.FullPath))
                    && source.FileSize.Equals(Dest.FileSize))
                {
                    source.Delete();
                    ((MediaDirectoryBase)Source.Directory).RefreshVolumeInfo();
                    return true;
                }
                else if (!Dest.Delete())
                {
                    AddOutputMessage("Move operation failed - destination media not deleted");
                    return false;
                }
            IsIndeterminate = true;
            Dest.MediaStatus = TMediaStatus.Copying;
            FileUtils.CreateDirectoryIfNotExists(Path.GetDirectoryName(Dest.FullPath));
            File.Move(source.FullPath, Dest.FullPath);
            File.SetCreationTimeUtc(Dest.FullPath, File.GetCreationTimeUtc(source.FullPath));
            File.SetLastWriteTimeUtc(Dest.FullPath, File.GetLastWriteTimeUtc(source.FullPath));
            Dest.MediaStatus = TMediaStatus.Copied;
            await Task.Run(() => Dest.Verify(false));
            ((MediaDirectoryBase)Source.Directory).RefreshVolumeInfo();
            ((MediaDirectoryBase)DestDirectory).RefreshVolumeInfo();
            return true;
        }
        
    }
}
