using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server;
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
            Array.Copy(_aspectConversions, _aspectConversionsEnforce, 3);
            if (preview != null)
            {
                _previewVm = new PreviewViewmodel(preview) { Media = operation.Source };
                _previewVm.PropertyChanged += _previewVm_PropertyChanged;
            }
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

        static readonly Array _categories = Enum.GetValues(typeof(TMediaCategory)); 
        public Array Categories { get { return _categories; } }
        private TMediaCategory _destCategory;
        public TMediaCategory DestCategory { get { return _destCategory; } set { SetField(ref _destCategory, value); } }

        
        public IEnumerable<ICGElement> Parentals { get { return _mediaManager?.CGElementsController?.Parentals; } }
        private byte _destParental;
        public byte DestParental { get { return _destParental; } set { SetField(ref _destParental, value); } }

        static readonly Array _aspectConversions = Enum.GetValues(typeof(TAspectConversion));
        public Array AspectConversions { get { return _aspectConversions; } }
        readonly Array _aspectConversionsEnforce = new TAspectConversion[3];
        public Array AspectConversionsEnforce { get { return _aspectConversionsEnforce; } }

        private TAspectConversion _aspectConversion;
        public TAspectConversion AspectConversion
        {
            get { return _aspectConversion; }
            set { SetField(ref _aspectConversion, value); }
        }

        static readonly Array _audioChannelMappingConversions = Enum.GetValues(typeof(TAudioChannelMappingConversion));
        public Array AudioChannelMappingConversions { get { return _audioChannelMappingConversions; } }
        private TAudioChannelMappingConversion _audioChannelMappingConversion;
        public TAudioChannelMappingConversion AudioChannelMappingConversion
        {
            get { return _audioChannelMappingConversion; }
            set { SetField(ref _audioChannelMappingConversion, value); }
        }

        private decimal _audioVolume;
        public decimal AudioVolume
        {
            get { return _audioVolume; }
            set { SetField(ref _audioVolume, value); }
        }

        static readonly Array _sourceFieldOrderEnforceConversions = Enum.GetValues(typeof(TFieldOrder));
        public Array SourceFieldOrderEnforceConversions { get { return _sourceFieldOrderEnforceConversions; } }
        private TFieldOrder _sourceFieldOrderEnforceConversion;
        public TFieldOrder SourceFieldOrderEnforceConversion
        {
            get { return _sourceFieldOrderEnforceConversion; }
            set { SetField(ref _sourceFieldOrderEnforceConversion, value); }
        }
    
        public bool EncodeVideo { get { return ((IIngestDirectory)_convertOperation.Source.Directory).VideoCodec != TVideoCodec.copy; } }
        public bool EncodeAudio { get { return ((IIngestDirectory)_convertOperation.Source.Directory).AudioCodec != TAudioCodec.copy; } }

        private bool _trim;
        public bool Trim { get { return _trim; } set { SetField(ref _trim, value); } }

        public string SourceFileName { get { return string.Format("{0}:{1}", _convertOperation.Source.Directory.DirectoryName, _convertOperation.Source.FileName); } }

        private string _destMediaName;
        public string DestMediaName
        {
            get { return _destMediaName; }
            set
            {
                if (SetField(ref _destMediaName, value))
                    _makeFileName();
            }
        }

        void _makeFileName()
        {
            List<string> filenameParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(IdAux))
                filenameParts.Add(IdAux);
            if (!string.IsNullOrWhiteSpace(DestMediaName))
                filenameParts.Add(DestMediaName);
            DestFileName = FileUtils.SanitizeFileName(string.Join(" ", filenameParts)) + FileUtils.DefaultFileExtension(_convertOperation.DestProperties.MediaType);
        }

        private TimeSpan _startTC;
        public TimeSpan StartTC
        {
            get { return _startTC; }
            set
            {
                if (SetField(ref _startTC, value))
                    NotifyPropertyChanged(nameof(EndTC));
            }
        }

        private TimeSpan _duration;
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
            get { return ((_startTC + _duration).ToSMPTEFrames(SourceMediaFrameRate) - 1).SMPTEFramesToTimeSpan(SourceMediaFrameRate); }
            set
            {
                var end = ((value - StartTC).ToSMPTEFrames(SourceMediaFrameRate) + 1).SMPTEFramesToTimeSpan(SourceMediaFrameRate);
                if (SetField(ref _duration, end))
                    NotifyPropertyChanged(nameof(Duration));
            }
        }


        string _idAux;
        public string IdAux
        {
            get { return _idAux; }
            set
            {
                if (SetField(ref _idAux, value))
                    _makeFileName();
            }
        }

        string _destFileName;
        public string DestFileName { 
            get { return _destFileName; }
            set { SetField(ref _destFileName, value); }
        }


        static readonly Array _mediaEmphasises = Enum.GetValues(typeof(TMediaEmphasis));
        public Array MediaEmphasises { get { return _mediaEmphasises; } }

        private TMediaEmphasis _destMediaEmphasis;
        public TMediaEmphasis DestMediaEmphasis
        {
            get { return _destMediaEmphasis; }
            set { SetField(ref _destMediaEmphasis, value); }
        }

        static readonly Array _videoFormats = Enum.GetValues(typeof(TVideoFormat));
        public Array VideoFormats { get { return _videoFormats; } }

        private TVideoFormat _destMediaVideoFormat;
        public TVideoFormat DestMediaVideoFormat
        {
            get { return _destMediaVideoFormat; }
            set { SetField(ref _destMediaVideoFormat, value); }
        }

        public bool ShowParentalCombo { get { return _mediaManager?.CGElementsController?.Parentals != null; } }

        public bool CanTrim
        {
            get { return EncodeVideo && EncodeAudio && _convertOperation.Source.MediaStatus == TMediaStatus.Available && _convertOperation.Source.Duration > TimeSpan.Zero; }
        }

        public PreviewViewmodel PreviewViewmodel { get { return _previewVm; } }

        public bool CanPreview
        {
            get { return (_previewVm != null && ((IIngestDirectory)_convertOperation.Source.Directory).AccessType == TDirectoryAccessType.Direct); }
        }

        bool _loudnessCheck;
        public bool LoudnessCheck {
            get { return _loudnessCheck; }
            set { SetField(ref _loudnessCheck, value); }
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

        internal RationalNumber SourceMediaFrameRate { get { return _convertOperation.Source.FrameRate(); } }

        public string Error
        {
            get { throw new NotImplementedException(); }
        }
    }
}
