using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.ComponentModel;
using TAS.Common;
using TAS.Common.Interfaces;
using resources = TAS.Client.Common.Properties.Resources;

namespace TAS.Client.ViewModels
{
    public class IngestOperationViewModel: FileOperationViewmodel, IDataErrorInfo
    {
        private readonly IIngestOperation _ingestOperation;
        private readonly IEngine _engine;
        private readonly IPersistentMediaProperties _destMediaProperties;

        private TAspectConversion _aspectConversion;
        private TAudioChannelMappingConversion _audioChannelMappingConversion;
        private double _audioVolume;
        private TFieldOrder _sourceFieldOrderEnforceConversion;
        private bool _loudnessCheck;
        
        public IngestOperationViewModel(IIngestOperation operation, IPreview preview, IEngine engine)
            : base(operation)
        {
            _ingestOperation = operation;
            _engine = engine;
            string destFileName = $"{Path.GetFileNameWithoutExtension(operation.Source.FileName)}.{operation.MovieContainerFormat}";
            _destMediaProperties = new PersistentMediaProxy
            {
                FileName = operation.DestDirectory.GetUniqueFileName(destFileName),
                MediaName = FileUtils.GetFileNameWithoutExtension(destFileName, operation.Source.MediaType),
                MediaType = operation.Source.MediaType == TMediaType.Unknown ? TMediaType.Movie : operation.Source.MediaType,
                Duration = operation.Source.Duration,
                TcStart = operation.StartTC,
                MediaGuid = operation.Source.MediaGuid,
                MediaCategory = operation.Source.MediaCategory
            };
            
            _audioChannelMappingConversion = operation.AudioChannelMappingConversion;
            _aspectConversion = operation.AspectConversion;
            _audioVolume = operation.AudioVolume;
            _sourceFieldOrderEnforceConversion = operation.SourceFieldOrderEnforceConversion;
            _loudnessCheck = operation.LoudnessCheck;
            operation.Source.PropertyChanged += OnSourceMediaPropertyChanged;
            AspectConversionsEnforce = new TAspectConversion[3];
            Array.Copy(AspectConversions, AspectConversionsEnforce, 3);
            if (preview != null)
                PreviewViewmodel = new PreviewViewmodel(engine, preview) { SelectedIngestOperation = operation };
        }

        public Array Categories { get; } = Enum.GetValues(typeof(TMediaCategory));
        public TMediaCategory DestCategory { get => _destMediaProperties.MediaCategory; set => _destMediaProperties.MediaCategory = value; }
        
        public IEnumerable<ICGElement> Parentals => _engine?.CGElementsController?.Parentals;

        public byte DestParental { get => _destMediaProperties.Parental; set => _destMediaProperties.Parental = value; }

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
    
        public bool EncodeVideo => ((IIngestDirectory)_ingestOperation.Source.Directory).VideoCodec != TVideoCodec.copy;

        public bool EncodeAudio => ((IIngestDirectory)_ingestOperation.Source.Directory).AudioCodec != TAudioCodec.copy;

        public bool Trim
        {
            get => _ingestOperation.Trim;
            set => _ingestOperation.Trim = value;
        }

        public string SourceFileName => $"{_ingestOperation.Source.Directory.DirectoryName}:{_ingestOperation.Source.FileName}";

        public string DestMediaName
        {
            get => _destMediaProperties.MediaName;
            set
            {
                _destMediaProperties.MediaName = value;
                _makeFileName();
            }
        }
        
        public TimeSpan StartTC
        {
            get => _destMediaProperties.TcStart;
            set
            {
                if (_destMediaProperties.TcStart == value)
                    return;
                _destMediaProperties.TcStart = value;
                _destMediaProperties.TcPlay = value;
                NotifyPropertyChanged(nameof(StartTC));
                NotifyPropertyChanged(nameof(Duration));
                NotifyPropertyChanged(nameof(EndTC));
                NotifyPropertyChanged(nameof(IsValid));
            }
        }

        public TimeSpan Duration
        {
            get => _destMediaProperties.Duration;
            set
            {
                if (_destMediaProperties.Duration == value)
                    return;
                _destMediaProperties.Duration = value;
                _destMediaProperties.DurationPlay = value;
                NotifyPropertyChanged(nameof(StartTC));
                NotifyPropertyChanged(nameof(Duration));
                NotifyPropertyChanged(nameof(EndTC));
                NotifyPropertyChanged(nameof(IsValid));
            }
        }

        public TimeSpan EndTC
        {
            get => ((StartTC + Duration).ToSMPTEFrames(SourceMediaFrameRate()) - 1).SMPTEFramesToTimeSpan(SourceMediaFrameRate());
            set
            {
                var duration = ((value - StartTC).ToSMPTEFrames(SourceMediaFrameRate()) + 1).SMPTEFramesToTimeSpan(SourceMediaFrameRate());
                Duration = duration;
            }
        }

        public string IdAux
        {
            get => _destMediaProperties.IdAux;
            set
            {
                _destMediaProperties.IdAux = value;
                _makeFileName();
            }
        }

        public string DestFileName { 
            get => _destMediaProperties.FileName;
            set
            {
                if (_destMediaProperties.FileName == value)
                    return;
                _destMediaProperties.FileName = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsValid));
            }
        }
        
        public Array MediaEmphasises { get; } = Enum.GetValues(typeof(TMediaEmphasis));

        public TMediaEmphasis DestMediaEmphasis
        {
            get => _destMediaProperties.MediaEmphasis;
            set => _destMediaProperties.MediaEmphasis = value;
        }

        public Array VideoFormats { get; } = Enum.GetValues(typeof(TVideoFormat));

        public TVideoFormat DestMediaVideoFormat
        {
            get => _destMediaProperties.VideoFormat;
            set => _destMediaProperties.VideoFormat = value;
        }

        public bool ShowParentalCombo => _engine?.CGElementsController?.Parentals != null;

        public bool CanTrim => EncodeVideo && EncodeAudio && _ingestOperation.Source.MediaStatus == TMediaStatus.Available && _ingestOperation.Source.Duration > TimeSpan.Zero;

        public PreviewViewmodel PreviewViewmodel { get; }

        public bool CanPreview => (PreviewViewmodel != null && ((IIngestDirectory)_ingestOperation.Source.Directory).AccessType == TDirectoryAccessType.Direct);

        public bool LoudnessCheck {
            get => _loudnessCheck;
            set => SetField(ref _loudnessCheck, value);
        }
        
        public bool IsValid => (from pi in GetType().GetProperties() select this[pi.Name]).All(string.IsNullOrEmpty);

        public bool IsMovie => _destMediaProperties.MediaType == TMediaType.Movie;

        public bool IsStill => _destMediaProperties.MediaType == TMediaType.Still;

        public void Apply()
        {
            _ingestOperation.LoudnessCheck = _loudnessCheck;
            _ingestOperation.AudioVolume = _audioVolume;
            _ingestOperation.StartTC = StartTC;
            _ingestOperation.Duration = Duration;
            _ingestOperation.SourceFieldOrderEnforceConversion = _sourceFieldOrderEnforceConversion;
            _ingestOperation.AudioChannelMappingConversion = _audioChannelMappingConversion;
            _ingestOperation.AspectConversion = _aspectConversion;
            _ingestOperation.SourceFieldOrderEnforceConversion = _sourceFieldOrderEnforceConversion;
            _ingestOperation.DestProperties = _destMediaProperties; 
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
                }
                return null;
            }
        }

        public string Error => string.Empty;

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
                    StartTC = _ingestOperation.StartTC;
                    break;
                case nameof(IIngestOperation.Duration):
                    Duration = _ingestOperation.Duration;
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
                    Duration = _ingestOperation.Source.DurationPlay;
                    NotifyPropertyChanged(nameof(CanTrim));
                    break;
                case nameof(IMedia.TcPlay):
                    StartTC = _ingestOperation.Source.TcPlay;
                    break;
                case nameof(IMedia.IsVerified):
                    NotifyPropertyChanged(nameof(IsValid));
                    break;
            }
        }

        protected override void OnDispose()
        {
            _ingestOperation.Source.PropertyChanged -= OnSourceMediaPropertyChanged;
            PreviewViewmodel?.Dispose();
            base.OnDispose();
        }

        private RationalNumber SourceMediaFrameRate() => _ingestOperation.Source.FrameRate();

        private void _makeFileName()
        {
            DestFileName = MediaExtensions.MakeFileName(IdAux, DestMediaName, _ingestOperation.MovieContainerFormat);
        }

        private string ValidateTc()
        {
            if (IsStill)
                return null;
            if (StartTC < _ingestOperation.Source.TcStart)
                return string.Format(resources._validate_StartTCBeforeFile, _ingestOperation.Source.TcStart.ToSMPTETimecodeString(_ingestOperation.Source.VideoFormat));
            if (StartTC > _ingestOperation.Source.TcLastFrame())
                return string.Format(resources._validate_StartTCAfterFile, _ingestOperation.Source.TcLastFrame().ToSMPTETimecodeString(_ingestOperation.Source.VideoFormat));
            if (EndTC < _ingestOperation.Source.TcStart)
                return string.Format(resources._validate_EndTCBeforeFile, _ingestOperation.Source.TcStart.ToSMPTETimecodeString(_ingestOperation.Source.VideoFormat));
            if (EndTC > _ingestOperation.Source.TcLastFrame())
                return string.Format(resources._validate_EndTCAfterFile, _ingestOperation.Source.TcLastFrame().ToSMPTETimecodeString(_ingestOperation.Source.VideoFormat));
            return null;
        }

        private string ValidateDestFileName()
        {
            if (!(_ingestOperation.DestDirectory is IServerDirectory dir))
                throw new ApplicationException("Invalid directory in ValidateDestFileName");
            if (_destMediaProperties.FileName.StartsWith(" ") || _destMediaProperties.FileName.EndsWith(" "))
                return resources._validate_FileNameCanNotStartOrEndWithSpace;
            if (_destMediaProperties.FileName.IndexOfAny(Path.GetInvalidFileNameChars()) > 0)
                return resources._validate_FileNameCanNotContainSpecialCharacters;
            if (_engine.ServerMediaFieldLengths.TryGetValue(nameof(IServerMedia.FileName), out var length) && _destMediaProperties.FileName.Length > length)
                return resources._validate_TextTooLong;
            var newName = _destMediaProperties.FileName.ToLowerInvariant();
            if (dir.FileExists(newName, _destMediaProperties.Folder))
                return resources._validate_FileAlreadyExists;
            switch (_destMediaProperties.MediaType)
            {
                case TMediaType.Movie when !FileUtils.VideoFileTypes.Contains(Path.GetExtension(newName).ToLower()):
                    return string.Format(resources._validate_FileMustHaveExtension, string.Join(resources._or_, FileUtils.VideoFileTypes));
                case TMediaType.Still when !FileUtils.StillFileTypes.Contains(Path.GetExtension(newName).ToLower()):
                    return string.Format(resources._validate_FileMustHaveExtension, string.Join(resources._or_, FileUtils.StillFileTypes));
            }
            return null;
        }
    }
}
