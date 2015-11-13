using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using TAS.Common;
using TAS.Server.Common;

namespace TAS.Server.Interfaces
{
    [ServiceContract]
    public interface IMediaManager: IInitializable, IDto
    {
        IEngine getEngine();
        IAnimationDirectory getAnimationDirectoryPGM();
        IAnimationDirectory getAnimationDirectoryPRV();
        IServerDirectory getMediaDirectoryPGM();
        IServerDirectory getMediaDirectoryPRV();
        IArchiveDirectory getArchiveDirectory();
        List<IIngestDirectory> getIngestDirectories();
        ObservableSynchronizedCollection<ITemplate> getTemplates();
        IFileManager getFileManager();
        [OperationContract]
        VideoFormatDescription getFormatDescription();
        [OperationContract]
        TVideoFormat getVideoFormat();

        void IngestMediaToPlayout(IMedia media, bool toTop = false);
        void IngestMediaToPlayout(IEnumerable<IMedia> mediaList, bool ToTop = false);
        void IngestMediaToArchive(IIngestMedia media, bool toTop = false);
        void IngestMediaToArchive(IArchiveMedia media, bool toTop = false);
        void IngestMediaToArchive(IEnumerable<IIngestMedia> mediaList, bool ToTop = false);
        void Queue(IFileOperation operation, bool toTop = false);
        void ArchiveMedia(IMedia media, bool deleteAfter);
        void Export(IEnumerable<MediaExport> exportList, IIngestDirectory directory);
        void Export(MediaExport export, IIngestDirectory directory);
        MediaDeleteDenyReason DeleteMedia(IMedia media);
        IEnumerable<MediaDeleteDenyReason> DeleteMedia(IEnumerable<IMedia> mediaList);

        void ReloadIngestDirs();

        void GetLoudness(IEnumerable<IMedia> mediaList);
        void GetLoudness(IMedia media);
        void GetLoudness(IMedia media, TimeSpan startTime, TimeSpan duration, EventHandler<AudioVolumeMeasuredEventArgs> audioVolumeMeasuredCallback, Action finishCallback);
        List<IMediaDirectory> getDirectories();

    }
}
