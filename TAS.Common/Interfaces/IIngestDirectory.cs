using System.Collections.Generic;

namespace TAS.Common.Interfaces
{
    public interface IIngestDirectory: IMediaDirectory, IIngestDirectoryProperties
    {
        TDirectoryAccessType AccessType { get; }
        int XdcamClipCount { get; }
        string Filter { get; set; }
    }

    public interface IIngestDirectoryProperties : IMediaDirectoryProperties
    {
        TAspectConversion AspectConversion { get; }
        decimal AudioVolume { get;  }
        bool DeleteSource { get; }
        bool IsExport { get; }
        bool IsImport { get; }
        TVideoCodec VideoCodec { get; }
        TAudioCodec AudioCodec { get; }
        decimal VideoBitrateRatio { get; }
        decimal AudioBitrateRatio { get; }
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
