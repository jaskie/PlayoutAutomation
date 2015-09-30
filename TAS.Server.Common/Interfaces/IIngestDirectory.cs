using System;
using TAS.Common;

namespace TAS.Server.Interfaces
{
    public interface IIngestDirectory: IMediaDirectory
    {
        TAspectConversion AspectConversion { get; set; }
        decimal AudioVolume { get; set; }
        bool DeleteSource { get; set; }
        string EncodeParams { get; set; }
        bool IsXDCAM { get; set; }
        TMediaCategory MediaCategory { get; set; }
        bool MediaDoNotArchive { get; set; }
        int MediaRetnentionDays { get; set; }
        TFieldOrder SourceFieldOrder { get; set; }
        TxDCAMAudioExportFormat XDCAMAudioExportFormat { get; set; }
        TxDCAMVideoExportFormat XDCAMVideoExportFormat { get; set; }
    }
}
