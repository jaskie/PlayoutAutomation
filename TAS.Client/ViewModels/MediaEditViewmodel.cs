using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using TAS.Client.Common;
using System.Threading;
using System.Threading.Tasks;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using resources = TAS.Client.Common.Properties.Resources;

namespace TAS.Client.ViewModels
{
    public class MediaEditViewmodel: EditViewmodelBase<IMedia>, IDataErrorInfo
    {
        private readonly IMediaManager _mediaManager;

        private bool _isVolumeChecking;
        private string _folder;
        private string _fileName;
        private DateTime _lastUpdated;
        private DateTime _lastAccess;
        private TMediaType _mediaType;
        private TimeSpan _duration;
        private TimeSpan _durationPlay;
        private TimeSpan _tcStart;
        private TimeSpan _tcPlay;
        private TVideoFormat _videoFormat;
        private bool _fieldOrderInverted;
        private TAudioChannelMapping _audioChannelMapping;
        private double _audioVolume;
        private string _mediaName;
        private TMediaEmphasis _mediaEmphasis;
        private DateTime _killDate;
        private bool _isProtected;
        private bool _doNotArchive;
        private byte _parental;
        private TMediaCategory _mediaCategory;
        private string _idAux;

        private AutoResetEvent _checkVolumeSignal;

        public MediaEditViewmodel(IMedia media, IMediaManager mediaManager, bool showButtons) : base(media)
        {
            CommandSaveEdit = new UiCommand(CommandName(nameof(Save)), _ => Save(), _ => CanSave());
            CommandCancelEdit = new UiCommand(CommandName(nameof(UndoEdit)), UndoEdit, _ => IsModified);
            CommandRefreshStatus = new UiCommand(CommandName(nameof(RefreshStatus)), RefreshStatus);
            CommandCheckVolume = new UiCommand
            (
                CommandName(nameof(CheckVolume)),
                CheckVolume,
                _ => !_isVolumeChecking
            );
            _mediaManager = mediaManager;
            ShowButtons = showButtons;
            Model.PropertyChanged += OnMediaPropertyChanged;
            if (Model is ITemplated templated)
            {
                TemplatedEditViewmodel = new TemplatedEditViewmodel(templated, false, false, media.VideoFormat);
                TemplatedEditViewmodel.ModifiedChanged += TemplatedEditViewmodel_ModifiedChanged;
            }
        }


        public ICommand CommandSaveEdit { get; }
        public ICommand CommandCancelEdit { get; }
        public ICommand CommandRefreshStatus { get; }
        public ICommand CommandCheckVolume { get; }

        public void Save()
        {
            TemplatedEditViewmodel?.Save();
            if (FileName != Model.FileName)
                Model.RenameFileTo(FileName);
            Update(Model);
        }

        public bool CanSave()
        {
            return IsModified && IsValid && Model.MediaStatus == TMediaStatus.Available;
        }
        
        public bool IsVolumeChecking
        {
            get => _isVolumeChecking;
            set
            {
                if (_isVolumeChecking == value)
                    return;
                _isVolumeChecking = value;
                NotifyPropertyChanged(nameof(IsVolumeChecking));
                InvalidateRequerySuggested();
            }
        }


        public bool ShowButtons { get; }

        public string Folder
        {
            get => _folder;
            set => SetField(ref _folder, value);
        }

        [IgnoreOnUpdate]
        public string FileName 
        {
            get => _fileName;
            set
            {
                if (SetField(ref _fileName, value))
                    NotifyPropertyChanged(nameof(IsValid));
            }
        }

        public DateTime LastUpdated
        {
            get => _lastUpdated;
            protected set => SetField(ref _lastUpdated, value);
        }

        public DateTime LastAccess
        {
            get => _lastAccess;
            protected set => SetField(ref _lastAccess, value);
        }

        public TMediaType MediaType
        {
            get => _mediaType;
            set => SetField(ref _mediaType, value);
        }

        public TimeSpan Duration
        {
            get => _duration;
            set => SetField(ref _duration, value);
        }

        public TimeSpan DurationPlay
        {
            get => _durationPlay;
            set => SetField(ref _durationPlay, value);
        }

        public TimeSpan TcStart
        {
            get => _tcStart;
            set => SetField(ref _tcStart, value);
        }

        public TimeSpan TcPlay
        {
            get => _tcPlay;
            set => SetField(ref _tcPlay, value);
        }
        
        public Array VideoFormats { get; } = Enum.GetValues(typeof(TVideoFormat));

        public TVideoFormat VideoFormat
        {
            get => _videoFormat;
            set
            {
                if (SetField(ref _videoFormat, value))
                    NotifyPropertyChanged(nameof(IsInterlaced));
            }
        }

        public bool FieldOrderInverted
        {
            get => _fieldOrderInverted;
            set => SetField(ref _fieldOrderInverted, value);
        }

        public Array AudioChannelMappings { get; } = Enum.GetValues(typeof(TAudioChannelMapping));

        public TAudioChannelMapping AudioChannelMapping
        {
            get => _audioChannelMapping;
            set => SetField(ref _audioChannelMapping, value);
        }

        public double AudioVolume
        {
            get => _audioVolume;
            set => SetField(ref _audioVolume, value);
        }

        public string MediaName
        {
            get => _mediaName;
            set => SetField(ref _mediaName, value);
        }

        public Array MediaEmphasises { get; } = Enum.GetValues(typeof(TMediaEmphasis));
        public TMediaEmphasis MediaEmphasis
        {
            get => _mediaEmphasis;
            set => SetField(ref _mediaEmphasis, value);
        }
        
        public DateTime KillDate
        {
            get => _killDate;
            set => SetField(ref _killDate, value);
        }

        public bool IsKillDate
        {
            get => _killDate != default;
            set
            {
                if (value == IsKillDate)
                    return;
                if (value)
                    _killDate = DateTime.UtcNow.Date.AddDays(30);
                else
                    _killDate = default;
                IsModified = true;
                NotifyPropertyChanged(nameof(KillDate));
                NotifyPropertyChanged();
            }
        }

        public DateTime LastPlayed => (Model as IServerMedia)?.LastPlayed ?? default;

        public bool IsProtected
        {
            get => _isProtected;
            set => SetField(ref _isProtected, value);
        }

        public TMediaStatus MediaStatus => Model.MediaStatus;

        public Guid MediaGuid => Model.MediaGuid;
        
        public bool DoNotArchive
        {
            get => _doNotArchive;
            set => SetField(ref _doNotArchive, value);
        }

        public bool ShowParentalCombo => _mediaManager?.Engine.CGElementsController?.Parentals!= null;

        public IEnumerable<ICGElement> Parentals => _mediaManager?.Engine.CGElementsController?.Parentals;

        public byte Parental
        {
            get => _parental;
            set => SetField(ref _parental, value);
        }

        public Array MediaCategories { get; } = Enum.GetValues(typeof(TMediaCategory));

        public TMediaCategory MediaCategory
        {
            get => _mediaCategory;
            set => SetField(ref _mediaCategory, value);
        }

        public string IdAux
        {
            get => _idAux;
            set => SetField(ref _idAux, value);
        }

        public TemplatedEditViewmodel TemplatedEditViewmodel { get; }

        public bool IsPersistentMedia => Model is IPersistentMedia;

        public bool IsServerMedia => Model is IServerMedia;

        public bool IsAnimatedMedia => Model is IAnimatedMedia;

        public bool IsIngestDataShown => Model is IPersistentMedia && Model.MediaStatus != TMediaStatus.Required;

        public bool IsMovie => Model.MediaType == TMediaType.Movie;

        public bool IsMovieOrStill => Model.MediaType == TMediaType.Movie || Model.MediaType == TMediaType.Still;

        public bool IsInterlaced
        {
            get
            {
                var format = _videoFormat;
                return VideoFormatDescription.Descriptions.ContainsKey(format) && VideoFormatDescription.Descriptions[format].Interlaced;
            }
        }

        public string Error => string.Empty;
        
        public string this[string propertyName]
        {
            get
            {
                string validationResult = null;
                switch (propertyName)
                {
                    case nameof(MediaName):
                        validationResult = ValidateMediaName();
                        break;
                    case nameof(FileName):
                        validationResult = ValidateFileName();
                        break;
                    case nameof(TcPlay):
                        validationResult = ValidateTcPlay();
                        break;
                    case nameof(DurationPlay):
                        validationResult = ValidateDurationPlay();
                        break;
                }
                return validationResult;
            }
        }


        public bool IsValid => (from pi in GetType().GetProperties() select this[pi.Name]).All(string.IsNullOrEmpty);

        public override string ToString()
        {
            return $"{Infralution.Localization.Wpf.ResourceEnumConverter.ConvertToString(MediaType)} - {MediaName}";
        }

        protected override void Update(object destObject = null)
        {
            base.Update(Model);
            (Model as IPersistentMedia)?.Save();
        }

        protected override void OnDispose()
        {
            Model.PropertyChanged -= OnMediaPropertyChanged;
            if (TemplatedEditViewmodel != null)
            {
                TemplatedEditViewmodel.ModifiedChanged -= TemplatedEditViewmodel_ModifiedChanged;
                TemplatedEditViewmodel.Dispose();
            }
        }

        private string ValidateMediaName()
        {
            if (Model is IPersistentMedia pm 
                && MediaName != null
                && pm.FieldLengths.TryGetValue(nameof(IMedia.MediaName), out var mnLength) 
                && MediaName.Length > mnLength)
                return resources._validate_TextTooLong;
            return null;
        }

        private string ValidateFileName()
        {
            var dir = Model.Directory;
            if (dir == null || _fileName == null)
                return null;
            if (FileName.StartsWith(" ") || FileName.EndsWith(" "))
                return resources._validate_FileNameCanNotStartOrEndWithSpace;
            if (FileName.IndexOfAny(Path.GetInvalidFileNameChars()) > 0)
                return resources._validate_FileNameCanNotContainSpecialCharacters;
            var fileName = FileName.ToLowerInvariant();
            if ((Model.MediaStatus == TMediaStatus.Required || fileName != Model.FileName.ToLowerInvariant())
                && dir.FileExists(FileName, Model.Folder))
                return resources._validate_FileAlreadyExists;
            if (!(Model is IPersistentMedia pm))
                return null;
            if (pm.FieldLengths.TryGetValue(nameof(IMedia.FileName), out var length) && fileName.Length > length)
                return resources._validate_TextTooLong;
            if (pm.MediaType == TMediaType.Movie
                && !FileUtils.VideoFileTypes.Contains(Path.GetExtension(fileName).ToLower()))
                return string.Format(resources._validate_FileMustHaveExtension, string.Join(resources._or_, FileUtils.VideoFileTypes));
            if (pm.MediaType == TMediaType.Still
                && !FileUtils.StillFileTypes.Contains(Path.GetExtension(fileName).ToLower()))
                return string.Format(resources._validate_FileMustHaveExtension, string.Join(resources._or_, FileUtils.StillFileTypes));
            return null;
        }

        private string ValidateTcPlay()
        {
            return TcPlay < TcStart
                   || TcPlay > TcStart + Duration
                ? resources._validateStartPlayMustBeInsideFile
                : null;
        }

        private string ValidateDurationPlay()
        {
            return DurationPlay + TcPlay > Duration + TcStart ? resources._validate_DurationInvalid : null;
        }
        
        #region Command methods


        private async void RefreshStatus(object _)
        {
            Model.MediaStatus = TMediaStatus.Unknown;
            await Task.Run(() => Model.Verify(true));
        }

        private void CheckVolume(object _)
        {
            if (_isVolumeChecking)
                return;
            IsVolumeChecking = true;
            var fileManager = _mediaManager.FileManager;
            var operation = (ILoudnessOperation)fileManager.CreateFileOperation(TFileOperationKind.Loudness);
            operation.Source = Model;
            operation.MeasureStart = TcPlay - TcStart;
            operation.MeasureDuration = DurationPlay;
            operation.AudioVolumeMeasured += AudioVolumeMeasured;
            operation.Finished += AudioVolumeFinished;
            _checkVolumeSignal = new AutoResetEvent(false);
            fileManager.Queue(operation);
        }

        private void AudioVolumeFinished(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                _checkVolumeSignal.WaitOne(5000);
                IsVolumeChecking = false; // finishCallback
                ((ILoudnessOperation)sender).Finished -= AudioVolumeFinished;
                ((ILoudnessOperation)sender).AudioVolumeMeasured -= AudioVolumeMeasured;
                _checkVolumeSignal.Dispose();
                _checkVolumeSignal = null;
            });
        }

        private void AudioVolumeMeasured(object _, AudioVolumeEventArgs e)
        {
            AudioVolume = e.AudioVolume;
            AutoResetEvent signal = _checkVolumeSignal;
            signal?.Set();
        }

        #endregion //Command methods

        private void OnMediaPropertyChanged(object media, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName))
                return;
            OnUiThread(() =>
            {
                var sourcePi = Model.GetType().GetProperty(e.PropertyName);
                var destPi = GetType().GetProperty(e.PropertyName);
                if (!(sourcePi is null || destPi is null || !sourcePi.CanRead || !destPi.CanWrite))
                {
                    var oldModified = IsModified;
                    destPi.SetValue(this, sourcePi.GetValue(Model, null), null);
                    IsModified = oldModified;
                }
                NotifyPropertyChanged(e.PropertyName);
                switch (e.PropertyName)
                {
                    case nameof(IMedia.MediaStatus):
                        NotifyPropertyChanged(nameof(IsIngestDataShown));
                        break;
                    case nameof(IPersistentMedia.KillDate):
                        NotifyPropertyChanged(nameof(IsKillDate));
                        break;
                }
            });
        }

        private void UndoEdit(object _)
        {
            TemplatedEditViewmodel?.UndoEdit();
            Load();
        }
        private void TemplatedEditViewmodel_ModifiedChanged(object sender, EventArgs e)
        {
            if (!(sender is TemplatedEditViewmodel templatedEditViewmodel))
                return;
            if (templatedEditViewmodel.IsModified)
                IsModified = true;
        }


    }


}
