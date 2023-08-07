using System.Collections.Generic;
using jNet.RPC;
using jNet.RPC.Client;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Remoting.Model.Media;

namespace TAS.Remoting.Model
{
    public class MediaManager : ProxyObjectBase, IMediaManager
    {

        #pragma warning disable CS0649

        [DtoMember(nameof(IMediaManager.AnimationDirectoryPRI))]
        private AnimationDirectory _animationDirectoryPRI;

        [DtoMember(nameof(IMediaManager.MediaDirectoryPRI))]
        private ServerDirectory _mediaDirectoryPri;

        [DtoMember(nameof(IMediaManager.AnimationDirectorySEC))]
        private AnimationDirectory _animationDirectorySEC;

        [DtoMember(nameof(IMediaManager.IngestDirectories))]
        private List<IngestDirectory> _ingestDirectories;
        
        [DtoMember(nameof(IMediaManager.AnimationDirectoryPRV))]
        private AnimationDirectory _animationDirectoryPRV;
        
        [DtoMember(nameof(IMediaManager.ArchiveDirectory))]
        private ArchiveDirectory _archiveDirectory;

        [DtoMember(nameof(IMediaManager.Recorders))]
        private List<Recorder> _recorders;

        [DtoMember(nameof(IMediaManager.MediaDirectorySEC))]
        private ServerDirectory _mediaDirectorySec;

        [DtoMember(nameof(IMediaManager.FileManager))]
        private FileManager _fileManager;

        [DtoMember(nameof(IMediaManager.MediaDirectoryPRV))]
        private ServerDirectory _mediaDirectoryPrv;

        #pragma warning restore

        public IEngine Engine => Get<Engine>();

        public IFileManager FileManager => _fileManager;

        public IEnumerable<IIngestDirectory> IngestDirectories => _ingestDirectories;

        public IAnimationDirectory AnimationDirectoryPRI => _animationDirectoryPRI;

        public IAnimationDirectory AnimationDirectorySEC => _animationDirectorySEC;

        public IAnimationDirectory AnimationDirectoryPRV => _animationDirectoryPRV;

        public IServerDirectory MediaDirectoryPRI => _mediaDirectoryPri;

        public IArchiveDirectory ArchiveDirectory => _archiveDirectory;

        public IServerDirectory MediaDirectorySEC => _mediaDirectorySec;

        public IServerDirectory MediaDirectoryPRV => _mediaDirectoryPrv;

        public IServerDirectory DetermineValidServerDirectory()
        {
            return Query<ServerDirectory>();
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


        protected override void OnEventNotification(SocketMessage message) { }


    }
}
