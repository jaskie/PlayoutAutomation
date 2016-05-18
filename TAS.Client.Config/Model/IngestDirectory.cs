using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using TAS.Common;

namespace TAS.Client.Config.Model
{
    public class IngestDirectory: TAS.Server.Interfaces.IIngestDirectoryConfig
    {
        public IngestDirectory()
        {
            IsImport = true;
        }
        [DefaultValue(default(TAspectConversion))]
        public TAspectConversion AspectConversion { get; set; }
        [DefaultValue(0)]
        public decimal AudioVolume { get; set; }
        [DefaultValue(false)]
        public bool DeleteSource { get; set; }
        [DefaultValue(false)]
        public bool IsXDCAM { get; set; }
        [DefaultValue(false)]
        public bool IsWAN { get; set; }
        [DefaultValue(false)]
        public bool IsRecursive { get; set; }
        [DefaultValue(false)]
        public bool IsExport { get; set; }
        [DefaultValue(true)]
        public bool IsImport { get; set; }
        [DefaultValue(false)]
        public bool DoNotEncode { get; set; }
        [DefaultValue(default(TMediaCategory))]
        public TMediaCategory MediaCategory { get; set; }
        [DefaultValue(false)]
        public bool MediaDoNotArchive { get; set; }
        [DefaultValue(default(int))]
        public int MediaRetnentionDays { get; set; }
        [DefaultValue(false)]
        public bool MediaLoudnessCheckAfterIngest { get; set; }
        [DefaultValue(default(TFieldOrder))]
        public TFieldOrder SourceFieldOrder { get; set; }
        [DefaultValue(default(TxDCAMAudioExportFormat))]
        public TxDCAMAudioExportFormat XDCAMAudioExportFormat { get; set; }
        [DefaultValue(default(TxDCAMVideoExportFormat))]
        public TxDCAMVideoExportFormat XDCAMVideoExportFormat { get; set; }
        public string DirectoryName { get; set; }
        public string Folder { get; set; }
        [DefaultValue(default(string))]
        public string Username { get; set; }
        [DefaultValue(default(string))]
        public string Password { get; set; }
        [DefaultValue(default(string))]
        public string EncodeParams { get; set; }
        [DefaultValue(default(TMediaExportContainerFormat))]
        public TMediaExportContainerFormat ExportContainerFormat { get; set; }
        [DefaultValue(default(TVideoFormat))]
        public TVideoFormat ExportVideoFormat { get; set; }
        [DefaultValue(default(string))]
        public string ExportParams { get; set; }
        [XmlArray]
        [XmlArrayItem("Extension")]
        public string[] Extensions { get; set; }
    }
}
