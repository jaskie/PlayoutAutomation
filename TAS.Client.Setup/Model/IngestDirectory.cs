using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Client.Setup.Model
{
    public class IngestDirectory: TAS.Server.Interfaces.IIngestDirectory
    {
        public TAS.Common.TAspectConversion AspectConversion { get; set; }
        public double AudioVolume { get; set; }
        public bool DeleteSource { get; set; }
        public bool IsXDCAM { get; set; }
        public TAS.Common.TMediaCategory MediaCategory { get; set; }
        public bool MediaDoNotArchive { get; set; }
        public int MediaRetnentionDays { get; set; }
        public TAS.Common.TFieldOrder SourceFieldOrder { get; set; }
        public TAS.Common.TxDCAMAudioExportFormat XDCAMAudioExportFormat { get; set; }
        public TAS.Common.TxDCAMVideoExportFormat XDCAMVideoExportFormat { get; set; }
        public string DirectoryName { get; set; }
        public string Folder { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string EncodeParams { get; set; }
    }
}
