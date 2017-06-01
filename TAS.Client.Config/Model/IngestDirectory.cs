using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using TAS.Server.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Client.Config.Model
{
    public class IngestDirectory: IIngestDirectoryProperties
    {
        
        public IngestDirectory()
        {
            IsImport = true;
            VideoBitrateRatio = 1.0M;
            AudioBitrateRatio = 1.0M;
        }
        [DefaultValue(default(TAspectConversion))]
        public TAspectConversion AspectConversion { get; set; }
        [DefaultValue(typeof(Decimal), "0")]
        public decimal AudioVolume { get; set; }
        [DefaultValue(false)]
        public bool DeleteSource { get; set; }
        [DefaultValue(default(TIngestDirectoryKind))]
        public TIngestDirectoryKind Kind { get; set; }
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
        [DefaultValue(default(TmXFAudioExportFormat))]
        public TmXFAudioExportFormat MXFAudioExportFormat { get; set; }
        [DefaultValue(default(TmXFVideoExportFormat))]
        public TmXFVideoExportFormat MXFVideoExportFormat { get; set; }
        public string DirectoryName { get; set; }
        public string Folder { get; set; }
        [DefaultValue(default(string))]
        public string Username { get; set; }
        [DefaultValue(default(string))]
        public string Password { get; set; }
        [DefaultValue(default(string))]
        public string EncodeParams { get; set; }
        [DefaultValue(default(TMovieContainerFormat))]
        public TMovieContainerFormat ExportContainerFormat { get; set; }
        [DefaultValue(default(TVideoFormat))]
        public TVideoFormat ExportVideoFormat { get; set; }
        [DefaultValue(default(string))]
        public string ExportParams { get; set; }
        [XmlArray]
        [XmlArrayItem("Extension")]
        public string[] Extensions { get; set; }
        public TVideoCodec VideoCodec { get; set; }
        public TAudioCodec AudioCodec { get; set; }
        [DefaultValue(typeof(Decimal), "1")]
        public decimal VideoBitrateRatio { get; set; }
        [DefaultValue(typeof(Decimal), "1")]
        public decimal AudioBitrateRatio { get; set; }
        [XmlArray(nameof(SubDirectories))]
        public IngestDirectory[] _subDirectories = new IngestDirectory[0];
        [XmlIgnore]
        public IEnumerable<IIngestDirectoryProperties> SubDirectories { get { return _subDirectories; }  set { _subDirectories = value.Cast<IngestDirectory>().ToArray(); } }
    }
}
