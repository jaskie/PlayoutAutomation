using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using TAS.Remoting.Client;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model
{
    public class MediaManager : ProxyBase, IMediaManager
    {

        #pragma warning disable CS0649

        [JsonProperty(nameof(IMediaManager.AnimationDirectoryPRI))]
        private AnimationDirectory _animationDirectoryPRI;

        [JsonProperty(nameof(IMediaManager.MediaDirectoryPRI))]
        private ServerDirectory _mediaDirectoryPri;

        [JsonProperty(nameof(IMediaManager.AnimationDirectorySEC))]
        private AnimationDirectory _animationDirectorySEC;

        [JsonProperty(nameof(IMediaManager.IngestDirectories))]
        private List<IngestDirectory> _ingestDirectories;
        
        [JsonProperty(nameof(IMediaManager.AnimationDirectoryPRV))]
        private AnimationDirectory _animationDirectoryPRV;
        
        [JsonProperty(nameof(IMediaManager.ArchiveDirectory))]
        private ArchiveDirectory _archiveDirectory;

        [JsonProperty(nameof(IMediaManager.Recorders))]
        private List<Recorder> _recorders;

        [JsonProperty(nameof(IMediaManager.MediaDirectorySEC))]
        private ServerDirectory _mediaDirectorySec;


        [JsonProperty(nameof(IMediaManager.FileManager))]
        private FileManager _fileManager;

        [JsonProperty(nameof(IMediaManager.MediaDirectoryPRV))]
        private ServerDirectory _mediaDirectoryPrv;

        [JsonProperty(nameof(IMediaManager.FormatDescription))]
        private VideoFormatDescription _videoFormatDescription;

        [JsonProperty(nameof(IMediaManager.VideoFormat))]
        private TVideoFormat _videoFormat;

        [JsonProperty(nameof(IEngine.CGElementsController))]
        private CGElementsController _cgElementsController;

        #pragma warning restore

        public ICGElementsController CGElementsController => _cgElementsController;

        public IEngine Engine => Get<Engine>();

        public IFileManager FileManager => _fileManager;

        public VideoFormatDescription FormatDescription => _videoFormatDescription;

        public IEnumerable<IIngestDirectory> IngestDirectories => _ingestDirectories;

        public IAnimationDirectory AnimationDirectoryPRI => _animationDirectoryPRI;

        public IAnimationDirectory AnimationDirectorySEC => _animationDirectorySEC;

        public IAnimationDirectory AnimationDirectoryPRV => _animationDirectoryPRV;

        public IServerDirectory MediaDirectoryPRI => _mediaDirectoryPri;

        public IArchiveDirectory ArchiveDirectory => _archiveDirectory;

        public IServerDirectory MediaDirectorySEC => _mediaDirectorySec;

        public IServerDirectory MediaDirectoryPRV => _mediaDirectoryPrv;

        public TVideoFormat VideoFormat => _videoFormat;


        public IServerDirectory DetermineValidServerDirectory()
        {
            return Query<ServerDirectory>();
        }

        public void MeasureLoudness(IEnumerable<IMedia> mediaList)
        {
            Invoke(parameters: mediaList);
        }
       
        public void CopyMediaToPlayout(IEnumerable<IMedia> mediaList) { Invoke(parameters: mediaList); }

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public void LoadIngestDirs()
        {
            Invoke();
        }

        public void SynchronizeMediaSecToPri()
        {
            Invoke();
        }

        public void SynchronizeAnimationsSecToPri()
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
