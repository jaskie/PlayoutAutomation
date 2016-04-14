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
        public ConvertOperationViewModel(IConvertOperation operation)
            : base(operation)
        {
            _convertOperation = operation;
            _destMediaName = operation.DestMedia.MediaName;
            _destFileName = operation.DestMedia.FileName;
            _duration = operation.Duration;
            _startTC = operation.StartTC;
            _trim = operation.Trim;
            operation.SourceMedia.PropertyChanged += OnSourceMediaPropertyChanged;
            operation.DestMedia.PropertyChanged += OnDestMediaPropertyChanged;
            Array.Copy(_aspectConversions, _aspectConversionsEnforce, 3);
        }

        protected override void OnDispose()
        {
            _convertOperation.SourceMedia.PropertyChanged -= OnSourceMediaPropertyChanged;
            _convertOperation.DestMedia.PropertyChanged -= OnDestMediaPropertyChanged;
            base.OnDispose();
        }
                
        static readonly Array _categories = Enum.GetValues(typeof(TMediaCategory)); 
        public Array Categories { get { return _categories; } }
        public TMediaCategory DestCategory { get { return _convertOperation.DestMedia.MediaCategory; } set { _convertOperation.DestMedia.MediaCategory = value; } }

        static readonly Array _parentals = Enum.GetValues(typeof(TParental));
        public Array Parentals{ get { return _parentals; } }
        public TParental DestParental { get { return _convertOperation.DestMedia.Parental; } set { _convertOperation.DestMedia.Parental = value; } }

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
    
        public bool DoNotEncode { get { return ((IIngestDirectory)_convertOperation.SourceMedia.Directory).DoNotEncode; } }

        private bool _trim;
        public bool Trim { get { return _trim; } set { SetField(ref _trim, value, "Trim"); } }

        public string SourceFileName { get { return string.Format("{0}:{1}", _convertOperation.SourceMedia.Directory.DirectoryName, _convertOperation.SourceMedia.FileName); } }

        private string _destMediaName;
        public string DestMediaName
        {
            get { return _destMediaName; }
            set
            {
                if (SetField(ref _destMediaName, value, "DestMediaName"))
                    DestFileName = FileUtils.SanitizeFileName(value) + FileUtils.DefaultFileExtension(_convertOperation.DestMedia.MediaType);
            }
        }

        private TimeSpan _startTC;
        public TimeSpan StartTC
        {
            get { return _startTC; }
            set { SetField(ref _startTC, value, "StartTC"); }
        }

        private TimeSpan _duration;
        public TimeSpan Duration
        {
            get { return _duration; }
            set { SetField(ref _duration, value, "Duration"); }
        }


        string _destFileName;
        public string DestFileName { 
            get { return _destFileName; }
            set { SetField(ref _destFileName, value, "DestFileName"); }
        }

        public bool CanTrim
        {
            get { return !DoNotEncode && _convertOperation.SourceMedia.MediaStatus == TMediaStatus.Available && _convertOperation.SourceMedia.Duration > TimeSpan.Zero; }
        }
        
        public string this[string propertyName]
        {
            get
            {
                string validationResult = null;
                switch (propertyName)
                {
                    case "DestFileName":
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
        
        protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "AspectConversion"
                || e.PropertyName == "AudioChannelMappingConversion"
                || e.PropertyName == "AudioVolume"
                || e.PropertyName == "SourceFieldOrderEnforceConversion"
                || e.PropertyName == "OperationOuput"
                || e.PropertyName == "StartTC"
                || e.PropertyName == "Duration"
                )
                NotifyPropertyChanged(e.PropertyName);
            else
                base.OnPropertyChanged(sender, e);
        }

        protected virtual void OnSourceMediaPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FileName")
                NotifyPropertyChanged("SourceFileName");
            if (e.PropertyName == "MediaStatus")
                NotifyPropertyChanged("CanTrim");
            if (e.PropertyName == "DurationPlay")
            {
                Duration = _convertOperation.SourceMedia.DurationPlay;
                NotifyPropertyChanged("CanTrim");
            }
            if (e.PropertyName == "TCPlay")
                StartTC = _convertOperation.SourceMedia.TcPlay;
        }

        protected virtual void OnDestMediaPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FileName")
            {
                NotifyPropertyChanged("DestFileName");
                NotifyPropertyChanged("IsValid");
            }
            if (e.PropertyName == "MediaName")
                NotifyPropertyChanged("DestMediaName");
            if (e.PropertyName == "MediaEmphasis")
                NotifyPropertyChanged("DestMediaEmphasis");
            if (e.PropertyName == "MediaCategory")
                NotifyPropertyChanged("DestCategory");
            if (e.PropertyName == "Parental")
                NotifyPropertyChanged("DestParental");
            if (e.PropertyName == "MediaType")
                NotifyPropertyChanged("IsMovie");
            if (e.PropertyName == "VideoFormat")
                NotifyPropertyChanged("DestMediaVideoFormat");
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
            _convertOperation.DestMedia.FileName = _destFileName;
            _convertOperation.StartTC = _startTC;
            _convertOperation.Duration = _duration;
            _convertOperation.Trim = _trim;
        }

        internal RationalNumber SourceMediaFrameRate { get { return _convertOperation.SourceMedia.FrameRate; } }

        public string Error
        {
            get { throw new NotImplementedException(); }
        }
    }
}
