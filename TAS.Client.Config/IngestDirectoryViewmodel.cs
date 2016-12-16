using System;
using TAS.Client.Common;
using TAS.Client.Config.Model;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.Config
{
    public class IngestDirectoryViewmodel: EditViewmodelBase<IngestDirectory>, IIngestDirectoryProperties
    {
        // only required by serializer
        public IngestDirectoryViewmodel(IngestDirectory model):base(model, new IngestDirectoryView())
        {
            Array.Copy(_aspectConversions, _aspectConversionsEnforce, 3);
        }
        
        #region Enumerations
        static readonly Array _aspectConversions = Enum.GetValues(typeof(TAspectConversion));
        public Array AspectConversions { get { return _aspectConversions; } }
        static readonly Array _aspectConversionsEnforce = new TAspectConversion[3];
        public Array AspectConversionsEnforce { get { return _aspectConversionsEnforce; } }
        static readonly Array _mediaCategories = Enum.GetValues(typeof(TMediaCategory));
        public Array MediaCategories { get { return _mediaCategories; } }
        static readonly Array _sourceFieldOrders = Enum.GetValues(typeof(TFieldOrder));
        public Array SourceFieldOrders { get { return _sourceFieldOrders; } }
        static readonly Array _mXFAudioExportFormats = Enum.GetValues(typeof(TmXFAudioExportFormat));
        public Array MXFAudioExportFormats { get { return _mXFAudioExportFormats; } }
        static readonly Array _mXFVideoExportFormats = Enum.GetValues(typeof(TmXFVideoExportFormat));
        public Array MXFVideoExportFormats { get { return _mXFVideoExportFormats; } }
        static readonly Array _exportContainerFormats = Enum.GetValues(typeof(TMediaExportContainerFormat));
        public Array ExportContainerFormats { get { return _exportContainerFormats; } }
        static readonly Array _exportVideoFormats = Enum.GetValues(typeof(TVideoFormat));
        public Array ExportVideoFormats { get { return _exportVideoFormats; } }
        static readonly Array _videoCodecs = Enum.GetValues(typeof(TVideoCodec));
        public Array VideoCodecs { get { return _videoCodecs; } }
        static readonly Array _audioCodecs = Enum.GetValues(typeof(TAudioCodec));
        public Array AudioCodecs { get { return _audioCodecs; } }
        #endregion // Enumerations


        #region IIngestDirectoryConfig
        string _directoryName;
        public string DirectoryName { get { return _directoryName; } set { SetField(ref _directoryName, value, nameof(DirectoryName)); } }
        string _folder;
        public string Folder { get { return _folder; } set { SetField(ref _folder, value, nameof(Folder)); } }
        string _username;
        public string Username { get { return _username; } set { SetField(ref _username, value, nameof(Username)); } }
        string _password;
        public string Password { get { return _password; } set { SetField(ref _password, value, nameof(Password)); } }
        TAspectConversion _aspectConversion;
        public TAspectConversion AspectConversion { get { return _aspectConversion; } set { SetField(ref _aspectConversion, value, nameof(AspectConversion)); } }
        decimal _audioVolume;
        public decimal AudioVolume { get { return _audioVolume; } set { SetField(ref _audioVolume, value, nameof(AudioVolume)); } }
        bool _deleteSource;
        public bool DeleteSource { get { return _deleteSource; } set { SetField(ref _deleteSource, value, nameof(DeleteSource)); } }
        bool _isXDCAM;
        public bool IsXDCAM
        {
            get { return _isXDCAM; }
            set
            {
                if (SetField(ref _isXDCAM, value, nameof(IsXDCAM)))
                    NotifyPropertyChanged(nameof(IsMXF));
            }
        }
        bool _isWAN;
        public bool IsWAN { get { return _isWAN; } set { SetField(ref _isWAN, value, nameof(IsWAN)); } }
        bool _isRecursive;
        public bool IsRecursive { get { return _isRecursive; }  set { SetField(ref _isRecursive, value, nameof(IsRecursive)); } }
        bool _isExport;
        public bool IsExport { get { return _isExport; }  set { SetField(ref _isExport, value, nameof(IsExport)); } }
        bool _isImport = true;
        public bool IsImport { get { return _isImport; } set { SetField(ref _isImport, value, nameof(IsImport)); } }

        TMediaCategory _mediaCategory;
        public TMediaCategory MediaCategory { get { return _mediaCategory; } set { SetField(ref _mediaCategory, value, nameof(MediaCategory)); } }
        bool _mediaDoNotArchive;
        public bool MediaDoNotArchive { get { return _mediaDoNotArchive; } set { SetField(ref _mediaDoNotArchive, value, nameof(MediaDoNotArchive)); } }
        int _mediaRetentionDays;
        public int MediaRetnentionDays { get { return _mediaRetentionDays; } set { SetField(ref _mediaRetentionDays, value, nameof(MediaRetnentionDays)); } }
        bool _mediaLoudnessCheckAfterIngest;
        public bool MediaLoudnessCheckAfterIngest { get { return _mediaLoudnessCheckAfterIngest; } set { SetField(ref _mediaLoudnessCheckAfterIngest, value, nameof(MediaLoudnessCheckAfterIngest)); } }
        TFieldOrder _sourceFieldOrder;
        public TFieldOrder SourceFieldOrder { get { return _sourceFieldOrder; } set { SetField(ref _sourceFieldOrder, value, nameof(SourceFieldOrder)); } }
        TmXFAudioExportFormat _mXFAudioExportFormat;
        public TmXFAudioExportFormat MXFAudioExportFormat { get { return _mXFAudioExportFormat; } set { SetField(ref _mXFAudioExportFormat, value, nameof(MXFAudioExportFormat)); } }
        TmXFVideoExportFormat _mXFVideoExportFormat;
        public TmXFVideoExportFormat MXFVideoExportFormat { get { return _mXFVideoExportFormat; } set { SetField(ref _mXFVideoExportFormat, value, nameof(MXFVideoExportFormat)); } }
        string _encodeParams;
        public string EncodeParams { get { return _encodeParams; } set { SetField(ref _encodeParams, value, nameof(EncodeParams)); } }
        TMediaExportContainerFormat _exportFormat;
        public TMediaExportContainerFormat ExportContainerFormat
        {
            get { return _exportFormat; }
            set
            {
                if (SetField(ref _exportFormat, value, nameof(ExportContainerFormat)))
                    NotifyPropertyChanged(nameof(IsMXF));
            }
        }
        TVideoFormat _exportVideoFormat;
        public TVideoFormat ExportVideoFormat { get { return _exportVideoFormat; } set { SetField(ref _exportVideoFormat, value, nameof(ExportVideoFormat)); } }
        string _exportParams;
        public string ExportParams { get { return _exportParams; }  set { SetField(ref _exportParams, value, nameof(ExportParams)); } }
        string[] _extensions;
        public string[] Extensions { get { return _extensions; } set { SetField(ref _extensions, value, nameof(Extensions)); } }

        TVideoCodec _videoCodec;
        public TVideoCodec VideoCodec
        {
            get { return _videoCodec; }
            set
            {
                if (SetField(ref _videoCodec, value, nameof(VideoCodec)))
                    NotifyPropertyChanged(nameof(VideoDoNotEncode));
            }
        }
        TAudioCodec _audioCodec;
        public TAudioCodec AudioCodec
        {
            get { return _audioCodec; }
            set
            {
                if (SetField(ref _audioCodec, value, nameof(AudioCodec)))
                    NotifyPropertyChanged(nameof(AudioDoNotEncode));
            }
        }

        decimal _videoBitrateRatio;
        decimal _audioBitrateRatio;
        public decimal VideoBitrateRatio { get { return _videoBitrateRatio; } set { SetField(ref _videoBitrateRatio, value, nameof(VideoBitrateRatio)); }}
        public decimal AudioBitrateRatio { get { return _audioBitrateRatio; } set { SetField(ref _audioBitrateRatio, value, nameof(AudioBitrateRatio)); } }


        #endregion // IIngestDirectory

        public bool IsMXF { get { return IsXDCAM || (!IsXDCAM && ExportContainerFormat == TMediaExportContainerFormat.mxf); } }
        public bool VideoDoNotEncode { get { return _videoCodec == TVideoCodec.copy; } }
        public bool AudioDoNotEncode { get { return _audioCodec == TAudioCodec.copy; } }

        protected override void OnDispose() { }

        public override string ToString()
        {
            return string.Format("{0} ({1})", DirectoryName, Folder);
        }
    }
}
