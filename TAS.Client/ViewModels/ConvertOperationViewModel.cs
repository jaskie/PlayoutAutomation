using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server;
using System.IO;
using System.ComponentModel;
using TAS.Common;
using TAS.Server.Interfaces;
using TAS.Server.Common;
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
            _destMediaName = operation.DestMediaProperties.MediaName;
            _destFileName = operation.DestMediaProperties.FileName;
            _destCategory = operation.DestMediaProperties.MediaCategory;
            _destMediaEmphasis = operation.DestMediaProperties is IPersistentMediaProperties ? ((IPersistentMediaProperties)operation.DestMediaProperties).MediaEmphasis : TMediaEmphasis.None;
            _destParental = operation.DestMediaProperties.Parental;
            _destMediaVideoFormat = operation.DestMediaProperties.VideoFormat;
            _duration = operation.Duration;
            _startTC = operation.StartTC;
            _trim = operation.Trim;
            _loudnessCheck = operation.LoudnessCheck;
            operation.SourceMedia.PropertyChanged += OnSourceMediaPropertyChanged;
            Array.Copy(_aspectConversions, _aspectConversionsEnforce, 3);
            if (preview != null)
            {
                _previewVm = new PreviewViewmodel(preview) { Media = operation.SourceMedia };
                _previewVm.PropertyChanged += _previewVm_PropertyChanged;
            }
        }

        protected override void OnDispose()
        {
            _convertOperation.SourceMedia.PropertyChanged -= OnSourceMediaPropertyChanged;
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
                && _previewVm.LoadedMedia == _convertOperation.SourceMedia
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
        public TMediaCategory DestCategory { get { return _destCategory; } set { SetField(ref _destCategory, value, nameof(DestCategory)); } }

        
        public IEnumerable<ICGElement> Parentals { get { return _mediaManager?.CGElementsController?.Parentals; } }
        private byte _destParental;
        public byte DestParental { get { return _destParental; } set { SetField(ref _destParental, value, nameof(DestParental)); } }

        static readonly Array _aspectConversions = Enum.GetValues(typeof(TAspectConversion));
        public Array AspectConversions { get { return _aspectConversions; } }
        readonly Array _aspectConversionsEnforce = new TAspectConversion[3];
        public Array AspectConversionsEnforce { get { return _aspectConversionsEnforce; } }

        public TAspectConversion AspectConversion
        {
            get { return _convertOperation.AspectConversion; }
            set { _convertOperation.AspectConversion = value; }
        }

        static readonly Array _audioChannelMappingConversions = Enum.GetValues(typeof(TAudioChannelMappingConversion));
        public Array AudioChannelMappingConversions { get { return _audioChannelMappingConversions; } }
        private TAudioChannelMappingConversion _audioChannelMappingConversion;
        public TAudioChannelMappingConversion AudioChannelMappingConversion
        {
            get { return _audioChannelMappingConversion; }
            set { SetField(ref _audioChannelMappingConversion, value, nameof(AudioChannelMappingConversion)); }
        }

        private decimal _audioVolume;
        public decimal AudioVolume
        {
            get { return _audioVolume; }
            set { SetField(ref _audioVolume, value, nameof(AudioVolume)); }
        }

        static readonly Array _sourceFieldOrderEnforceConversions = Enum.GetValues(typeof(TFieldOrder));
        public Array SourceFieldOrderEnforceConversions { get { return _sourceFieldOrderEnforceConversions; } }
        private TFieldOrder _sourceFieldOrderEnforceConversion;
        public TFieldOrder SourceFieldOrderEnforceConversion
        {
            get { return _sourceFieldOrderEnforceConversion; }
            set { SetField(ref _sourceFieldOrderEnforceConversion, value, nameof(SourceFieldOrderEnforceConversion)); }
        }
    
        public bool EncodeVideo { get { return ((IIngestDirectory)_convertOperation.SourceMedia.Directory).VideoCodec != TVideoCodec.copy; } }
        public bool EncodeAudio { get { return ((IIngestDirectory)_convertOperation.SourceMedia.Directory).AudioCodec != TAudioCodec.copy; } }

        private bool _trim;
        public bool Trim { get { return _trim; } set { SetField(ref _trim, value, nameof(Trim)); } }

        public string SourceFileName { get { return string.Format("{0}:{1}", _convertOperation.SourceMedia.Directory.DirectoryName, _convertOperation.SourceMedia.FileName); } }

        private string _destMediaName;
        public string DestMediaName
        {
            get { return _destMediaName; }
            set
            {
                if (SetField(ref _destMediaName, value, nameof(DestMediaName)))
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
            DestFileName = FileUtils.SanitizeFileName(string.Join(" ", filenameParts)) + FileUtils.DefaultFileExtension(_convertOperation.DestMediaProperties.MediaType);
        }

        private TimeSpan _startTC;
        public TimeSpan StartTC
        {
            get { return _startTC; }
            set
            {
                if (SetField(ref _startTC, value, nameof(StartTC)))
                    NotifyPropertyChanged(nameof(EndTC));
            }
        }

        private TimeSpan _duration;
        public TimeSpan Duration
        {
            get { return _duration; }
            set
            {
                if (SetField(ref _duration, value, nameof(Duration)))
                    NotifyPropertyChanged(nameof(EndTC));
            }
        }

        public TimeSpan EndTC
        {
            get { return _startTC+_duration; }
            set
            {
                if (SetField(ref _duration, value - StartTC, nameof(EndTC)))
                    NotifyPropertyChanged(nameof(Duration));
            }
        }


        string _idAux;
        public string IdAux
        {
            get { return _idAux; }
            set
            {
                if (SetField(ref _idAux, value, nameof(IdAux)))
                    _makeFileName();
            }
        }

        string _destFileName;
        public string DestFileName { 
            get { return _destFileName; }
            set { SetField(ref _destFileName, value, nameof(DestFileName)); }
        }


        static readonly Array _mediaEmphasises = Enum.GetValues(typeof(TMediaEmphasis));
        public Array MediaEmphasises { get { return _mediaEmphasises; } }

        private TMediaEmphasis _destMediaEmphasis;
        public TMediaEmphasis DestMediaEmphasis
        {
            get { return _destMediaEmphasis; }
            set { SetField(ref _destMediaEmphasis, value, nameof(DestMediaEmphasis)); }
        }

        static readonly Array _videoFormats = Enum.GetValues(typeof(TVideoFormat));
        public Array VideoFormats { get { return _videoFormats; } }

        private TVideoFormat _destMediaVideoFormat;
        public TVideoFormat DestMediaVideoFormat
        {
            get { return _destMediaVideoFormat; }
            set { SetField(ref _destMediaVideoFormat, value, nameof(DestMediaVideoFormat)); }
        }

        public bool ShowParentalCombo { get { return _mediaManager?.CGElementsController?.Parentals != null; } }

        public bool CanTrim
        {
            get { return EncodeVideo && EncodeAudio && _convertOperation.SourceMedia.MediaStatus == TMediaStatus.Available && _convertOperation.SourceMedia.Duration > TimeSpan.Zero; }
        }

        public PreviewViewmodel Preview { get { return _previewVm; } }
        public Views.PreviewView View { get { return _previewVm.View;  } }

        public bool CanPreview
        {
            get { return (_previewVm != null && ((IIngestDirectory)_convertOperation.SourceMedia.Directory).AccessType == TDirectoryAccessType.Direct); }
        }

        bool _loudnessCheck;
        public bool LoudnessCheck {
            get { return _loudnessCheck; }
            set { SetField(ref _loudnessCheck, value, nameof(LoudnessCheck)); }
        }


        public string this[string propertyName]
        {
            get
            {
                string validationResult = null;
                switch (propertyName)
                {
                    case nameof(DestFileName):
                        validationResult = _validateDestFileName();
                        break;
                }
                return validationResult;
            }
        }

        private string _validateDestFileName()
        {
            string validationResult = string.Empty;
            IMediaProperties media = _convertOperation.DestMediaProperties;
            if (media != null)
            {
                IMediaDirectory dir = _convertOperation.DestDirectory;
                if (dir != null)
                {
                    if (_destFileName.StartsWith(" ") || _destFileName.EndsWith(" "))
                        validationResult = resources._validate_FileNameCanNotStartOrEndWithSpace;
                    else
                    if (_destFileName.IndexOfAny(Path.GetInvalidFileNameChars()) > 0)
                        validationResult = resources._validate_FileNameCanNotContainSpecialCharacters;
                    else
                    {
                        var newName = _destFileName.ToLowerInvariant();
                        if (dir.FileExists(newName, media.Folder))
                            validationResult = resources._validate_FileAlreadyExists;
                        else
                            if (media is IPersistentMediaProperties)
                            {
                                if (media.MediaType == TMediaType.Movie
                                    && !FileUtils.VideoFileTypes.Contains(Path.GetExtension(newName).ToLower()))
                                    validationResult = string.Format(resources._validate_FileMustHaveExtension, string.Join(resources._or_, FileUtils.VideoFileTypes));
                                if (media.MediaType == TMediaType.Still
                                    && !FileUtils.StillFileTypes.Contains(Path.GetExtension(newName).ToLower()))
                                    validationResult = string.Format(resources._validate_FileMustHaveExtension, string.Join(resources._or_, FileUtils.StillFileTypes));
                            }
                    }
                }
            }
            return validationResult;
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
                Duration = _convertOperation.SourceMedia.DurationPlay;
                NotifyPropertyChanged(nameof(CanTrim));
            }
            if (e.PropertyName == nameof(IMedia.TcPlay))
                StartTC = _convertOperation.SourceMedia.TcPlay;
        }

        public bool IsValid
        {
            get { return (from pi in this.GetType().GetProperties() select this[pi.Name]).Where(s => !string.IsNullOrEmpty(s)).Count() == 0; }
        }

        public bool IsMovie
        {
            get
            {
                var media = _convertOperation.DestMediaProperties;
                return (media != null && media.MediaType == TMediaType.Movie);
            }
        }
        public bool IsStill
        {
            get
            {
                var media = _convertOperation.DestMediaProperties;
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

            _convertOperation.DestMediaProperties.MediaName = _destMediaName;
            if (_convertOperation.DestMediaProperties is IPersistentMediaProperties)
            {
                ((IPersistentMediaProperties)_convertOperation.DestMediaProperties).IdAux = _idAux;
                ((IPersistentMediaProperties)_convertOperation.DestMediaProperties).MediaEmphasis = _destMediaEmphasis;
            }
            _convertOperation.DestMediaProperties.VideoFormat = _destMediaVideoFormat;
            _convertOperation.DestMediaProperties.FileName = _destFileName;
            _convertOperation.DestMediaProperties.TcStart = _startTC;
            _convertOperation.DestMediaProperties.TcPlay = _startTC;
            _convertOperation.DestMediaProperties.Duration = _duration;
            _convertOperation.DestMediaProperties.DurationPlay = _duration;
            _convertOperation.DestMediaProperties.MediaCategory = _destCategory;
            _convertOperation.DestMediaProperties.Parental = _destParental;
        }

        internal RationalNumber SourceMediaFrameRate { get { return _convertOperation.SourceMedia.FrameRate; } }

        public string Error
        {
            get { throw new NotImplementedException(); }
        }
    }
}
