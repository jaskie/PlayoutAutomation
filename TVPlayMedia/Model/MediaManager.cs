using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Common;
using TAS.Server.Interfaces;
using TAS.Server.Remoting;

namespace TAS.Client.Model
{
    public class MediaManager : ProxyBase, IMediaManager
    {
        public void ArchiveMedia(IMedia media, bool deleteAfter)
        {
            Invoke(parameters: new object[] { media, deleteAfter });
        }

        public List<MediaDeleteDenyReason> DeleteMedia(IEnumerable<IMedia> mediaList)
        {
            return Query<List<MediaDeleteDenyReason>>(parameters: mediaList);
        }

        public MediaDeleteDenyReason DeleteMedia(IMedia media)
        {
            throw new NotImplementedException();
        }

        public void Export(MediaExport export, IIngestDirectory directory)
        {
            Invoke(parameters: new object[] { export, directory });
        }

        public void Export(IEnumerable<MediaExport> exportList, IIngestDirectory directory)
        {
            Invoke(parameters: new object[] { exportList, directory });
        }

        public IAnimationDirectory getAnimationDirectoryPGM()
        {
            throw new NotImplementedException();
        }

        public IAnimationDirectory getAnimationDirectoryPRV()
        {
            throw new NotImplementedException();
        }

        public IArchiveDirectory getArchiveDirectory()
        {
            return Query<ArchiveDirectory>();
        }

        public IEngine getEngine()
        {
            throw new NotImplementedException();
        }

        public IFileManager getFileManager()
        {
            return null;
        }

        public VideoFormatDescription getFormatDescription()
        {
            throw new NotImplementedException();
        }

        public List<IIngestDirectory> IngestDirectories
        {
            get { return Get<List<Model.IngestDirectory>>().Cast<IIngestDirectory>().ToList(); }
            set { Set(value); }
        }

        public void GetLoudness(IEnumerable<IMedia> mediaList)
        {
            Invoke(parameters: mediaList);
        }

        public void GetLoudnessWithCallback(IMedia media, TimeSpan startTime, TimeSpan duration, EventHandler<AudioVolumeMeasuredEventArgs> audioVolumeMeasuredCallback, Action finishCallback)
        {
            throw new NotImplementedException();
        }

        public IServerDirectory getMediaDirectoryPGM()
        {
            return Query<ServerDirectory>();
        }

        public IServerDirectory getMediaDirectoryPRV()
        {
            return Query<ServerDirectory>();
        }

        public IMedia GetPRVMedia(IMedia media)
        {
            return Query<Media>(parameters: media);
        }

        public ObservableSynchronizedCollection<ITemplate> getTemplates()
        {
            throw new NotImplementedException();
        }

        public TVideoFormat getVideoFormat()
        {
            throw new NotImplementedException();
        }

        public void IngestMediaToArchive(IEnumerable<IIngestMedia> mediaList, bool ToTop = false)
        {
            throw new NotImplementedException();
        }

        public void IngestMediaToArchive(IArchiveMedia media, bool toTop = false)
        {
            throw new NotImplementedException();
        }

        public void IngestMediaToArchive(IIngestMedia media, bool toTop = false)
        {
            throw new NotImplementedException();
        }

        public void IngestMediaToPlayout(IEnumerable<IMedia> mediaList, bool ToTop = false)
        {
            Invoke(parameters: new object[] { mediaList, ToTop });
        }

        public void IngestMediaToPlayout(IMedia media, bool toTop = false)
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public void Queue(IFileOperation operation, bool toTop = false)
        {
            throw new NotImplementedException();
        }

        public void ReloadIngestDirs()
        {
            throw new NotImplementedException();
        }
    }
}
