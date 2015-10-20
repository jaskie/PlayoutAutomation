using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using TAS.Common;

namespace TAS.Client.Setup.Model
{
    public class IngestDirectory: TAS.Server.Interfaces.IIngestDirectoryConfig
    {
        public TAspectConversion AspectConversion { get; set; }
        public decimal AudioVolume { get; set; }
        public bool DeleteSource { get; set; }
        public bool IsXDCAM { get; set; }
        public bool IsWAN { get; set; }
        public TMediaCategory MediaCategory { get; set; }
        public bool MediaDoNotArchive { get; set; }
        public int MediaRetnentionDays { get; set; }
        public TFieldOrder SourceFieldOrder { get; set; }
        public TxDCAMAudioExportFormat XDCAMAudioExportFormat { get; set; }
        public TxDCAMVideoExportFormat XDCAMVideoExportFormat { get; set; }
        public string DirectoryName { get; set; }
        public string Folder { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string EncodeParams { get; set; }
        [XmlArray]
        [XmlArrayItem("Extension")]
        public string[] Extensions { get; set; }
    }
}
