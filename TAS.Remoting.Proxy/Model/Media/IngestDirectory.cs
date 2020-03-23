using jNet.RPC;
using System.Collections.Generic;
using TAS.Common;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Remoting.Model.Media
{
    public class IngestDirectory : WatcherDirectory, IIngestDirectory
    {
        #pragma warning disable CS0649

        [DtoMember(nameof(IIngestDirectory.DirectoryName))]
        private string _directoryName;

        [DtoMember(nameof(IIngestDirectory.AccessType))]
        private TDirectoryAccessType _accessType;

        [DtoMember(nameof(IIngestDirectory.AspectConversion))]
        private TAspectConversion _aspectConversion;

        [DtoMember(nameof(IIngestDirectory.AudioVolume))]
        private double _audioVolume;

        [DtoMember(nameof(IIngestDirectory.DeleteSource))]
        private bool _deleteSource;

        [DtoMember(nameof(IIngestDirectory.EncodeParams))]
        private string _encodeParams;

        [DtoMember(nameof(IIngestDirectory.ExportParams))]
        private string _exportParams;

        [DtoMember(nameof(IIngestDirectory.IsWAN))]
        private bool _isWan;

        [DtoMember(nameof(IIngestDirectory.Kind))]
        private TIngestDirectoryKind _kind;

        [DtoMember(nameof(IIngestDirectory.IsRecursive))]
        private bool _isRecursive;

        [DtoMember(nameof(IIngestDirectory.IsExport))]
        private bool _isExport;

        [DtoMember(nameof(IIngestDirectory.IsImport))]
        private bool _isImport;

        [DtoMember(nameof(IIngestDirectory.MediaCategory))]
        private TMediaCategory _mediaCategory;

        [DtoMember(nameof(IIngestDirectory.MediaDoNotArchive))]
        private bool _mediaDoNotArchive;

        [DtoMember(nameof(IIngestDirectory.MediaRetnentionDays))]
        private int _mediaRetnentionDays;

        [DtoMember(nameof(IIngestDirectory.MediaLoudnessCheckAfterIngest))]
        private bool _mediaLoudnessCheckAfterIngest;

        [DtoMember(nameof(IIngestDirectory.SourceFieldOrder))]
        private TFieldOrder _sourceFieldOrder;

        [DtoMember(nameof(IIngestDirectory.MXFAudioExportFormat))]
        private TmXFAudioExportFormat _mxfAudioExportFormat;

        [DtoMember(nameof(IIngestDirectory.MXFVideoExportFormat))]
        private TmXFVideoExportFormat _mxfVideoExportFormat;

        [DtoMember(nameof(IIngestDirectory.ExportContainerFormat))]
        private TMovieContainerFormat _exportContainerFormat;

        [DtoMember(nameof(IIngestDirectory.ExportVideoFormat))]
        private TVideoFormat _exportVideoFormat;

        [DtoMember(nameof(IIngestDirectory.VideoCodec))]
        private TVideoCodec _videoCodec;

        [DtoMember(nameof(IIngestDirectory.AudioCodec))]
        private TAudioCodec _audioCodec;

        [DtoMember(nameof(IIngestDirectory.VideoBitrateRatio))]
        private double _videoBitrateRatio;

        [DtoMember(nameof(IIngestDirectory.AudioBitrateRatio))]
        private double _audioBitrateRatio;

        [DtoMember(nameof(IIngestDirectory.Extensions))]
        private string[] _extensions;

        [DtoMember(nameof(IIngestDirectory.Password))]
        private string _password;

        [DtoMember(nameof(IIngestDirectory.Username))]
        private string _username;

        [DtoMember(nameof(IIngestDirectory.XdcamClipCount))]
        private int _xdcamClipCount;

        [DtoMember(nameof(SubDirectories))]
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
