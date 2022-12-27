using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.ComponentModel;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Client.Common.Plugin;
using resources = TAS.Client.Common.Properties.Resources;

namespace TAS.Client.ViewModels
{
    public class IngestOperationViewModel : FileOperationViewmodel, IDataErrorInfo, IUiPreviewProvider
    {
        private readonly IIngestOperation _operation;
        private readonly IEngine _engine;

        private TAspectConversion _aspectConversion;
        private TAudioChannelMappingConversion _audioChannelMappingConversion;
        private double _audioVolume;
        private TFieldOrder _sourceFieldOrderEnforceConversion;
        private bool _loudnessCheck;
        private TMediaCategory _destCategory;
        private byte _destParental;
        private string _destMediaName;
        private TimeSpan _startTc;
        private TimeSpan _duration;
        private string _idAux;
        private string _destFileName;
        private TMediaEmphasis _destMediaEmphasis;
        private TVideoFormat _destMediaVideoFormat;
        private DateTime? _killDate;

        public IngestOperationViewModel(IIngestOperation operation, IEngine engine)
            : base(operation, engine.MediaManager)
        {
            _operation = operation;
            _engine = engine;
            _destMediaVideoFormat = operation.Source.VideoFormat;
            DestMediaName = FileUtils.GetFileNameWithoutExtension(operation.Source.MediaName, operation.Source.MediaType);
            _duration = operation.Source.Duration;
            _startTc = operation.Source.TcStart;
            _destCategory = ((IIngestDirectory)operation.Source.Directory).MediaCategory;
            IsMovie = operation.Source.MediaType == TMediaType.Unknown || operation.Source.MediaType == TMediaType.Movie;
            IsStill = operation.Source.MediaType == TMediaType.Still;
            _audioChannelMappingConversion = operation.AudioChannelMappingConversion;
            _aspectConversion = operation.AspectConversion;
            _audioVolume = operation.AudioVolume;
            _sourceFieldOrderEnforceConversion = operation.SourceFieldOrderEnforceConversion;
            _loudnessCheck = operation.LoudnessCheck;
            operation.Source.PropertyChanged += OnSourceMediaPropertyChanged;
            AspectConversionsEnforce = new TAspectConversion[3];
            Array.Copy(AspectConversions, AspectConversionsEnforce, 3);
            if (engine.Preview != null)
                _preview = new PreviewViewmodel(engine.Preview, true, false) { SelectedIngestOperation = operation };
            CommandRemove = new UiCommand(o => Removed?.Invoke(this, EventArgs.Empty));
        }

        public ICommand CommandRemove { get; }

        public event EventHandler Removed;

        public Array Categories { get; } = Enum.GetValues(typeof(TMediaCategory));
        public TMediaCategory DestCategory { get => _destCategory; set => SetField(ref _destCategory, value); }

        public IEnumerable<ICGElement> Parentals => _engine?.CGElementsController?.Parentals;

        public byte DestParental { get => _destParental; set => SetField(ref _destParental, value); }

        public Array AspectConversions { get; } = Enum.GetValues(typeof(TAspectConversion));
        public Array AspectConversionsEnforce { get; }

        public TAspectConversion AspectConversion
        {
            get => _aspectConversion;
            set => SetField(ref _aspectConversion, value);
        }

        public Array AudioChannelMappingConversions { get; } = Enum.GetValues(typeof(TAudioChannelMappingConversion));
        public TAudioChannelMappingConversion AudioChannelMappingConversion
        {
            get => _audioChannelMappingConversion;
            set => SetField(ref _audioChannelMappingConversion, value);
        }

        public double AudioVolume
        {
            get => _audioVolume;
            set => SetField(ref _audioVolume, value);
        }

        public Array SourceFieldOrderEnforceConversions { get; } = Enum.GetValues(typeof(TFieldOrder));
        public TFieldOrder SourceFieldOrderEnforceConversion
        {
            get => _sourceFieldOrderEnforceConversion;
            set => SetField(ref _sourceFieldOrderEnforceConversion, value);
        }

        public bool EncodeVideo => ((IIngestDirectory)_operation.Source.Directory).VideoCodec != TVideoCodec.copy;

        public bool EncodeAudio => ((IIngestDirectory)_operation.Source.Directory).AudioCodec != TAudioCodec.copy;

        public bool Trim
        {
            get => _operation.Trim;
            set => _operation.Trim = value;
        }

        public string SourceFileName => $"{_operation.Source.Directory.GetDisplayName(_engine.MediaManager)}:{_operation.Source.MediaName}";

        public string DestMediaName
        {
            get => _destMediaName;
            set
            {
                if (!SetField(ref _destMediaName, value))
                    return;
                _makeFileName();
            }
        }

        public TimeSpan StartTC
        {
            get => _startTc;
            set
            {
                if (!SetField(ref _startTc, value))
                    return;
                NotifyPropertyChanged(nameof(Duration));
                NotifyPropertyChanged(nameof(EndTC));
                NotifyPropertyChanged(nameof(IsValid));
            }
        }

        public DateTime? KillDate
        {
            get => _killDate;
            set
            {
                if(!SetField(ref _killDate, value))
                    return;
                NotifyPropertyChanged(nameof(IsKillDate));
            }
        }

        public bool IsKillDate
        {
            get => _killDate != null;
            set
            {
                if (value == IsKillDate)
                    return;
                if (value)
                    _killDate = DateTime.UtcNow.Date.AddDays(30);
                else
                    _killDate = null;
                NotifyPropertyChanged(nameof(KillDate));
                NotifyPropertyChanged();
            }
        }

        public TimeSpan Duration
        {
            get => _duration;
            set
            {
                if (!SetField(ref _duration, value))
                    return;
                NotifyPropertyChanged(nameof(StartTC));
                NotifyPropertyChanged(nameof(EndTC));
                NotifyPropertyChanged(nameof(IsValid));
            }
        }

        public TimeSpan EndTC
        {
            get => ((StartTC + Duration).ToSmpteFrames(SourceMediaFrameRate()) - 1).SmpteFramesToTimeSpan(SourceMediaFrameRate());
            set
            {
                var duration = ((value - StartTC).ToSmpteFrames(SourceMediaFrameRate()) + 1).SmpteFramesToTimeSpan(SourceMediaFrameRate());
                Duration = duration;
            }
        }

        public string IdAux
        {
            get => _idAux;
            set
            {
                if (!SetField(ref _idAux, value))
                    return;
                _makeFileName();
            }
        }

        public string DestFileName
        {
            get => _destFileName;
            set
            {
                if (!SetField(ref _destFileName, value))
                    return;
                NotifyPropertyChanged(nameof(IsValid));
            }
        }

        public Array MediaEmphasises { get; } = Enum.GetValues(typeof(TMediaEmphasis));

        public TMediaEmphasis DestMediaEmphasis
        {
            get => _destMediaEmphasis;
            set => SetField(ref _destMediaEmphasis, value);
        }

        public Array VideoFormats { get; } = Enum.GetValues(typeof(TVideoFormat));

        public TVideoFormat DestMediaVideoFormat
        {
            get => _destMediaVideoFormat;
            set
            {
                if (!SetField(ref _destMediaVideoFormat, value))
                    return;
                NotifyPropertyChanged(nameof(IsValid));
            }
        }

        public bool ShowParentalCombo => _engine?.CGElementsController?.Parentals != null;

        public bool CanTrim => EncodeVideo && EncodeAudio && _operation.Source.MediaStatus == TMediaStatus.Available && _operation.Source.Duration > TimeSpan.Zero;

        private PreviewViewmodel _preview;

        public bool CanPreview => (_preview != null && ((IIngestDirectory)_operation.Source.Directory).AccessType == TDirectoryAccessType.Direct);

        public bool LoudnessCheck
        {
            get => _loudnessCheck;
            set => SetField(ref _loudnessCheck, value);
        }

        public bool IsValid => (from pi in GetType().GetProperties() select this[pi.Name]).All(string.IsNullOrEmpty);

        public bool IsMovie { get; }

        public bool IsStill { get; }

        public void Apply()
        {
            _operation.LoudnessCheck = _loudnessCheck;
            _operation.AudioVolume = _audioVolume;
            _operation.StartTC = StartTC;
            _operation.Duration = Duration;
            _operation.SourceFieldOrderEnforceConversion = _sourceFieldOrderEnforceConversion;
            _operation.AudioChannelMappingConversion = _audioChannelMappingConversion;
            _operation.AspectConversion = _aspectConversion;
            _operation.SourceFieldOrderEnforceConversion = _sourceFieldOrderEnforceConversion;
            _operation.DestProperties = new PersistentMediaProxy
            {
                FileName = DestFileName,
                MediaName = DestMediaName,
                MediaType = IsMovie ? TMediaType.Movie : TMediaType.Still,
                Duration = Duration,
                TcStart = StartTC,
                MediaGuid = _operation.Source.MediaGuid,
                MediaCategory = DestCategory,
                Parental = DestParental,
                MediaEmphasis = DestMediaEmphasis,
                VideoFormat = DestMediaVideoFormat,
                KillDate = KillDate
            };
        }

        public string this[string propertyName]
        {
            get
            {
                switch (propertyName)
                {
                    case nameof(DestFileName):
                        return ValidateDestFileName();
                    case nameof(StartTC):
                    case nameof(EndTC):
                    case nameof(Duration):
                        return ValidateTc();
                    case nameof(DestMediaName):
                        if (string.IsNullOrEmpty(DestMediaName))
                            return null;
                        if (_engine.ServerMediaFieldLengths.TryGetValue(nameof(IServerMedia.MediaName), out var mnLength) && DestMediaName.Length > mnLength)
                            return resources._validate_TextTooLong;
                        break;
                    case nameof(IdAux):
                        if (string.IsNullOrEmpty(IdAux))
                            return null;
                        if (_engine.ServerMediaFieldLengths.TryGetValue(nameof(IServerMedia.IdAux), out var iaLength) && IdAux.Length > iaLength)
                            return resources._validate_TextTooLong;
                        break;
                    case nameof(DestMediaVideoFormat):
                        if (DestMediaVideoFormat == TVideoFormat.Unknown)
                            return resources._validate_InvalidVideoFormat;
                        break;
                }
                return null;
            }
        }

        public string Error => string.Empty;

        public IUiPreview Preview => _preview;

        public IEngine Engine => _engine;

        // utilities

        protected override void OnFileOperationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(IIngestOperation.AspectConversion):
                case nameof(IIngestOperation.AudioChannelMappingConversion):
                case nameof(IIngestOperation.AudioVolume):
                case nameof(IIngestOperation.SourceFieldOrderEnforceConversion):
                case nameof(IIngestOperation.OperationOutput):
                case nameof(IIngestOperation.Trim):
                    NotifyPropertyChanged(e.PropertyName);
                    break;
                case nameof(IIngestOperation.StartTC):
                    StartTC = _operation.StartTC;
                    break;
                case nameof(IIngestOperation.Duration):
                    Duration = _operation.Duration;
                    break;
                default:
                    base.OnFileOperationPropertyChanged(sender, e);
                    break;
            }
        }

        protected virtual void OnSourceMediaPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(IMedia.FileName):
                    NotifyPropertyChanged(nameof(SourceFileName));
                    break;
                case nameof(IMedia.MediaStatus):
                    NotifyPropertyChanged(nameof(CanTrim));
                    break;
                case nameof(IMedia.DurationPlay):
                    Duration = _operation.Source.DurationPlay;
                    NotifyPropertyChanged(nameof(CanTrim));
                    break;
                case nameof(IMedia.TcPlay):
                    StartTC = _operation.Source.TcPlay;
                    break;
                case (nameof(IMedia.VideoFormat)):
                    DestMediaVideoFormat = _operation.Source.VideoFormat;
                    break;
                case nameof(IMedia.IsVerified):
                    NotifyPropertyChanged(nameof(IsValid));
                    break;
            }
        }

        protected override void OnDispose()
        {
            _operation.Source.PropertyChanged -= OnSourceMediaPropertyChanged;
            _preview?.Dispose();
            base.OnDispose();
        }

        private RationalNumber SourceMediaFrameRate() => _operation.Source.FrameRate();

        private void _makeFileName()
        {
            DestFileName = MediaExtensions.MakeFileName(IdAux, DestMediaName, _operation.Source.MediaType == TMediaType.Movie ? $".{(_operation.DestDirectory as IServerDirectory)?.MovieContainerFormat ?? TMovieContainerFormat.mov}" : FileUtils.DefaultFileExtension(_operation.Source.MediaType));
        }

        private string ValidateTc()
        {
            if (IsStill)
                return null;
            if (StartTC < _operation.Source.TcStart)
                return string.Format(resources._validate_StartTCBeforeFile, _operation.Source.TcStart.ToSmpteTimecodeString(_operation.Source.VideoFormat));
            if (StartTC > _operation.Source.TcLastFrame())
                return string.Format(resources._validate_StartTCAfterFile, _operation.Source.TcLastFrame().ToSmpteTimecodeString(_operation.Source.VideoFormat));
            if (EndTC < _operation.Source.TcStart)
                return string.Format(resources._validate_EndTCBeforeFile, _operation.Source.TcStart.ToSmpteTimecodeString(_operation.Source.VideoFormat));
            if (EndTC > _operation.Source.TcLastFrame())
                return string.Format(resources._validate_EndTCAfterFile, _operation.Source.TcLastFrame().ToSmpteTimecodeString(_operation.Source.VideoFormat));
            return null;
        }

        private string ValidateDestFileName()
        {
            if (!(_operation.DestDirectory is IServerDirectory dir))
                throw new ApplicationException("Invalid directory in ValidateDestFileName");
            if (DestFileName.StartsWith(" ") || DestFileName.EndsWith(" "))
                return resources._validate_FileNameCanNotStartOrEndWithSpace;
            if (DestFileName.IndexOfAny(Path.GetInvalidFileNameChars()) > 0)
                return resources._validate_FileNameCanNotContainSpecialCharacters;
            if (_engine.ServerMediaFieldLengths.TryGetValue(nameof(IServerMedia.FileName), out var length) && DestFileName.Length > length)
                return resources._validate_TextTooLong;
            var newName = DestFileName.ToLowerInvariant();
            if (dir.FileExists(newName))
                return resources._validate_FileAlreadyExists;
            if (IsMovie && !FileUtils.VideoFileTypes.Contains(Path.GetExtension(newName).ToLower()))
                return string.Format(resources._validate_FileMustHaveExtension, string.Join(resources._or_, FileUtils.VideoFileTypes));
            if (IsStill && !FileUtils.StillFileTypes.Contains(Path.GetExtension(newName).ToLower()))
                return string.Format(resources._validate_FileMustHaveExtension, string.Join(resources._or_, FileUtils.StillFileTypes));
            return null;
        }
    }
}
