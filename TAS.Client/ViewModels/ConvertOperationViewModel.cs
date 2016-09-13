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
            _destMediaName = operation.DestMedia.MediaName;
            _destFileName = operation.DestMedia.FileName;
            _duration = operation.Duration;
            _startTC = operation.StartTC;
            _trim = operation.Trim;
            _loudnessCheck = operation.LoudnessCheck;
            operation.SourceMedia.PropertyChanged += OnSourceMediaPropertyChanged;
            operation.DestMedia.PropertyChanged += OnDestMediaPropertyChanged;
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
            _convertOperation.DestMedia.PropertyChanged -= OnDestMediaPropertyChanged;
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
        public TMediaCategory DestCategory { get { return _convertOperation.DestMedia.MediaCategory; } set { _convertOperation.DestMedia.MediaCategory = value; } }

        public bool ShowParentalCombo { get { return _mediaManager?.CGElementsController?.Parentals != null; } }
        public IEnumerable<ICGElement> Parentals { get { return _mediaManager?.CGElementsController?.Parentals; } }
        public byte DestParental { get { return _convertOperation.DestMedia.Parental; } set { _convertOperation.DestMedia.Parental = value; } }

        static readonly Array _mediaEmphasises = Enum.GetValues(typeof(TMediaEmphasis));
        public Array MediaEmphasises { get { return _mediaEmphasises; } }
        public TMediaEmphasis DestMediaEmphasis { 
            get { return _convertOperation.DestMedia is IPersistentMedia ? ((IPersistentMedia)_convertOperation.DestMedia).MediaEmphasis : TMediaEmphasis.None; }
            set { if (_convertOperation.DestMedia is IPersistentMedia) ((IPersistentMedia)_convertOperation.DestMedia).MediaEmphasis = value; }
        }



        static readonly Array _videoFormats = Enum.GetValues(typeof(TVideoFormat));
        public Array VideoFormats { get { return _videoFormats; } }

        public TVideoFormat DestMediaVideoFormat
        {
            get { return _convertOperation.DestMedia.VideoFormat; }
            set { _convertOperation.DestMedia.VideoFormat = value; }
        }

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
        public TAudioChannelMappingConversion AudioChannelMappingConversion
        {
            get { return _convertOperation.AudioChannelMappingConversion; }
            set { _convertOperation.AudioChannelMappingConversion = value; }
        }
        public decimal AudioVolume
        {
            get { return _convertOperation.AudioVolume; }
            set { _convertOperation.AudioVolume = value; }
        }

        public string IdAux { get { return _convertOperation.IdAux; } set { _convertOperation.IdAux = value; } }
        static readonly Array _sourceFieldOrderEnforceConversions = Enum.GetValues(typeof(TFieldOrder));
        public Array SourceFieldOrderEnforceConversions { get { return _sourceFieldOrderEnforceConversions; } }
        public TFieldOrder SourceFieldOrderEnforceConversion
        {
            get { return _convertOperation.SourceFieldOrderEnforceConversion; }
            set { _convertOperation.SourceFieldOrderEnforceConversion = value; }
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
            if (!string.IsNullOrWhiteSpace(DestExternalId))
                filenameParts.Add(DestExternalId);
            if (!string.IsNullOrWhiteSpace(DestMediaName))
                filenameParts.Add(DestMediaName);
            DestFileName = FileUtils.SanitizeFileName(string.Join(" ", filenameParts)) + FileUtils.DefaultFileExtension(_convertOperation.DestMedia.MediaType);
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


        string _destExternalId;
        public string DestExternalId
        {
            get { return _destExternalId; }
            set
            {
                if (SetField(ref _destExternalId, value, nameof(DestExternalId)))
                    _makeFileName();
            }
        }

        string _destFileName;
        public string DestFileName { 
            get { return _destFileName; }
            set { SetField(ref _destFileName, value, nameof(DestFileName)); }
        }

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
            IMedia media = _convertOperation.DestMedia;
            if (media != null)
            {
                IMediaDirectory dir = media.Directory;
                if (dir != null && media.FileName != null)
                {
                    if (_destFileName.IndexOfAny(Path.GetInvalidFileNameChars()) > 0)
                        validationResult = resources._validate_FileNameCanNotContainSpecialCharacters;
                    else
                    {
                        var newName = _destFileName.ToLowerInvariant();
                        if ((media.MediaStatus == TMediaStatus.Required)
                            && dir.FileExists(newName, media.Folder))
                            validationResult = resources._validate_FileAlreadyExists;
                        else
                            if (media is IPersistentMedia)
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

        protected virtual void OnDestMediaPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IMedia.FileName))
            {
                NotifyPropertyChanged(nameof(DestFileName));
                NotifyPropertyChanged(nameof(IsValid));
            }
            if (e.PropertyName == nameof(IMedia.MediaName))
                NotifyPropertyChanged(nameof(DestMediaName));
            if (e.PropertyName == nameof(IPersistentMedia.MediaEmphasis))
                NotifyPropertyChanged(nameof(DestMediaEmphasis));
            if (e.PropertyName == nameof(IPersistentMedia.MediaCategory))
                NotifyPropertyChanged(nameof(DestCategory));
            if (e.PropertyName == nameof(IPersistentMedia.Parental))
                NotifyPropertyChanged(nameof(DestParental));
            if (e.PropertyName == nameof(IPersistentMedia.MediaType))
                NotifyPropertyChanged(nameof(IsMovie));
            if (e.PropertyName == nameof(IPersistentMedia.VideoFormat))
                NotifyPropertyChanged(nameof(DestMediaVideoFormat));
        }

        public bool IsValid
        {
            get { return (from pi in this.GetType().GetProperties() select this[pi.Name]).Where(s => !string.IsNullOrEmpty(s)).Count() == 0; }
        }

        public bool IsMovie
        {
            get
            {
                var media = _convertOperation.DestMedia;
                return (media != null && media.MediaType == TMediaType.Movie);
            }
        }
        public bool IsStill
        {
            get
            {
                var media = _convertOperation.DestMedia;
                return (media != null && media.MediaType == TMediaType.Still);
            }
        }

        public void Apply()
        {
            _convertOperation.DestMedia.MediaName = _destMediaName;
            if (_convertOperation.DestMedia is IPersistentMedia)
                ((IPersistentMedia)_convertOperation.DestMedia).IdAux = _destExternalId;
            _convertOperation.DestMedia.FileName = _destFileName;
            _convertOperation.StartTC = _startTC;
            _convertOperation.Duration = _duration;
            _convertOperation.DestMedia.TcStart = _startTC;
            _convertOperation.DestMedia.TcPlay = _startTC;
            _convertOperation.DestMedia.Duration = _duration;
            _convertOperation.DestMedia.DurationPlay = _duration;
            _convertOperation.Trim = _trim;
            _convertOperation.LoudnessCheck = _loudnessCheck;
        }

        internal RationalNumber SourceMediaFrameRate { get { return _convertOperation.SourceMedia.FrameRate; } }

        public string Error
        {
            get { throw new NotImplementedException(); }
        }
    }
}
