using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Remoting.Client;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Remoting.Model
{
    public class MediaManager : ProxyBase, IMediaManager
    {
        public void ArchiveMedia(IEnumerable<IServerMedia> mediaList, bool deleteAfter)
        {
            Invoke(parameters: new object[] { mediaList, deleteAfter });
        }

        public IEnumerable<MediaDeleteDenyReason> DeleteMedia(IEnumerable<IMedia> mediaList, bool forceDelete)
        {
            return Query<List<MediaDeleteDenyReason>>(parameters: new object[] { mediaList, forceDelete });
        }

        public void Export(IEnumerable<ExportMedia> exportList, bool asSingleFile, string singleFilename, IIngestDirectory directory, TmXFAudioExportFormat mXFAudioExportFormat, TmXFVideoExportFormat mXFVideoExportFormat)
        {
            Invoke(parameters: new object[] { exportList, asSingleFile, singleFilename, directory, mXFAudioExportFormat, mXFVideoExportFormat });
        }

        public IAnimationDirectory AnimationDirectoryPRI { get { return Get<AnimationDirectory>(); } set { SetLocalValue(value); } }
        public IAnimationDirectory AnimationDirectorySEC { get { return Get<AnimationDirectory>(); } set { SetLocalValue(value); } }
        public IAnimationDirectory AnimationDirectoryPRV { get { return Get<AnimationDirectory>(); } set { SetLocalValue(value); } }

        public IArchiveDirectory ArchiveDirectory { get { return Get<ArchiveDirectory>(); } set { SetLocalValue(value); } }


        public IFileManager FileManager { get { return Get<FileManager>(); }  set { SetLocalValue(value); } }

        public VideoFormatDescription FormatDescription { get { return Get<VideoFormatDescription>(); } set { SetLocalValue(value); } }

        [JsonProperty(nameof(IMediaManager.IngestDirectories))]
        private List<IngestDirectory> _ingestDirectories { get { return Get<List<IngestDirectory>>(); }  set { SetLocalValue(value); } }
        [JsonIgnore]
        public IEnumerable<IIngestDirectory> IngestDirectories
        {
            get { return _ingestDirectories; }
        }

        public void MeasureLoudness(IEnumerable<IMedia> mediaList)
        {
            Invoke(parameters: mediaList);
        }

        public IServerDirectory MediaDirectoryPRI { get { return Get<ServerDirectory>(); } set { SetLocalValue(value); } }
        public IServerDirectory MediaDirectorySEC { get { return Get<ServerDirectory>(); } set { SetLocalValue(value); } }
        public IServerDirectory MediaDirectoryPRV { get { return Get<ServerDirectory>(); } set { SetLocalValue(value); } }

        public IMedia GetPRVMedia(IMedia media)
        {
            return Query<Media>(parameters: media);
        }

        public TVideoFormat VideoFormat { get { return Get<TVideoFormat>(); } set { SetLocalValue(value); } }

        public void CopyMediaToPlayout(IEnumerable<IMedia> mediaList, bool toTop) { Invoke(parameters: new object[] { mediaList, toTop }); }
        
        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public void ReloadIngestDirs()
        {
            Invoke();
        }

        public void SynchronizeMediaSecToPri(bool deleteNotExisted)
        {
            Invoke(parameters: deleteNotExisted);
        }

        public void SynchronizeAnimationsSecToPri()
        {
            Invoke();
        }

        public void Export(IEnumerable<ExportMedia> exportList, bool asSingleFile, string singleFilename, IIngestDirectory directory)
        {
            Invoke(parameters: new object[] { exportList, asSingleFile, singleFilename, directory });
        }

        [JsonProperty(nameof(IEngine.CGElementsController))]
        private CGElementsController _cgElementsController { get { return Get<CGElementsController>(); } set { SetLocalValue(value); } }
        [JsonIgnore]
        public ICGElementsController CGElementsController { get { return _cgElementsController; } }

        public IEngine Engine { get { return Get<Engine>(); }  set { SetLocalValue(value); } }

        public IEnumerable<IRecorder> Recorders
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        protected override void OnEventNotification(WebSocketMessage e) { }


    }
}
