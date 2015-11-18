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
            throw new NotImplementedException();
        }

        public IEnumerable<MediaDeleteDenyReason> DeleteMedia(IEnumerable<IMedia> mediaList)
        {
            throw new NotImplementedException();
        }

        public MediaDeleteDenyReason DeleteMedia(IMedia media)
        {
            throw new NotImplementedException();
        }

        public void Export(MediaExport export, IIngestDirectory directory)
        {
            throw new NotImplementedException();
        }

        public void Export(IEnumerable<MediaExport> exportList, IIngestDirectory directory)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public List<IMediaDirectory> getDirectories()
        {
            return Query<List<MediaDirectory>>().Cast<IMediaDirectory>().ToList();
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

        public List<IIngestDirectory> getIngestDirectories()
        {
            throw new NotImplementedException();
        }

        public void GetLoudness(IMedia media)
        {
            throw new NotImplementedException();
        }

        public void GetLoudness(IEnumerable<IMedia> mediaList)
        {
            throw new NotImplementedException();
        }

        public void GetLoudness(IMedia media, TimeSpan startTime, TimeSpan duration, EventHandler<AudioVolumeMeasuredEventArgs> audioVolumeMeasuredCallback, Action finishCallback)
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
            throw new NotImplementedException();
        }

        public void IngestMediaToPlayout(IMedia media, bool toTop = false)
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public override void OnMessage(object sender, WebSocketMessageEventArgs e)
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
