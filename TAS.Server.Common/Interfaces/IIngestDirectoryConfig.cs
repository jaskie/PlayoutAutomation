using System;
using TAS.Common;

namespace TAS.Server.Interfaces
{
    public interface IIngestDirectoryConfig: IMediaDirectoryConfig
    {
        TAspectConversion AspectConversion { get; set; }
        decimal AudioVolume { get; set; }
        bool DeleteSource { get; set; }
        bool DoNotEncode { get; set; }
        bool IsExport { get; set; }
        bool IsImport { get; set; }
        string EncodeParams { get; set; }
        TMediaExportFormat ExportFormat { get; set; }
        string ExportParams { get; set; }
        bool IsXDCAM { get; set; }
        bool IsWAN { get; set; }
        bool IsRecursive { get; set; }
        string Username { get; set; }
        string Password { get; set; }
        string[] Extensions { get; set; }
        TMediaCategory MediaCategory { get; set; }
        bool MediaDoNotArchive { get; set; }
        int MediaRetnentionDays { get; set; }
        TFieldOrder SourceFieldOrder { get; set; }
        TxDCAMAudioExportFormat XDCAMAudioExportFormat { get; set; }
        TxDCAMVideoExportFormat XDCAMVideoExportFormat { get; set; }
    }
}
