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
        private readonly IIngestOperation _convertOperation;
        private readonly PreviewViewmodel _previewVm;
        private readonly IMediaManager _mediaManager;
        private readonly IPersistentMediaProperties _destMediaProperties;

        private TAspectConversion _aspectConversion;
        private TAudioChannelMappingConversion _audioChannelMappingConversion;
        private double _audioVolume;
        private TFieldOrder _sourceFieldOrderEnforceConversion;
        private bool _trim;
        private bool _loudnessCheck;
        private TimeSpan _startTC;
        private TimeSpan _duration;
        
        public IngestOperationViewModel(IIngestOperation operation, IPersistentMediaProperties destMediaProperties, IPreview preview, IMediaManager mediaManager)
            : base(operation)
        {
            _convertOperation = operation;
            _mediaManager = mediaManager;
            _destMediaProperties = destMediaProperties;

            _audioChannelMappingConversion = operation.AudioChannelMappingConversion;
            _aspectConversion = operation.AspectConversion;
            _audioVolume = operation.AudioVolume;
            _sourceFieldOrderEnforceConversion = operation.SourceFieldOrderEnforceConversion;
            _duration = operation.Duration;
            _startTC = operation.StartTC;
            _trim = operation.Trim;
            _loudnessCheck = operation.LoudnessCheck;
            operation.Source.PropertyChanged += OnSourceMediaPropertyChanged;
            AspectConversionsEnforce = new TAspectConversion[3];
            Array.Copy(AspectConversions, AspectConversionsEnforce, 3);
            if (preview != null)
                _previewVm = new PreviewViewmodel(preview) { SelectedMedia = operation.Source };
        }
        
        public Array Categories { get; } = Enum.GetValues(typeof(TMediaCategory));
        public TMediaCategory DestCategory { get { return _destMediaProperties.MediaCategory; } set { _destMediaProperties.MediaCategory = value; } }
        
        public IEnumerable<ICGElement> Parentals => _mediaManager?.CGElementsController?.Parentals;
        public byte DestParental { get { return _destMediaProperties.Parental; } set { _destMediaProperties.Parental = value; } }

        public Array AspectConversions { get; } = Enum.GetValues(typeof(TAspectConversion));
        public Array AspectConversionsEnforce { get; }

        public TAspectConversion AspectConversion
        {
            get { return _aspectConversion; }
            set { SetField(ref _aspectConversion, value); }
        }

        public Array AudioChannelMappingConversions { get; } = Enum.GetValues(typeof(TAudioChannelMappingConversion));
        public TAudioChannelMappingConversion AudioChannelMappingConversion
        {
            get { return _audioChannelMappingConversion; }
            set { SetField(ref _audioChannelMappingConversion, value); }
        }

        public double AudioVolume
        {
            get { return _audioVolume; }
            set { SetField(ref _audioVolume, value); }
        }

        public Array SourceFieldOrderEnforceConversions { get; } = Enum.GetValues(typeof(TFieldOrder));
        public TFieldOrder SourceFieldOrderEnforceConversion
        {
            get { return _sourceFieldOrderEnforceConversion; }
            set { SetField(ref _sourceFieldOrderEnforceConversion, value); }
        }
    
        public bool EncodeVideo => ((IIngestDirectory)_convertOperation.Source.Directory).VideoCodec != TVideoCodec.copy;

        public bool EncodeAudio => ((IIngestDirectory)_convertOperation.Source.Directory).AudioCodec != TAudioCodec.copy;

        public bool Trim { get { return _trim; } set { SetField(ref _trim, value); } }

        public string SourceFileName => $"{_convertOperation.Source.Directory.DirectoryName}:{_convertOperation.Source.FileName}";

        public string DestMediaName
        {
            get { return _destMediaProperties.MediaName; }
            set
            {
                _destMediaProperties.MediaName = value;
                _makeFileName();
            }
        }
        
        public TimeSpan StartTC
        {
            get { return _startTC; }
            set
            {
                if (SetField(ref _startTC, value))
                    NotifyPropertyChanged(nameof(EndTC));
            }
        }

        public TimeSpan Duration
        {
            get { return _duration; }
            set
            {
                if (SetField(ref _duration, value))
                    NotifyPropertyChanged(nameof(EndTC));
            }
        }

        public TimeSpan EndTC
        {
            get { return ((_startTC + _duration).ToSMPTEFrames(SourceMediaFrameRate()) - 1).SMPTEFramesToTimeSpan(SourceMediaFrameRate()); }
            set
            {
                var end = ((value - StartTC).ToSMPTEFrames(SourceMediaFrameRate()) + 1).SMPTEFramesToTimeSpan(SourceMediaFrameRate());
                if (SetField(ref _duration, end))
                    NotifyPropertyChanged(nameof(Duration));
            }
        }

        public string IdAux
        {
            get { return _destMediaProperties.IdAux; }
            set
            {
                _destMediaProperties.IdAux = value;
                _makeFileName();
            }
        }

        public string DestFileName { 
            get { return _destMediaProperties.FileName; }
            set
            {
                if (_destMediaProperties.FileName != value)
                {
                    _destMediaProperties.FileName = value;
                    NotifyPropertyChanged();
                }
            }
        }
        
        public Array MediaEmphasises { get; } = Enum.GetValues(typeof(TMediaEmphasis));

        public TMediaEmphasis DestMediaEmphasis
        {
            get { return _destMediaProperties.MediaEmphasis; }
            set { _destMediaProperties.MediaEmphasis = value; }
        }

        public Array VideoFormats { get; } = Enum.GetValues(typeof(TVideoFormat));

        public TVideoFormat DestMediaVideoFormat
        {
            get { return _destMediaProperties.VideoFormat; }
            set { _destMediaProperties.VideoFormat = value; }
        }

        public bool ShowParentalCombo => _mediaManager?.CGElementsController?.Parentals != null;

        public bool CanTrim => EncodeVideo && EncodeAudio && _convertOperation.Source.MediaStatus == TMediaStatus.Available && _convertOperation.Source.Duration > TimeSpan.Zero;

        public PreviewViewmodel PreviewViewmodel => _previewVm;

        public bool CanPreview => (_previewVm != null && ((IIngestDirectory)_convertOperation.Source.Directory).AccessType == TDirectoryAccessType.Direct);

        public bool LoudnessCheck {
            get { return _loudnessCheck; }
            set { SetField(ref _loudnessCheck, value); }
        }
        
        public bool IsValid => (from pi in GetType().GetProperties() select this[pi.Name]).All(string.IsNullOrEmpty);

        public bool IsMovie => _destMediaProperties.MediaType == TMediaType.Movie;

        public bool IsStill => _destMediaProperties.MediaType == TMediaType.Still;

        public void Apply()
        {
            _convertOperation.Trim = _trim;
            _convertOperation.LoudnessCheck = _loudnessCheck;
            _convertOperation.AudioVolume = _audioVolume;
            _convertOperation.StartTC = _startTC;
            _convertOperation.Duration = _duration;
            _convertOperation.SourceFieldOrderEnforceConversion = _sourceFieldOrderEnforceConversion;
            _convertOperation.AudioChannelMappingConversion = _audioChannelMappingConversion;
            _convertOperation.AspectConversion = _aspectConversion;
            _convertOperation.SourceFieldOrderEnforceConversion = _sourceFieldOrderEnforceConversion;


            _destMediaProperties.TcStart = _startTC;
            _destMediaProperties.TcPlay = _startTC;
            _destMediaProperties.Duration = _duration;
            _destMediaProperties.DurationPlay = _duration;
            _convertOperation.DestProperties = _destMediaProperties;  //required to pass this parameter from client to server application
        }

        public string this[string propertyName]
        {
            get
            {
                string validationResult = null;
                switch (propertyName)
                {
                    case nameof(DestFileName):
                        validationResult = ValidateDestFileName();
                        break;
                    case nameof(StartTC):
                    case nameof(EndTC):
                    case nameof(Duration):
                        validationResult = ValidateTc();
                        break;
                }
                return validationResult;
            }
        }

        public string Error => string.Empty;

        // utilities
        
        protected override void OnFileOperationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IIngestOperation.AspectConversion)
                || e.PropertyName == nameof(IIngestOperation.AudioChannelMappingConversion)
                || e.PropertyName == nameof(IIngestOperation.AudioVolume)
                || e.PropertyName == nameof(IIngestOperation.SourceFieldOrderEnforceConversion)
                || e.PropertyName == nameof(IIngestOperation.OperationOutput)
                || e.PropertyName == nameof(IIngestOperation.StartTC)
                || e.PropertyName == nameof(IIngestOperation.Duration)
            )
                NotifyPropertyChanged(e.PropertyName);
            else
                base.OnFileOperationPropertyChanged(sender, e);
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
                    Duration = _convertOperation.Source.DurationPlay;
                    NotifyPropertyChanged(nameof(CanTrim));
                    break;
                case nameof(IMedia.TcPlay):
                    StartTC = _convertOperation.Source.TcPlay;
                    break;
            }
        }

        protected override void OnDispose()
        {
            _convertOperation.Source.PropertyChanged -= OnSourceMediaPropertyChanged;
            _previewVm?.Dispose();
            base.OnDispose();
        }

        private RationalNumber SourceMediaFrameRate() => _convertOperation.Source.FrameRate();

        private void _makeFileName()
        {
            DestFileName = MediaExtensions.MakeFileName(IdAux, DestMediaName, FileUtils.DefaultFileExtension(_destMediaProperties.MediaType));
        }

        private string ValidateTc()
        {
            if (StartTC < _convertOperation.Source.TcStart)
                return string.Format(resources._validate_StartTCBeforeFile, _convertOperation.Source.TcStart.ToSMPTETimecodeString(_convertOperation.Source.VideoFormat));
            if (StartTC > _convertOperation.Source.TcLastFrame())
                return string.Format(resources._validate_StartTCAfterFile, _convertOperation.Source.TcLastFrame().ToSMPTETimecodeString(_convertOperation.Source.VideoFormat));
            if (EndTC < _convertOperation.Source.TcStart)
                return string.Format(resources._validate_EndTCBeforeFile, _convertOperation.Source.TcStart.ToSMPTETimecodeString(_convertOperation.Source.VideoFormat));
            if (EndTC > _convertOperation.Source.TcLastFrame())
                return string.Format(resources._validate_EndTCAfterFile, _convertOperation.Source.TcLastFrame().ToSMPTETimecodeString(_convertOperation.Source.VideoFormat));
            return null;
        }

        private string ValidateDestFileName()
        {
            IMediaDirectory dir = _convertOperation.DestDirectory;
            if (dir != null)
            {
                if (_destMediaProperties.FileName.StartsWith(" ") || _destMediaProperties.FileName.EndsWith(" "))
                    return resources._validate_FileNameCanNotStartOrEndWithSpace;
                else if (_destMediaProperties.FileName.IndexOfAny(Path.GetInvalidFileNameChars()) > 0)
                    return resources._validate_FileNameCanNotContainSpecialCharacters;
                else
                {
                    var newName = _destMediaProperties.FileName.ToLowerInvariant();
                    if (dir.FileExists(newName, _destMediaProperties.Folder))
                        return resources._validate_FileAlreadyExists;
                    else
                    {
                        if (_destMediaProperties.MediaType == TMediaType.Movie
                            && !FileUtils.VideoFileTypes.Contains(Path.GetExtension(newName).ToLower()))
                            return string.Format(resources._validate_FileMustHaveExtension,
                                string.Join(resources._or_, FileUtils.VideoFileTypes));
                        if (_destMediaProperties.MediaType == TMediaType.Still
                            && !FileUtils.StillFileTypes.Contains(Path.GetExtension(newName).ToLower()))
                            return string.Format(resources._validate_FileMustHaveExtension,
                                string.Join(resources._or_, FileUtils.StillFileTypes));
                    }
                }
            }
            return null;
        }
    }
}
