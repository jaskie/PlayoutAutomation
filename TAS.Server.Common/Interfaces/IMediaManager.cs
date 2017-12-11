using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using TAS.Common;
using TAS.Server.Common;

namespace TAS.Server.Interfaces
{
    public interface IMediaManager: IInitializable
    {
        IAnimationDirectory AnimationDirectoryPRI { get; }
        IAnimationDirectory AnimationDirectorySEC { get; }
        IAnimationDirectory AnimationDirectoryPRV { get; }
        IServerDirectory MediaDirectoryPRI { get; }
        IServerDirectory MediaDirectorySEC { get; }
        IServerDirectory MediaDirectoryPRV { get; }
        IArchiveDirectory ArchiveDirectory { get; }
        IEngine Engine { get; }
        IEnumerable<IIngestDirectory> IngestDirectories { get; }
        IEnumerable<IRecorder> Recorders { get; }
        IFileManager FileManager { get; }
        VideoFormatDescription FormatDescription { get; }
        TVideoFormat VideoFormat { get; }
        ICGElementsController CGElementsController { get; }

        void CopyMediaToPlayout(IEnumerable<IMedia> mediaList, bool ToTop = false);
        void ArchiveMedia(IEnumerable<IServerMedia> mediaList, bool deleteAfter);
        void Export(IEnumerable<ExportMedia> exportList, bool asSingleFile, string singleFilename, IIngestDirectory directory, TmXFAudioExportFormat mXFAudioExportFormat, TmXFVideoExportFormat mXFVideoExportFormat);
        IEnumerable<MediaDeleteDenyReason> DeleteMedia(IEnumerable<IMedia> mediaList, bool forceDelete);

        void ReloadIngestDirs();
        void SynchronizeMediaSecToPri(bool deleteNotExisted);
        void SynchronizeAnimationsSecToPri();

        void MeasureLoudness(IEnumerable<IMedia> mediaList);
    }
}
