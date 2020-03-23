using jNet.RPC;
using System.Collections.Generic;
using TAS.Common;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model.Media
{
    public class IngestDirectory : WatcherDirectory, IIngestDirectory
    {
        #pragma warning disable CS0649

        [DtoField(nameof(IIngestDirectory.DirectoryName))]
        private string _directoryName;

        [DtoField(nameof(IIngestDirectory.AccessType))]
        private TDirectoryAccessType _accessType;

        [DtoField(nameof(IIngestDirectory.AspectConversion))]
        private TAspectConversion _aspectConversion;

        [DtoField(nameof(IIngestDirectory.AudioVolume))]
        private double _audioVolume;

        [DtoField(nameof(IIngestDirectory.DeleteSource))]
        private bool _deleteSource;

        [DtoField(nameof(IIngestDirectory.EncodeParams))]
        private string _encodeParams;

        [DtoField(nameof(IIngestDirectory.ExportParams))]
        private string _exportParams;

        [DtoField(nameof(IIngestDirectory.IsWAN))]
        private bool _isWan;

        [DtoField(nameof(IIngestDirectory.Kind))]
        private TIngestDirectoryKind _kind;

        [DtoField(nameof(IIngestDirectory.IsRecursive))]
        private bool _isRecursive;

        [DtoField(nameof(IIngestDirectory.IsExport))]
        private bool _isExport;

        [DtoField(nameof(IIngestDirectory.IsImport))]
        private bool _isImport;

        [DtoField(nameof(IIngestDirectory.MediaCategory))]
        private TMediaCategory _mediaCategory;

        [DtoField(nameof(IIngestDirectory.MediaDoNotArchive))]
        private bool _mediaDoNotArchive;

        [DtoField(nameof(IIngestDirectory.MediaRetnentionDays))]
        private int _mediaRetnentionDays;

        [DtoField(nameof(IIngestDirectory.MediaLoudnessCheckAfterIngest))]
        private bool _mediaLoudnessCheckAfterIngest;

        [DtoField(nameof(IIngestDirectory.SourceFieldOrder))]
        private TFieldOrder _sourceFieldOrder;

        [DtoField(nameof(IIngestDirectory.MXFAudioExportFormat))]
        private TmXFAudioExportFormat _mxfAudioExportFormat;

        [DtoField(nameof(IIngestDirectory.MXFVideoExportFormat))]
        private TmXFVideoExportFormat _mxfVideoExportFormat;

        [DtoField(nameof(IIngestDirectory.ExportContainerFormat))]
        private TMovieContainerFormat _exportContainerFormat;

        [DtoField(nameof(IIngestDirectory.ExportVideoFormat))]
        private TVideoFormat _exportVideoFormat;

        [DtoField(nameof(IIngestDirectory.VideoCodec))]
        private TVideoCodec _videoCodec;

        [DtoField(nameof(IIngestDirectory.AudioCodec))]
        private TAudioCodec _audioCodec;

        [DtoField(nameof(IIngestDirectory.VideoBitrateRatio))]
        private double _videoBitrateRatio;

        [DtoField(nameof(IIngestDirectory.AudioBitrateRatio))]
        private double _audioBitrateRatio;

        [DtoField(nameof(IIngestDirectory.Extensions))]
        private string[] _extensions;

        [DtoField(nameof(IIngestDirectory.Password))]
        private string _password;

        [DtoField(nameof(IIngestDirectory.Username))]
        private string _username;

        [DtoField(nameof(IIngestDirectory.XdcamClipCount))]
        private int _xdcamClipCount;

        [DtoField(nameof(SubDirectories))]
        private List<IngestDirectory> _subDirectories;

        #pragma warning restore

        public string DirectoryName { get => _directoryName; set => Set(value); }

        public TDirectoryAccessType AccessType => _accessType;

        public TAspectConversion AspectConversion => _aspectConversion;

        public double AudioVolume => _audioVolume;

        public bool DeleteSource => _deleteSource;

        public string EncodeParams => _encodeParams;

        public string ExportParams => _exportParams;

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
    }
}
