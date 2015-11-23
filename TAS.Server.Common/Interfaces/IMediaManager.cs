using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using TAS.Common;
using TAS.Server.Common;

namespace TAS.Server.Interfaces
{
    public interface IMediaManager: IInitializable, IDto
    {
        IEngine getEngine();
        IAnimationDirectory AnimationDirectoryPGM { get; }
        IAnimationDirectory AnimationDirectoryPRV { get; }
        IServerDirectory MediaDirectoryPGM { get; }
        IServerDirectory MediaDirectoryPRV { get; }
        IArchiveDirectory ArchiveDirectory { get; }
        List<IIngestDirectory> IngestDirectories { get; }
        IFileManager FileManager { get; }
        VideoFormatDescription FormatDescription { get; }
        TVideoFormat VideoFormat { get; }

        void IngestMediaToPlayout(IMedia media, bool toTop = false);
        void IngestMediaToPlayout(IEnumerable<IMedia> mediaList, bool ToTop = false);
        void IngestMediaToArchive(IIngestMedia media, bool toTop = false);
        void IngestMediaToArchive(IArchiveMedia media, bool toTop = false);
        void IngestMediaToArchive(IEnumerable<IIngestMedia> mediaList, bool ToTop = false);
        void Queue(IFileOperation operation, bool toTop = false);
        void ArchiveMedia(IMedia media, bool deleteAfter);
        void Export(IEnumerable<MediaExport> exportList, IIngestDirectory directory);
        void Export(MediaExport export, IIngestDirectory directory);
        IEnumerable<MediaDeleteDenyReason> DeleteMedia(IDto[] mediaList);

        void ReloadIngestDirs();

        void GetLoudness(IEnumerable<IMedia> mediaList);
        void GetLoudnessWithCallback(IMedia media, TimeSpan startTime, TimeSpan duration, EventHandler<AudioVolumeMeasuredEventArgs> audioVolumeMeasuredCallback, Action finishCallback);
        IMedia GetPRVMedia(IMedia media);

    }
}
