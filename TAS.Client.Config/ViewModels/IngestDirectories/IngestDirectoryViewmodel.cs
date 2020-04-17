using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TAS.Client.Common;
using TAS.Client.Config.Model;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Client.Config.ViewModels.IngestDirectories
{
    public class IngestDirectoryViewmodel: EditViewmodelBase<IngestDirectory>, IIngestDirectoryProperties
    {

        private string _directoryName;
        private string _folder;
        private string _username;
        private string _password;
        private TAspectConversion _aspectConversion;
        private double _audioVolume;
        private bool _deleteSource;
        private TIngestDirectoryKind _kind;
        private bool _isWAN;
        private bool _isRecursive;
        private bool _isExport;
        private bool _isImport = true;
        private TMediaCategory _mediaCategory;
        private bool _mediaDoNotArchive;
        private int _mediaRetentionDays;
        private bool _mediaLoudnessCheckAfterIngest;
        private TFieldOrder _sourceFieldOrder;
        private TmXFAudioExportFormat _mXFAudioExportFormat;
        private TmXFVideoExportFormat _mXFVideoExportFormat;
        private string _encodeParams;
        private TMovieContainerFormat _exportFormat;
        private string _exportParams;
        private string[] _extensions;
        private TVideoCodec _videoCodec;
        private TAudioCodec _audioCodec;
        private double _videoBitrateRatio;
        private double _audioBitrateRatio;
        private TVideoFormat _exportVideoFormat;
        
        public IngestDirectoryViewmodel(IngestDirectory model, ModifyableViewModelBase owner):base(model)
        {
            Array.Copy(AspectConversions, AspectConversionsEnforce, 3);
            Owner = owner;
            SubDirectoriesVM = new ObservableCollection<IngestDirectoryViewmodel>(model.SubDirectoriesSerialized.Select(s => new IngestDirectoryViewmodel(s, this)));
        }
        
        #region Enumerations
        public Array AspectConversions { get; } = Enum.GetValues(typeof(TAspectConversion));
        public Array AspectConversionsEnforce { get; } = new TAspectConversion[3];
        public Array MediaCategories { get; } = Enum.GetValues(typeof(TMediaCategory));
        public Array SourceFieldOrders { get; } = Enum.GetValues(typeof(TFieldOrder));
        public Array MXFAudioExportFormats { get; } = Enum.GetValues(typeof(TmXFAudioExportFormat));
        public Array MXFVideoExportFormats { get; } = Enum.GetValues(typeof(TmXFVideoExportFormat));
        public Array ExportContainerFormats { get; } = Enum.GetValues(typeof(TMovieContainerFormat));
        public Array ExportVideoFormats { get; } = Enum.GetValues(typeof(TVideoFormat));
        public Array VideoCodecs { get; } = Enum.GetValues(typeof(TVideoCodec));
        public Array AudioCodecs { get; } = Enum.GetValues(typeof(TAudioCodec));
        public Array IngestDirectoryKinds { get; } = Enum.GetValues(typeof(TIngestDirectoryKind));
        #endregion // Enumerations
        
        #region IIngestDirectoryProperties

        public string DirectoryName
        {
            get => _directoryName;
            set => SetField(ref _directoryName, value);
        }

        public string Folder
        {
            get => _folder;
            set => SetField(ref _folder, value);
        }

        public string Username
        {
            get => _username;
            set => SetField(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetField(ref _password, value);
        }

        public TAspectConversion AspectConversion
        {
            get => _aspectConversion;
            set => SetField(ref _aspectConversion, value);
        }

        public double AudioVolume
        {
            get => _audioVolume;
            set => SetField(ref _audioVolume, value);
        }

        public bool DeleteSource
        {
            get => _deleteSource;
            set => SetField(ref _deleteSource, value);
        }

        public TIngestDirectoryKind Kind
        {
            get => _kind;
            set
            {
                if (SetField(ref _kind, value))
                    NotifyPropertyChanged(nameof(IsMxf));
            }
        }

        public bool IsWAN
        {
            get => _isWAN;
            set => SetField(ref _isWAN, value);
        }

        public bool IsRecursive
        {
            get => _isRecursive;
            set => SetField(ref _isRecursive, value);
        }

        public bool IsExport
        {
            get => _isExport;
            set => SetField(ref _isExport, value);
        }

        public bool IsImport
        {
            get => _isImport;
            set => SetField(ref _isImport, value);
        }

        public TMediaCategory MediaCategory
        {
            get => _mediaCategory;
            set => SetField(ref _mediaCategory, value);
        }

        public bool MediaDoNotArchive
        {
            get => _mediaDoNotArchive;
            set => SetField(ref _mediaDoNotArchive, value);
        }

        public int MediaRetnentionDays
        {
            get => _mediaRetentionDays;
            set => SetField(ref _mediaRetentionDays, value);
        }

        public bool MediaLoudnessCheckAfterIngest
        {
            get => _mediaLoudnessCheckAfterIngest;
            set => SetField(ref _mediaLoudnessCheckAfterIngest, value);
        }

        public TFieldOrder SourceFieldOrder
        {
            get => _sourceFieldOrder;
            set => SetField(ref _sourceFieldOrder, value);
        }

        public TmXFAudioExportFormat MXFAudioExportFormat
        {
            get => _mXFAudioExportFormat;
            set => SetField(ref _mXFAudioExportFormat, value);
        }

        public TmXFVideoExportFormat MXFVideoExportFormat
        {
            get => _mXFVideoExportFormat;
            set => SetField(ref _mXFVideoExportFormat, value);
        }

        public string EncodeParams
        {
            get => _encodeParams;
            set => SetField(ref _encodeParams, value);
        }

        public TMovieContainerFormat ExportContainerFormat
        {
            get => _exportFormat;
            set
            {
                if (!SetField(ref _exportFormat, value))
                    return;
                NotifyPropertyChanged(nameof(IsMxf));
                NotifyPropertyChanged(nameof(IsXdcam));
            }
        }

        public TVideoFormat ExportVideoFormat
        {
            get => _exportVideoFormat;
            set => SetField(ref _exportVideoFormat, value);
        }

        public string ExportParams
        {
            get => _exportParams;
            set => SetField(ref _exportParams, value);
        }

        public string[] Extensions
        {
            get => _extensions;
            set => SetField(ref _extensions, value);
        }

        public TVideoCodec VideoCodec
        {
            get => _videoCodec;
            set
            {
                if (SetField(ref _videoCodec, value))
                    NotifyPropertyChanged(nameof(VideoDoNotEncode));
            }
        }

        public TAudioCodec AudioCodec
        {
            get => _audioCodec;
            set
            {
                if (SetField(ref _audioCodec, value))
                    NotifyPropertyChanged(nameof(AudioDoNotEncode));
            }
        }

        public double VideoBitrateRatio
        {
            get => _videoBitrateRatio;
            set => SetField(ref _videoBitrateRatio, value);
        }

        public double AudioBitrateRatio
        {
            get => _audioBitrateRatio;
            set => SetField(ref _audioBitrateRatio, value);
        }

        public IEnumerable<IIngestDirectoryProperties> SubDirectories { get { return SubDirectoriesVM.Select(vm => vm.Model); } }

        #endregion // IIngestDirectoryProperties

        public bool IsMxf => Kind == TIngestDirectoryKind.XDCAM || (ExportContainerFormat == TMovieContainerFormat.mxf);

        public bool IsXdcam => Kind == TIngestDirectoryKind.XDCAM;
        
        public bool VideoDoNotEncode => _videoCodec == TVideoCodec.copy;

        public bool AudioDoNotEncode => _audioCodec == TAudioCodec.copy;

        protected override void Update(object destObject = null)
        {
            base.Update();
            foreach (var vm in SubDirectoriesVM)
                vm.Update();
            Model.SubDirectories = SubDirectoriesVM.Select(vm => vm.Model);
        }

        public override bool IsModified
        {
            get
            {
                return base.IsModified || SubDirectoriesVM.Any(d => d.IsModified);
            }
            set => base.IsModified = value;
        }

        public override string ToString()
        {
            return $"{DirectoryName} ({Folder})";
        }

        public ObservableCollection<IngestDirectoryViewmodel> SubDirectoriesVM { get; }

        public IngestDirectoryViewmodel AddSubdirectory()
        {
            var dir = new IngestDirectory { DirectoryName = Common.Properties.Resources._title_NewDirectory };
            var dirVm = new IngestDirectoryViewmodel(dir, this);
            SubDirectoriesVM.Add(dirVm);
            IsModified = true;
            return dirVm;
        }

        internal ModifyableViewModelBase Owner { get; }

        internal ObservableCollection<IngestDirectoryViewmodel> OwnerCollection => Owner is IngestDirectoriesViewmodel root
            ? root.Directories
            : ((IngestDirectoryViewmodel) Owner).SubDirectoriesVM;

        protected override void OnDispose() { }

        public void SaveToModel()
        {
            Update(Model);
        }
    }
}
