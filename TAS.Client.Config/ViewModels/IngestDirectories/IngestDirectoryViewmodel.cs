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
    public class IngestDirectoryViewModel : OkCancelViewModelBase
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
        private IngestDirectory _ingestDirectory;
        
        public IngestDirectoryViewModel(IngestDirectory model, ModifyableViewModelBase owner)
        {
            _ingestDirectory = model;
            
            Array.Copy(AspectConversions, AspectConversionsEnforce, 3);
            Owner = owner;
            SubDirectoriesVM = new ObservableCollection<IngestDirectoryViewModel>(model.SubDirectoriesSerialized.Select(s => new IngestDirectoryViewModel(s, this)));
            Init();
        }

        private void Init()
        {
            AspectConversion = _ingestDirectory.AspectConversion;
            AudioBitrateRatio = _ingestDirectory.AudioBitrateRatio;
            AudioCodec = _ingestDirectory.AudioCodec;
            AudioVolume = _ingestDirectory.AudioVolume;
            DeleteSource = _ingestDirectory.DeleteSource;
            DirectoryName = _ingestDirectory.DirectoryName;
            MediaDoNotArchive = _ingestDirectory.MediaDoNotArchive;
            EncodeParams = _ingestDirectory.EncodeParams;
            ExportContainerFormat = _ingestDirectory.ExportContainerFormat;
            ExportParams = _ingestDirectory.ExportParams;
            ExportVideoFormat = _ingestDirectory.ExportVideoFormat;
            Extensions = _ingestDirectory.Extensions;
            Folder = _ingestDirectory.Folder;
            IsExport = _ingestDirectory.IsExport;
            IsImport = _ingestDirectory.IsImport;
            IsRecursive = _ingestDirectory.IsRecursive;
            IsWAN = _ingestDirectory.IsWAN;
            Kind = _ingestDirectory.Kind;
            MediaCategory = _ingestDirectory.MediaCategory;
            MediaLoudnessCheckAfterIngest = _ingestDirectory.MediaLoudnessCheckAfterIngest;
            MediaRetnentionDays = _ingestDirectory.MediaRetnentionDays;
            MXFAudioExportFormat = _ingestDirectory.MXFAudioExportFormat;
            MXFVideoExportFormat = _ingestDirectory.MXFVideoExportFormat;
            Password = _ingestDirectory.Password;
            SourceFieldOrder = _ingestDirectory.SourceFieldOrder;            
            Username = _ingestDirectory.Username;
            VideoBitrateRatio = _ingestDirectory.VideoBitrateRatio;
            VideoCodec = _ingestDirectory.VideoCodec;
            IsModified = false;
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

        public IEnumerable<IIngestDirectoryProperties> SubDirectories { get { return SubDirectoriesVM.Select(vm => vm.IngestDirectory); } }

        #endregion // IIngestDirectoryProperties

        public bool IsMxf => Kind == TIngestDirectoryKind.XDCAM || (ExportContainerFormat == TMovieContainerFormat.mxf);

        public bool IsXdcam => Kind == TIngestDirectoryKind.XDCAM;
        
        public bool VideoDoNotEncode => _videoCodec == TVideoCodec.copy;

        public bool AudioDoNotEncode => _audioCodec == TAudioCodec.copy;

        public IngestDirectory IngestDirectory => _ingestDirectory;

        public override bool Ok(object destObject = null)
        {
            _ingestDirectory.AspectConversion = AspectConversion;
            _ingestDirectory.AudioBitrateRatio = AudioBitrateRatio;
            _ingestDirectory.AudioCodec = AudioCodec;
            _ingestDirectory.AudioVolume = AudioVolume;
            _ingestDirectory.DeleteSource = DeleteSource;
            _ingestDirectory.DirectoryName = DirectoryName;
            _ingestDirectory.DoNotEncode = AudioDoNotEncode;
            _ingestDirectory.MediaDoNotArchive = MediaDoNotArchive;
            _ingestDirectory.EncodeParams = EncodeParams;
            _ingestDirectory.ExportContainerFormat = ExportContainerFormat;
            _ingestDirectory.ExportParams = ExportParams;
            _ingestDirectory.ExportVideoFormat = ExportVideoFormat;
            _ingestDirectory.Extensions = Extensions;
            _ingestDirectory.Folder = Folder;
            _ingestDirectory.IsExport = IsExport;
            _ingestDirectory.IsImport = IsImport;
            _ingestDirectory.IsRecursive = IsRecursive;
            _ingestDirectory.IsWAN = IsWAN;
            _ingestDirectory.Kind = Kind;
            _ingestDirectory.MediaCategory = MediaCategory;
            _ingestDirectory.MediaLoudnessCheckAfterIngest = MediaLoudnessCheckAfterIngest;
            _ingestDirectory.MediaRetnentionDays = MediaRetnentionDays;
            _ingestDirectory.MXFAudioExportFormat = MXFAudioExportFormat;
            _ingestDirectory.MXFVideoExportFormat = MXFVideoExportFormat;
            _ingestDirectory.Password = Password;
            _ingestDirectory.SourceFieldOrder = SourceFieldOrder;
            _ingestDirectory.SubDirectories = SubDirectories;
            _ingestDirectory.Username = Username;
            _ingestDirectory.VideoBitrateRatio = VideoBitrateRatio;
            _ingestDirectory.VideoCodec = VideoCodec;

            foreach (var vm in SubDirectoriesVM)
                vm.Ok();
            _ingestDirectory.SubDirectories = SubDirectoriesVM.Select(vm => vm.IngestDirectory);

            return true;
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

        public ObservableCollection<IngestDirectoryViewModel> SubDirectoriesVM { get; }

        public IngestDirectoryViewModel AddSubdirectory()
        {
            var dir = new IngestDirectory { DirectoryName = Common.Properties.Resources._title_NewDirectory };
            var dirVm = new IngestDirectoryViewModel(dir, this);
            SubDirectoriesVM.Add(dirVm);
            IsModified = true;
            return dirVm;
        }

        internal ModifyableViewModelBase Owner { get; }

        internal ObservableCollection<IngestDirectoryViewModel> OwnerCollection => Owner is IngestDirectoriesViewModel root
            ? root.Directories
            : ((IngestDirectoryViewModel) Owner).SubDirectoriesVM;

        protected override void OnDispose() { }

        public void SaveToModel()
        {
            Ok();
        }
    }
}
