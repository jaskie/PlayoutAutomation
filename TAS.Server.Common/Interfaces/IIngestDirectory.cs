using System.Collections.Generic;

namespace TAS.Server.Common.Interfaces
{
    public interface IIngestDirectory: IMediaDirectory, IIngestDirectoryProperties
    {
        TDirectoryAccessType AccessType { get; }
        int XdcamClipCount { get; }
        string Filter { get; set; }
    }

    public interface IIngestDirectoryProperties : IMediaDirectoryProperties
    {
        TAspectConversion AspectConversion { get; set; }
        decimal AudioVolume { get; set; }
        bool DeleteSource { get; set; }
        bool IsExport { get; set; }
        bool IsImport { get; set; }
        TVideoCodec VideoCodec { get; set; }
        TAudioCodec AudioCodec { get; set; }
        decimal VideoBitrateRatio { get; set; }
        decimal AudioBitrateRatio { get; set; }
        string EncodeParams { get; set; }
        TMovieContainerFormat ExportContainerFormat { get; set; }
        string ExportParams { get; set; }
        TIngestDirectoryKind Kind { get; set; }
        bool IsWAN { get; set; }
        bool IsRecursive { get; set; }
        string Username { get; set; }
        string Password { get; set; }
        string[] Extensions { get; set; }
        TMediaCategory MediaCategory { get; set; }
        bool MediaDoNotArchive { get; set; }
        int MediaRetnentionDays { get; set; }
        bool MediaLoudnessCheckAfterIngest { get; set; }
        TFieldOrder SourceFieldOrder { get; set; }
        TmXFAudioExportFormat MXFAudioExportFormat { get; set; }
        TmXFVideoExportFormat MXFVideoExportFormat { get; set; }
        TVideoFormat ExportVideoFormat { get; set; }
        IEnumerable<IIngestDirectoryProperties> SubDirectories { get; }
    }
}
