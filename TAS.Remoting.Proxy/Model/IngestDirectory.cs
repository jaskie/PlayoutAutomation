using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using TAS.Server.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class IngestDirectory : MediaDirectory, IIngestDirectory
    {
        public bool DoNotEncode { get { return Get<bool>(); } set { SetLocalValue(value); } }
        public TAspectConversion AspectConversion { get { return Get<TAspectConversion>(); } set { SetLocalValue(value); } }
        public decimal AudioVolume { get { return Get<decimal>(); } set { SetLocalValue(value); } }
        public bool DeleteSource { get { return Get<bool>(); } set { SetLocalValue(value); } }
        public string EncodeParams { get { return Get<string>(); } set { SetLocalValue(value); } }
        public string ExportParams { get { return Get<string>(); } set { SetLocalValue(value); } }
        public string Filter { get { return Get<string>(); } set { Set(value); } }
        public bool IsWAN { get { return Get<bool>(); } set { SetLocalValue(value); } }
        public TIngestDirectoryKind Kind { get { return Get<TIngestDirectoryKind>(); } set { SetLocalValue(value); } }
        public bool IsRecursive { get { return Get<bool>(); } set { SetLocalValue(value); } }
        public bool IsExport { get { return Get<bool>(); } set { SetLocalValue(value); } }
        public bool IsImport { get { return Get<bool>(); } set { SetLocalValue(value); } }
        public TMediaCategory MediaCategory { get { return Get<TMediaCategory>(); } set { SetLocalValue(value); } }
        public bool MediaDoNotArchive { get { return Get<bool>(); } set { SetLocalValue(value); } }
        public int MediaRetnentionDays { get { return Get<int>(); } set { SetLocalValue(value); } }
        public bool MediaLoudnessCheckAfterIngest { get { return Get<bool>(); }  set { Set(value); } }
        public TFieldOrder SourceFieldOrder { get { return Get<TFieldOrder>(); } set { SetLocalValue(value); } }
        public TmXFAudioExportFormat MXFAudioExportFormat { get; set; }
        public TmXFVideoExportFormat MXFVideoExportFormat { get; set; }
        public TMovieContainerFormat ExportContainerFormat { get; set; }
        public TVideoFormat ExportVideoFormat { get; set; }
        public TVideoCodec VideoCodec { get; set; }
        public TAudioCodec AudioCodec { get; set; }
        public decimal VideoBitrateRatio { get; set; }
        public decimal AudioBitrateRatio { get; set; }
        public string[] Extensions { get; set; }
        public NetworkCredential NetworkCredential => null;
        public string Password { get; set; }
        public string Username { get; set; }
        public int XdcamClipCount { get; set; }

        [JsonProperty(nameof(SubDirectories))]
        public List<IngestDirectory> _subDirectories;
        [JsonIgnore]
        public IEnumerable<IIngestDirectoryProperties> SubDirectories { get { return _subDirectories; } }

        public override IMedia CreateMedia(IMediaProperties mediaProperties)
        {
            throw new NotImplementedException();
        }
    }
}
