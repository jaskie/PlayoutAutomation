using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Client.Config.Model
{
    public class IngestDirectory: IIngestDirectoryProperties
    {
        
        public IngestDirectory()
        {
            IsImport = true;
            VideoBitrateRatio = 1;
            AudioBitrateRatio = 1;
        }
        [DefaultValue(default(TAspectConversion))]
        public TAspectConversion AspectConversion { get; set; }
        [DefaultValue(0.0)]
        public double AudioVolume { get; set; }
        [DefaultValue(false)]
        public bool DeleteSource { get; set; }
        [DefaultValue(TIngestDirectoryKind.WatchFolder)]
        public TIngestDirectoryKind Kind { get; set; } = TIngestDirectoryKind.WatchFolder;
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
        [DefaultValue(1.0)]
        public double VideoBitrateRatio { get; set; }
        [DefaultValue(1.0)]
        public double AudioBitrateRatio { get; set; }
        [XmlArray(nameof(SubDirectories))]
        public IngestDirectory[] SubDirectoriesSerialized = new IngestDirectory[0];
        [XmlIgnore]
        public IEnumerable<IIngestDirectoryProperties> SubDirectories { get => SubDirectoriesSerialized; set => SubDirectoriesSerialized = value.Cast<IngestDirectory>().ToArray(); }
    }
}
