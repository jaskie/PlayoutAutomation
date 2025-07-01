using System.Collections.Generic;
using jNet.RPC;
using jNet.RPC.Client;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model
{
    public class MediaManager : ProxyObjectBase, IMediaManager
    {

        #pragma warning disable CS0649, IDE0044

        [DtoMember(nameof(IMediaManager.AnimationDirectoryPRI))]
        private IAnimationDirectory _animationDirectoryPRI;

        [DtoMember(nameof(IMediaManager.MediaDirectoryPRI))]
        private IServerDirectory _mediaDirectoryPri;

        [DtoMember(nameof(IMediaManager.AnimationDirectorySEC))]
        private IAnimationDirectory _animationDirectorySEC;

        [DtoMember(nameof(IMediaManager.IngestDirectories))]
        private IIngestDirectory[] _ingestDirectories;
        
        [DtoMember(nameof(IMediaManager.AnimationDirectoryPRV))]
        private IAnimationDirectory _animationDirectoryPRV;
        
        [DtoMember(nameof(IMediaManager.ArchiveDirectory))]
        private IArchiveDirectory _archiveDirectory;

        [DtoMember(nameof(IMediaManager.Recorders))]
        private IRecorder[] _recorders;

        [DtoMember(nameof(IMediaManager.MediaDirectorySEC))]
        private IServerDirectory _mediaDirectorySec;

        [DtoMember(nameof(IMediaManager.FileManager))]
        private IFileManager _fileManager;

        [DtoMember(nameof(IMediaManager.MediaDirectoryPRV))]
        private IServerDirectory _mediaDirectoryPrv;

        #pragma warning restore

        public IEngine Engine => Get<IEngine>();

        public IFileManager FileManager => _fileManager;

        public IEnumerable<IIngestDirectory> IngestDirectories => _ingestDirectories;

        public IAnimationDirectory AnimationDirectoryPRI => _animationDirectoryPRI;

        public IAnimationDirectory AnimationDirectorySEC => _animationDirectorySEC;

        public IAnimationDirectory AnimationDirectoryPRV => _animationDirectoryPRV;

        public IServerDirectory MediaDirectoryPRI => _mediaDirectoryPri;

        public IArchiveDirectory ArchiveDirectory => _archiveDirectory;

        public IServerDirectory MediaDirectorySEC => _mediaDirectorySec;

        public IServerDirectory MediaDirectoryPRV => _mediaDirectoryPrv;

        public IWatcherDirectory DetermineValidServerDirectory(bool forAnimations)
        {
            return Query<IWatcherDirectory>(parameters: new object[] { forAnimations });
        }

        public void MeasureLoudness(IEnumerable<IMedia> mediaList)
        {
            Invoke(parameters: mediaList);
        }

        public void CopyMediaToPlayout(IEnumerable<IMedia> mediaList) { Invoke(parameters: mediaList); }

        public void SynchronizeMediaSecToPri()
        {
            Invoke();
        }

        public void SynchronizeAnimationsPropertiesSecToPri()
        {
            Invoke();
        }

        public void Export(IEnumerable<MediaExportDescription> exportList, bool asSingleFile, string singleFilename, IIngestDirectory directory)
        {
            Invoke(parameters: new object[] { exportList, asSingleFile, singleFilename, directory });
        }

        public IEnumerable<IRecorder> Recorders => _recorders;

        public List<MediaDeleteResult> MediaArchive(IEnumerable<IMedia> mediaList, bool deleteAfter, bool forceDelete)
        {
            return Query<List<MediaDeleteResult>>(parameters: new object[] { mediaList, deleteAfter, forceDelete });
        }

        public List<MediaDeleteResult> MediaDelete(IEnumerable<IMedia> mediaList, bool forceDelete)
        {
            return Query<List<MediaDeleteResult>>(parameters: new object[] { mediaList, forceDelete });
        }

        public void Export(IEnumerable<MediaExportDescription> exportList, bool asSingleFile, string singleFilename, IIngestDirectory directory, TmXFAudioExportFormat mXFAudioExportFormat, TmXFVideoExportFormat mXFVideoExportFormat)
        {
            Invoke(parameters: new object[] { exportList, asSingleFile, singleFilename, directory, mXFAudioExportFormat, mXFVideoExportFormat });
        }

    }
}
