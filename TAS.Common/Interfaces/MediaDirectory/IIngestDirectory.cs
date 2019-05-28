using System.Collections.Generic;
using TAS.Common.Interfaces.Media;

namespace TAS.Common.Interfaces.MediaDirectory
{

    public interface IIngestDirectory: IWatcherDirectory, ISearchableDirectory, IIngestDirectoryProperties
    {
        TDirectoryAccessType AccessType { get; }
        int XdcamClipCount { get; }
    }

    public interface IIngestDirectoryProperties : IMediaDirectoryProperties
    {
        string DirectoryName { get; set; }
        TAspectConversion AspectConversion { get; }
        double AudioVolume { get;  }
        bool DeleteSource { get; }
        bool IsExport { get; }
        bool IsImport { get; }
        TVideoCodec VideoCodec { get; }
        TAudioCodec AudioCodec { get; }
        double VideoBitrateRatio { get; }
        double AudioBitrateRatio { get; }
        string EncodeParams { get; }
        TMovieContainerFormat ExportContainerFormat { get; }
        string ExportParams { get;  }
        TIngestDirectoryKind Kind { get; }
        bool IsWAN { get; }
        bool IsRecursive { get; }
        string Username { get; }
        string Password { get; }
        string[] Extensions { get; }
        TMediaCategory MediaCategory { get; }
        bool MediaDoNotArchive { get; }
        int MediaRetnentionDays { get; }
        bool MediaLoudnessCheckAfterIngest { get; }
        TFieldOrder SourceFieldOrder { get;  }
        TmXFAudioExportFormat MXFAudioExportFormat { get; }
        TmXFVideoExportFormat MXFVideoExportFormat { get; }
        TVideoFormat ExportVideoFormat { get; }
        IEnumerable<IIngestDirectoryProperties> SubDirectories { get; }
    }
}
