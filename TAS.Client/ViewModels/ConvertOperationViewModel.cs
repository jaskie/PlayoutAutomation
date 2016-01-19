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

        public string SourceFileName { get { return string.Format("{0}:{1}", _convertOperation.SourceMedia.Directory.DirectoryName, _convertOperation.SourceMedia.FileName); } }

        public string DestMediaName
        {
            get { return _convertOperation.DestMedia.MediaName; }
            set
            {
                if (_convertOperation.DestMedia.MediaName != value)
                {
                    _convertOperation.DestMedia.MediaName = value;
                    _convertOperation.DestMedia.FileName = FileUtils.SanitizeFileName(value) + FileUtils.DefaultFileExtension(_convertOperation.DestMedia.MediaType);
                }
            }
        }
        
        public string DestFileName { 
            get { return _convertOperation.DestMedia.FileName; }
            set { _convertOperation.DestMedia.FileName = value; }
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
                string newName = media.FileName;
                if (dir != null && media.FileName != null)
                {
                    if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) > 0)
                        validationResult = resources._validate_FileNameCanNotContainSpecialCharacters;
                    else
                    {
                        newName = newName.ToLowerInvariant();
                        if ((media.MediaStatus == TMediaStatus.Required || newName != media.FileName.ToLowerInvariant())
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
                )
                NotifyPropertyChanged(e.PropertyName);
            else
                base.OnPropertyChanged(sender, e);
        }

        protected virtual void OnSourceMediaPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FileName")
                NotifyPropertyChanged("SourceFileName");
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
        }

        public bool IsValid
        {
            get { return (from pi in this.GetType().GetProperties() select this[pi.Name]).Where(s => !string.IsNullOrEmpty(s)).Count() == 0; }
        }

        public string Error
        {
            get { throw new NotImplementedException(); }
        }
    }
}
