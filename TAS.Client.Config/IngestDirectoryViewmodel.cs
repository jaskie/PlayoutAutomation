using System;
using TAS.Client.Common;
using TAS.Client.Config.Model;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.Config
{
    public class IngestDirectoryViewmodel: EditViewmodelBase<IngestDirectory>, IIngestDirectoryConfig
    {
        // only required by serializer
        public IngestDirectoryViewmodel(IngestDirectory model):base(model, new IngestDirectoryView())
        {
            Array.Copy(_aspectConversions, _aspectConversionsEnforce, 3);
        }
        
        #region Enumerations
        Array _aspectConversions = Enum.GetValues(typeof(TAspectConversion));
        public Array AspectConversions { get { return _aspectConversions; } }
        Array _aspectConversionsEnforce = new TAspectConversion[3];
        public Array AspectConversionsEnforce { get { return _aspectConversionsEnforce; } }
        Array _mediaCategories = Enum.GetValues(typeof(TMediaCategory));
        public Array MediaCategories { get { return _mediaCategories; } }
        Array _sourceFieldOrders = Enum.GetValues(typeof(TFieldOrder));
        public Array SourceFieldOrders { get { return _sourceFieldOrders; } }
        Array _xDCAMAudioExportFormats = Enum.GetValues(typeof(TxDCAMAudioExportFormat));
        public Array XDCAMAudioExportFormats { get { return _xDCAMAudioExportFormats; } }
        Array _xDCAMVideoExportFormats = Enum.GetValues(typeof(TxDCAMVideoExportFormat));
        public Array XDCAMVideoExportFormats { get { return _xDCAMVideoExportFormats; } }
        Array _exportContainerFormats = Enum.GetValues(typeof(TMediaExportContainerFormat));
        public Array ExportContainerFormats { get { return _exportContainerFormats; } }
        Array _exportVideoFormats = Enum.GetValues(typeof(TVideoFormat));
        public Array ExportVideoFormats { get { return _exportVideoFormats; } }
        #endregion // Enumerations


        #region IIngestDirectoryConfig
        string _directoryName;
        public string DirectoryName { get { return _directoryName; } set { SetField(ref _directoryName, value, "DirectoryName"); } }
        string _folder;
        public string Folder { get { return _folder; } set { SetField(ref _folder, value, "Folder"); } }
        string _username;
        public string Username { get { return _username; } set { SetField(ref _username, value, "Username"); } }
        string _password;
        public string Password { get { return _password; } set { SetField(ref _password, value, "Password"); } }
        TAspectConversion _aspectConversion;
        public TAspectConversion AspectConversion { get { return _aspectConversion; } set { SetField(ref _aspectConversion, value, "AspectConversion"); } }
        decimal _audioVolume;
        public decimal AudioVolume { get { return _audioVolume; } set { SetField(ref _audioVolume, value, "AudioVolume"); } }
        bool _deleteSource;
        public bool DeleteSource { get { return _deleteSource; } set { SetField(ref _deleteSource, value, "DeleteSource"); } }
        bool _isXDCAM;
        public bool IsXDCAM { get { return _isXDCAM; } set { SetField(ref _isXDCAM, value, "IsXDCAM"); } }
        bool _isWAN;
        public bool IsWAN { get { return _isWAN; } set { SetField(ref _isWAN, value, "IsWAN"); } }
        bool _isRecursive;
        public bool IsRecursive { get { return _isRecursive; }  set { SetField(ref _isRecursive, value, "IsRecursive"); } }
        bool _isExport;
        public bool IsExport { get { return _isExport; }  set { SetField(ref _isExport, value, "IsExport"); } }
        bool _isImport = true;
        public bool IsImport { get { return _isImport; } set { SetField(ref _isImport, value, "IsImport"); } }

        TMediaCategory _mediaCategory;
        public TMediaCategory MediaCategory { get { return _mediaCategory; } set { SetField(ref _mediaCategory, value, "MediaCategory"); } }
        bool _mediaDoNotArchive;
        public bool MediaDoNotArchive { get { return _mediaDoNotArchive; } set { SetField(ref _mediaDoNotArchive, value, "MediaDoNotArchive"); } }
        int _mediaRetentionDays;
        public int MediaRetnentionDays { get { return _mediaRetentionDays; } set { SetField(ref _mediaRetentionDays, value, "MediaRetnentionDays"); } }
        bool _mediaLoudnessCheckAfterIngest;
        public bool MediaLoudnessCheckAfterIngest { get { return _mediaLoudnessCheckAfterIngest; } set { SetField(ref _mediaLoudnessCheckAfterIngest, value, "MediaLoudnessCheckAfterIngest"); } }
        TFieldOrder _sourceFieldOrder;
        public TFieldOrder SourceFieldOrder { get { return _sourceFieldOrder; } set { SetField(ref _sourceFieldOrder, value, "SourceFieldOrder"); } }
        TxDCAMAudioExportFormat _xDCAMAudioExportFormat;
        public TxDCAMAudioExportFormat XDCAMAudioExportFormat { get { return _xDCAMAudioExportFormat; } set { SetField(ref _xDCAMAudioExportFormat, value, "XDCAMAudioExportFormat"); } }
        TxDCAMVideoExportFormat _xDCAMVideoExportFormat;
        public TxDCAMVideoExportFormat XDCAMVideoExportFormat { get { return _xDCAMVideoExportFormat; } set { SetField(ref _xDCAMVideoExportFormat, value, "XDCAMVideoExportFormat"); } }
        string _encodeParams;
        public string EncodeParams { get { return _encodeParams; } set { SetField(ref _encodeParams, value, "EncodeParams"); } }
        TMediaExportContainerFormat _exportFormat;
        public TMediaExportContainerFormat ExportContainerFormat { get { return _exportFormat; } set { SetField(ref _exportFormat, value, "ExportContainerFormat"); } }
        TVideoFormat _exportVideoFormat;
        public TVideoFormat ExportVideoFormat { get { return _exportVideoFormat; } set { SetField(ref _exportVideoFormat, value, "ExportVideoFormat"); } }
        bool _doNotEncode;
        public bool DoNotEncode { get { return _doNotEncode; } set { SetField(ref _doNotEncode, value, "DoNotEncode"); } }
        string _exportParams;
        public string ExportParams { get { return _exportParams; }  set { SetField(ref _exportParams, value, "ExportParams"); } }


        string[] _extensions;
        public string[] Extensions { get { return _extensions; } set { SetField(ref _extensions, value, "Extensions"); } }

        #endregion // IIngestDirectory
        
        protected override void OnDispose() { }

        public override string ToString()
        {
            return string.Format("{0} ({1})", DirectoryName, Folder);
        }
    }
}
