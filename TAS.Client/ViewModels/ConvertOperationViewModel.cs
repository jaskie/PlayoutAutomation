using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.ComponentModel;
using TAS.Server.Common;
using TAS.Server.Common.Interfaces;
using resources = TAS.Client.Common.Properties.Resources;

namespace TAS.Client.ViewModels
{
    public class ConvertOperationViewModel: FileOperationViewmodel, IDataErrorInfo
    {
        private readonly IConvertOperation _convertOperation;
        private readonly PreviewViewmodel _previewVm;
        private readonly IMediaManager _mediaManager;

        private TMediaCategory _destCategory;
        private byte _destParental;
        private TAspectConversion _aspectConversion;
        private TAudioChannelMappingConversion _audioChannelMappingConversion;
        private decimal _audioVolume;
        private TFieldOrder _sourceFieldOrderEnforceConversion;
        private bool _trim;
        private bool _loudnessCheck;
        private string _destMediaName;
        private TimeSpan _startTC;
        private TimeSpan _duration;
        private string _idAux;
        private string _destFileName;
        private TMediaEmphasis _destMediaEmphasis;
        private TVideoFormat _destMediaVideoFormat;
        
        public ConvertOperationViewModel(IConvertOperation operation, IPreview preview, IMediaManager mediaManager)
            : base(operation)
        {
            _convertOperation = operation;
            _mediaManager = mediaManager;
            _destMediaName = operation.DestProperties.MediaName;
            _destFileName = operation.DestProperties.FileName;
            _destCategory = operation.DestProperties.MediaCategory;
            _destMediaEmphasis = operation.DestProperties is IPersistentMediaProperties ? ((IPersistentMediaProperties)operation.DestProperties).MediaEmphasis : TMediaEmphasis.None;
            _destParental = operation.DestProperties.Parental;
            _destMediaVideoFormat = operation.DestProperties.VideoFormat;

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
            {
                _previewVm = new PreviewViewmodel(preview) { Media = operation.Source };
                _previewVm.PropertyChanged += _previewVm_PropertyChanged;
            }
        }
        
        public Array Categories { get; } = Enum.GetValues(typeof(TMediaCategory));
        public TMediaCategory DestCategory { get { return _destCategory; } set { SetField(ref _destCategory, value); } }
        
        public IEnumerable<ICGElement> Parentals => _mediaManager?.CGElementsController?.Parentals;
        public byte DestParental { get { return _destParental; } set { SetField(ref _destParental, value); } }

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

        public decimal AudioVolume
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
            get { return _destMediaName; }
            set
            {
                if (SetField(ref _destMediaName, value))
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
            get { return _idAux; }
            set
            {
                if (SetField(ref _idAux, value))
                    _makeFileName();
            }
        }

        public string DestFileName { 
            get { return _destFileName; }
            set { SetField(ref _destFileName, value); }
        }
        
        public Array MediaEmphasises { get; } = Enum.GetValues(typeof(TMediaEmphasis));

        public TMediaEmphasis DestMediaEmphasis
        {
            get { return _destMediaEmphasis; }
            set { SetField(ref _destMediaEmphasis, value); }
        }

        public Array VideoFormats { get; } = Enum.GetValues(typeof(TVideoFormat));

        public TVideoFormat DestMediaVideoFormat
        {
            get { return _destMediaVideoFormat; }
            set { SetField(ref _destMediaVideoFormat, value); }
        }

        public bool ShowParentalCombo => _mediaManager?.CGElementsController?.Parentals != null;

        public bool CanTrim
        {
            get { return EncodeVideo && EncodeAudio && _convertOperation.Source.MediaStatus == TMediaStatus.Available && _convertOperation.Source.Duration > TimeSpan.Zero; }
        }

        public PreviewViewmodel PreviewViewmodel => _previewVm;

        public bool CanPreview
        {
            get { return (_previewVm != null && ((IIngestDirectory)_convertOperation.Source.Directory).AccessType == TDirectoryAccessType.Direct); }
        }

        public bool LoudnessCheck {
            get { return _loudnessCheck; }
            set { SetField(ref _loudnessCheck, value); }
        }
        
        public bool IsValid
        {
            get { return (from pi in this.GetType().GetProperties() select this[pi.Name]).Where(s => !string.IsNullOrEmpty(s)).Count() == 0; }
        }

        public bool IsMovie
        {
            get
            {
                var media = _convertOperation.DestProperties;
                return (media != null && media.MediaType == TMediaType.Movie);
            }
        }

        public bool IsStill
        {
            get
            {
                var media = _convertOperation.DestProperties;
                return (media != null && media.MediaType == TMediaType.Still);
            }
        }

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

            IMediaProperties newMediaProperties;
            if (_convertOperation.DestProperties is IPersistentMediaProperties)
                newMediaProperties = PersistentMediaProxy.FromMedia(_convertOperation.DestProperties as IPersistentMediaProperties);
            else
                newMediaProperties = MediaProxy.FromMedia(_convertOperation.DestProperties);

            newMediaProperties.MediaName = _destMediaName;
            if (newMediaProperties is IPersistentMediaProperties)
            {
                ((IPersistentMediaProperties)newMediaProperties).IdAux = _idAux;
                ((IPersistentMediaProperties)newMediaProperties).MediaEmphasis = _destMediaEmphasis;
            }
            newMediaProperties.VideoFormat = _destMediaVideoFormat;
            newMediaProperties.FileName = _destFileName;
            newMediaProperties.TcStart = _startTC;
            newMediaProperties.TcPlay = _startTC;
            newMediaProperties.Duration = _duration;
            newMediaProperties.DurationPlay = _duration;
            newMediaProperties.MediaCategory = _destCategory;
            newMediaProperties.Parental = _destParental;
            _convertOperation.DestProperties = newMediaProperties;  //required to pass this parameter from client to server application
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
            if (e.PropertyName == nameof(IConvertOperation.AspectConversion)
                || e.PropertyName == nameof(IConvertOperation.AudioChannelMappingConversion)
                || e.PropertyName == nameof(IConvertOperation.AudioVolume)
                || e.PropertyName == nameof(IConvertOperation.SourceFieldOrderEnforceConversion)
                || e.PropertyName == nameof(IConvertOperation.OperationOutput)
                || e.PropertyName == nameof(IConvertOperation.StartTC)
                || e.PropertyName == nameof(IConvertOperation.Duration)
            )
                NotifyPropertyChanged(e.PropertyName);
            else
                base.OnFileOperationPropertyChanged(sender, e);
        }

        protected virtual void OnSourceMediaPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IMedia.FileName))
                NotifyPropertyChanged(nameof(SourceFileName));
            if (e.PropertyName == nameof(IMedia.MediaStatus))
                NotifyPropertyChanged(nameof(CanTrim));
            if (e.PropertyName == nameof(IMedia.DurationPlay))
            {
                Duration = _convertOperation.Source.DurationPlay;
                NotifyPropertyChanged(nameof(CanTrim));
            }
            if (e.PropertyName == nameof(IMedia.TcPlay))
                StartTC = _convertOperation.Source.TcPlay;
        }

        protected override void OnDispose()
        {
            _convertOperation.Source.PropertyChanged -= OnSourceMediaPropertyChanged;
            if (_previewVm != null)
            {
                _previewVm.PropertyChanged -= _previewVm_PropertyChanged;
                _previewVm.Dispose();
            }
            base.OnDispose();
        }

        private RationalNumber SourceMediaFrameRate() => _convertOperation.Source.FrameRate();

        private void _makeFileName()
        {
            DestFileName = MediaExtensions.MakeFileName(IdAux, DestMediaName, FileUtils.DefaultFileExtension(_convertOperation.DestProperties.MediaType));
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
            IMediaProperties media = _convertOperation.DestProperties;
            if (media != null)
            {
                IMediaDirectory dir = _convertOperation.DestDirectory;
                if (dir != null)
                {
                    if (_destFileName.StartsWith(" ") || _destFileName.EndsWith(" "))
                        return resources._validate_FileNameCanNotStartOrEndWithSpace;
                    else
                    if (_destFileName.IndexOfAny(Path.GetInvalidFileNameChars()) > 0)
                        return resources._validate_FileNameCanNotContainSpecialCharacters;
                    else
                    {
                        var newName = _destFileName.ToLowerInvariant();
                        if (dir.FileExists(newName, media.Folder))
                            return resources._validate_FileAlreadyExists;
                        else
                        if (media is IPersistentMediaProperties)
                        {
                            if (media.MediaType == TMediaType.Movie
                                && !FileUtils.VideoFileTypes.Contains(Path.GetExtension(newName).ToLower()))
                                return string.Format(resources._validate_FileMustHaveExtension, string.Join(resources._or_, FileUtils.VideoFileTypes));
                            if (media.MediaType == TMediaType.Still
                                && !FileUtils.StillFileTypes.Contains(Path.GetExtension(newName).ToLower()))
                                return string.Format(resources._validate_FileMustHaveExtension, string.Join(resources._or_, FileUtils.StillFileTypes));
                        }
                    }
                }
            }
            return null;
        }

        private void _previewVm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_trim
                && _previewVm.LoadedMedia == _convertOperation.Source
                && (e.PropertyName == nameof(PreviewViewmodel.TcIn)
                    || e.PropertyName == nameof(PreviewViewmodel.TcOut)))
            {
                StartTC = _previewVm.TcIn;
                Duration = _previewVm.DurationSelection;
            }
        }

    }
}
