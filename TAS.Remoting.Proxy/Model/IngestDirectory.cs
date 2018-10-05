using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model
{
    public class IngestDirectory : WatcherDirectory, IIngestDirectory
    {
        #pragma warning disable CS0649

        [JsonProperty(nameof(IIngestDirectory.AccessType))]
        private TDirectoryAccessType _accessType;

        [JsonProperty(nameof(IIngestDirectory.AspectConversion))]
        private TAspectConversion _aspectConversion;

        [JsonProperty(nameof(IIngestDirectory.AudioVolume))]
        private double _audioVolume;

        [JsonProperty(nameof(IIngestDirectory.DeleteSource))]
        private bool _deleteSource;

        [JsonProperty(nameof(IIngestDirectory.EncodeParams))]
        private string _encodeParams;

        [JsonProperty(nameof(IIngestDirectory.ExportParams))]
        private string _exportParams;

        [JsonProperty(nameof(IIngestDirectory.Filter))]
        private string _filter;

        [JsonProperty(nameof(IIngestDirectory.IsWAN))]
        private bool _isWan;

        [JsonProperty(nameof(IIngestDirectory.Kind))]
        private TIngestDirectoryKind _kind;

        [JsonProperty(nameof(IIngestDirectory.IsRecursive))]
        private bool _isRecursive;

        [JsonProperty(nameof(IIngestDirectory.IsExport))]
        private bool _isExport;

        [JsonProperty(nameof(IIngestDirectory.IsImport))]
        private bool _isImport;

        [JsonProperty(nameof(IIngestDirectory.MediaCategory))]
        private TMediaCategory _mediaCategory;

        [JsonProperty(nameof(IIngestDirectory.MediaDoNotArchive))]
        private bool _mediaDoNotArchive;

        [JsonProperty(nameof(IIngestDirectory.MediaRetnentionDays))]
        private int _mediaRetnentionDays;

        [JsonProperty(nameof(IIngestDirectory.MediaLoudnessCheckAfterIngest))]
        private bool _mediaLoudnessCheckAfterIngest;

        [JsonProperty(nameof(IIngestDirectory.SourceFieldOrder))]
        private TFieldOrder _sourceFieldOrder;

        [JsonProperty(nameof(IIngestDirectory.MXFAudioExportFormat))]
        private TmXFAudioExportFormat _mxfAudioExportFormat;

        [JsonProperty(nameof(IIngestDirectory.MXFVideoExportFormat))]
        private TmXFVideoExportFormat _mxfVideoExportFormat;

        [JsonProperty(nameof(IIngestDirectory.ExportContainerFormat))]
        private TMovieContainerFormat _exportContainerFormat;

        [JsonProperty(nameof(IIngestDirectory.ExportVideoFormat))]
        private TVideoFormat _exportVideoFormat;

        [JsonProperty(nameof(IIngestDirectory.VideoCodec))]
        private TVideoCodec _videoCodec;

        [JsonProperty(nameof(IIngestDirectory.AudioCodec))]
        private TAudioCodec _audioCodec;

        [JsonProperty(nameof(IIngestDirectory.VideoBitrateRatio))]
        private double _videoBitrateRatio;

        [JsonProperty(nameof(IIngestDirectory.AudioBitrateRatio))]
        private double _audioBitrateRatio;

        [JsonProperty(nameof(IIngestDirectory.Extensions))]
        private string[] _extensions;

        [JsonProperty(nameof(IIngestDirectory.Password))]
        private string _password;

        [JsonProperty(nameof(IIngestDirectory.Username))]
        private string _username;

        [JsonProperty(nameof(IIngestDirectory.XdcamClipCount))]
        private int _xdcamClipCount;

        [JsonProperty(nameof(SubDirectories))]
        private List<IngestDirectory> _subDirectories;

        #pragma warning restore

        public TDirectoryAccessType AccessType => _accessType;

        public TAspectConversion AspectConversion => _aspectConversion;

        public double AudioVolume => _audioVolume;

        public bool DeleteSource => _deleteSource;

        public string EncodeParams => _encodeParams;

        public string ExportParams => _exportParams;

        public string Filter
        {
            get => _filter;
            set => Set(value);
        }

        public bool IsWAN => _isWan;

        public TIngestDirectoryKind Kind => _kind;

        public bool IsRecursive => _isRecursive;

        public bool IsExport => _isExport;

        public bool IsImport => _isImport;

        public TMediaCategory MediaCategory => _mediaCategory;

        public bool MediaDoNotArchive => _mediaDoNotArchive;

        public int MediaRetnentionDays => _mediaRetnentionDays;

        public bool MediaLoudnessCheckAfterIngest => _mediaLoudnessCheckAfterIngest;

        public TFieldOrder SourceFieldOrder => _sourceFieldOrder;

        public TmXFAudioExportFormat MXFAudioExportFormat => _mxfAudioExportFormat;

        public TmXFVideoExportFormat MXFVideoExportFormat => _mxfVideoExportFormat;

        public TMovieContainerFormat ExportContainerFormat => _exportContainerFormat;

        public TVideoFormat ExportVideoFormat => _exportVideoFormat;

        public TVideoCodec VideoCodec => _videoCodec;

        public TAudioCodec AudioCodec => _audioCodec;

        public double VideoBitrateRatio => _videoBitrateRatio;

        public double AudioBitrateRatio => _audioBitrateRatio;

        public string[] Extensions => _extensions;

        public string Password => _password;

        public string Username => _username;

        public int XdcamClipCount => _xdcamClipCount;

        public IEnumerable<IIngestDirectoryProperties> SubDirectories => _subDirectories;

        public override IEnumerable<IMedia> GetFiles()
        {
            return Query<List<IngestMedia>>();
        }

        public override IMedia CreateMedia(IMediaProperties mediaProperties)
        {
            throw new NotImplementedException();
        }
    }
}
