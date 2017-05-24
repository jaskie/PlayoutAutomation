using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TAS.Client.Common;
using TAS.Client.Config.Model;
using TAS.Server.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Client.Config
{
    public class IngestDirectoryViewmodel: EditViewmodelBase<IngestDirectory>, IIngestDirectoryProperties
    {
        // only required by serializer
        public IngestDirectoryViewmodel(IngestDirectory model, ObservableCollection<IngestDirectoryViewmodel> ownerCollection):base(model, new IngestDirectoryView())
        {
            Array.Copy(_aspectConversions, _aspectConversionsEnforce, 3);
            OwnerCollection = ownerCollection;
            _subDirectoriesVM = new System.Collections.ObjectModel.ObservableCollection<IngestDirectoryViewmodel>();
            foreach (var item in model._subDirectories.Select(s => new IngestDirectoryViewmodel(s, _subDirectoriesVM)))
                _subDirectoriesVM.Add(item);
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
        static readonly Array _exportContainerFormats = Enum.GetValues(typeof(TMovieContainerFormat));
        public Array ExportContainerFormats { get { return _exportContainerFormats; } }
        static readonly Array _exportVideoFormats = Enum.GetValues(typeof(TVideoFormat));
        public Array ExportVideoFormats { get { return _exportVideoFormats; } }
        static readonly Array _videoCodecs = Enum.GetValues(typeof(TVideoCodec));
        public Array VideoCodecs { get { return _videoCodecs; } }
        static readonly Array _audioCodecs = Enum.GetValues(typeof(TAudioCodec));
        public Array AudioCodecs { get { return _audioCodecs; } }
        #endregion // Enumerations


        #region IIngestDirectoryProperties
        string _directoryName;
        public string DirectoryName { get { return _directoryName; } set { SetField(ref _directoryName, value); } }
        string _folder;
        public string Folder { get { return _folder; } set { SetField(ref _folder, value); } }
        string _username;
        public string Username { get { return _username; } set { SetField(ref _username, value); } }
        string _password;
        public string Password { get { return _password; } set { SetField(ref _password, value); } }
        TAspectConversion _aspectConversion;
        public TAspectConversion AspectConversion { get { return _aspectConversion; } set { SetField(ref _aspectConversion, value); } }
        decimal _audioVolume;
        public decimal AudioVolume { get { return _audioVolume; } set { SetField(ref _audioVolume, value); } }
        bool _deleteSource;
        public bool DeleteSource { get { return _deleteSource; } set { SetField(ref _deleteSource, value); } }
        bool _isXDCAM;
        public bool IsXDCAM
        {
            get { return _isXDCAM; }
            set
            {
                if (SetField(ref _isXDCAM, value))
                    NotifyPropertyChanged(nameof(IsMXF));
            }
        }
        bool _isWAN;
        public bool IsWAN { get { return _isWAN; } set { SetField(ref _isWAN, value); } }
        bool _isRecursive;
        public bool IsRecursive { get { return _isRecursive; }  set { SetField(ref _isRecursive, value); } }
        bool _isExport;
        public bool IsExport { get { return _isExport; }  set { SetField(ref _isExport, value); } }
        bool _isImport = true;
        public bool IsImport { get { return _isImport; } set { SetField(ref _isImport, value); } }

        TMediaCategory _mediaCategory;
        public TMediaCategory MediaCategory { get { return _mediaCategory; } set { SetField(ref _mediaCategory, value); } }
        bool _mediaDoNotArchive;
        public bool MediaDoNotArchive { get { return _mediaDoNotArchive; } set { SetField(ref _mediaDoNotArchive, value); } }
        int _mediaRetentionDays;
        public int MediaRetnentionDays { get { return _mediaRetentionDays; } set { SetField(ref _mediaRetentionDays, value); } }
        bool _mediaLoudnessCheckAfterIngest;
        public bool MediaLoudnessCheckAfterIngest { get { return _mediaLoudnessCheckAfterIngest; } set { SetField(ref _mediaLoudnessCheckAfterIngest, value); } }
        TFieldOrder _sourceFieldOrder;
        public TFieldOrder SourceFieldOrder { get { return _sourceFieldOrder; } set { SetField(ref _sourceFieldOrder, value); } }
        TmXFAudioExportFormat _mXFAudioExportFormat;
        public TmXFAudioExportFormat MXFAudioExportFormat { get { return _mXFAudioExportFormat; } set { SetField(ref _mXFAudioExportFormat, value); } }
        TmXFVideoExportFormat _mXFVideoExportFormat;
        public TmXFVideoExportFormat MXFVideoExportFormat { get { return _mXFVideoExportFormat; } set { SetField(ref _mXFVideoExportFormat, value); } }
        string _encodeParams;
        public string EncodeParams { get { return _encodeParams; } set { SetField(ref _encodeParams, value); } }
        TMovieContainerFormat _exportFormat;
        public TMovieContainerFormat ExportContainerFormat
        {
            get { return _exportFormat; }
            set
            {
                if (SetField(ref _exportFormat, value))
                    NotifyPropertyChanged(nameof(IsMXF));
            }
        }
        TVideoFormat _exportVideoFormat;
        public TVideoFormat ExportVideoFormat { get { return _exportVideoFormat; } set { SetField(ref _exportVideoFormat, value); } }
        string _exportParams;
        public string ExportParams { get { return _exportParams; }  set { SetField(ref _exportParams, value); } }
        string[] _extensions;
        public string[] Extensions { get { return _extensions; } set { SetField(ref _extensions, value); } }

        TVideoCodec _videoCodec;
        public TVideoCodec VideoCodec
        {
            get { return _videoCodec; }
            set
            {
                if (SetField(ref _videoCodec, value))
                    NotifyPropertyChanged(nameof(VideoDoNotEncode));
            }
        }
        TAudioCodec _audioCodec;
        public TAudioCodec AudioCodec
        {
            get { return _audioCodec; }
            set
            {
                if (SetField(ref _audioCodec, value))
                    NotifyPropertyChanged(nameof(AudioDoNotEncode));
            }
        }

        decimal _videoBitrateRatio;
        decimal _audioBitrateRatio;
        public decimal VideoBitrateRatio { get { return _videoBitrateRatio; } set { SetField(ref _videoBitrateRatio, value); }}
        public decimal AudioBitrateRatio { get { return _audioBitrateRatio; } set { SetField(ref _audioBitrateRatio, value); } }

        public IEnumerable<IIngestDirectoryProperties> SubDirectories { get { return _subDirectoriesVM.Select(vm => vm.Model); } }

        #endregion // IIngestDirectoryProperties

        public bool IsMXF { get { return IsXDCAM || (!IsXDCAM && ExportContainerFormat == TMovieContainerFormat.mxf); } }
        public bool VideoDoNotEncode { get { return _videoCodec == TVideoCodec.copy; } }
        public bool AudioDoNotEncode { get { return _audioCodec == TAudioCodec.copy; } }
        private ObservableCollection<IngestDirectoryViewmodel> _subDirectoriesVM;
        public ObservableCollection<IngestDirectoryViewmodel> SubDirectoriesVM { get { return _subDirectoriesVM; } }
        public IngestDirectoryViewmodel AddSubdirectory()
        {
            var dir = new IngestDirectory() { DirectoryName = Common.Properties.Resources._title_NewDirectory };
            var dirVM = new IngestDirectoryViewmodel(dir, _subDirectoriesVM);
            _subDirectoriesVM.Add(dirVM);
            IsModified = true;
            return dirVM;
        }

        public ObservableCollection<IngestDirectoryViewmodel> OwnerCollection { get; private set; }

        protected override void OnDispose() { }

        public override void ModelUpdate(object destObject = null)
        {
            base.ModelUpdate(null);
            foreach (var vm in _subDirectoriesVM)
                vm.ModelUpdate(null);
            Model.SubDirectories = _subDirectoriesVM.Select(vm => vm.Model);
        }

        public override bool IsModified
        {
            get
            {
                return base.IsModified || _subDirectoriesVM.Any(d => d.IsModified);
            }

            protected set
            {
                base.IsModified = value;
            }
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", DirectoryName, Folder);
        }
    }
}
