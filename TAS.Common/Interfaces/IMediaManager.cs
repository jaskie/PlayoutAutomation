using System.Collections.Generic;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Common.Interfaces
{
    public interface IMediaManager
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
        ICGElementsController CGElementsController { get; }
        void CopyMediaToPlayout(IEnumerable<IMedia> mediaList);
        List<MediaDeleteResult> MediaArchive(IEnumerable<IMedia> mediaList, bool deleteAfter, bool forceDelete);
        List<MediaDeleteResult> MediaDelete(IEnumerable<IMedia> mediaList, bool forceDelete);
        void Export(IEnumerable<MediaExportDescription> exportList, bool asSingleFile, string singleFilename, IIngestDirectory directory, TmXFAudioExportFormat mXFAudioExportFormat, TmXFVideoExportFormat mXFVideoExportFormat);
        void SynchronizeMediaSecToPri();
        void SynchronizeAnimationsSecToPri();
        IServerDirectory DetermineValidServerDirectory();

        void MeasureLoudness(IEnumerable<IMedia> mediaList);
    }
}
