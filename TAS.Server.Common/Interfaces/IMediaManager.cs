using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using TAS.Common;
using TAS.Remoting;
using TAS.Server.Common;

namespace TAS.Server.Interfaces
{
    public interface IMediaManager: IInitializable, IDto
    {
        IEngine getEngine();
        IAnimationDirectory AnimationDirectoryPRI { get; }
        IAnimationDirectory AnimationDirectorySEC { get; }
        IAnimationDirectory AnimationDirectoryPRV { get; }
        IServerDirectory MediaDirectoryPRI { get; }
        IServerDirectory MediaDirectorySEC { get; }
        IServerDirectory MediaDirectoryPRV { get; }
        IArchiveDirectory ArchiveDirectory { get; }
        List<IIngestDirectory> IngestDirectories { get; }
        IFileManager FileManager { get; }
        VideoFormatDescription FormatDescription { get; }
        TVideoFormat VideoFormat { get; }

        void CopyMediaToPlayout(IEnumerable<IMedia> mediaList, bool ToTop = false);
        void ArchiveMedia(IEnumerable<IMedia> mediaList, bool deleteAfter);
        void Export(IEnumerable<ExportMedia> exportList, IIngestDirectory directory);
        IEnumerable<MediaDeleteDenyReason> DeleteMedia(IEnumerable<IMedia> mediaList);

        void ReloadIngestDirs();
        void SynchronizeSecToPri(bool deleteNotExisted);

        void GetLoudness(IEnumerable<IMedia> mediaList);
    }
}
