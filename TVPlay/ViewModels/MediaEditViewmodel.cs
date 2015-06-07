using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using TAS.Server;
using System.Windows;
using System.IO;
using System.Windows.Input;
using TAS.Common;

namespace TAS.Client.ViewModels
{
    public class MediaEditViewmodel : ViewmodelBase, IDataErrorInfo
    {
        private readonly PreviewViewmodel _previewVm;
        public MediaEditViewmodel(PreviewViewmodel previewVm)
        {
            CommandSaveEdit = new SimpleCommand() { ExecuteDelegate = _save, CanExecuteDelegate = o => Modified && IsValid };
            CommandCancelEdit = new SimpleCommand() { ExecuteDelegate = _load, CanExecuteDelegate = o => Modified };
            CommandRefreshStatus = new SimpleCommand() { ExecuteDelegate = _refreshStatus, CanExecuteDelegate = o => _media != null };
            CommandGetTCFromPreview = new SimpleCommand() { ExecuteDelegate = _getTCFromPreview, CanExecuteDelegate = _canGetTCFormPreview };
            _previewVm = previewVm;
            if (previewVm != null)
                previewVm.PropertyChanged += _onPreviewPropertyChanged;
            Modified = false;
        }

        protected override void OnDispose()
        {
            if (_media != null)
                Media = null;
            if (_previewVm != null)
                _previewVm.PropertyChanged -= _onPreviewPropertyChanged;
        }

        public ICommand CommandSaveEdit { get; private set; }
        public ICommand CommandCancelEdit { get; private set; }
        public ICommand CommandRefreshStatus { get; private set; }
        public ICommand CommandGetTCFromPreview { get; private set; }

        private Media _media;
        internal Media Media
        {
            get
            {
                return _media;
            }
            set
            {
                if (value != _media)
                {
                    if (Modified
                        && (IsAutoSave || MessageBox.Show(Properties.Resources._query_SaveChangedData, Properties.Resources._caption_Confirmation, MessageBoxButton.YesNo) == MessageBoxResult.Yes))
                        Save();
                    if (_media != null)
                        _media.PropertyChanged -= OnMediaPropertyChanged;

                    _media = value;

                    if (_media != null)
                        _media.PropertyChanged += OnMediaPropertyChanged;
                    Load();
                }
            }
        }

        public void Save() { _save(null); }

        void _save(object o)
        {
            if (Modified && _media != null)
            {
                PropertyInfo[] copiedProperties = this.GetType().GetProperties();
                foreach (PropertyInfo copyPi in copiedProperties)
                {
                    PropertyInfo destPi = _media.GetType().GetProperty(copyPi.Name);
                    if (destPi != null)
                    {
                        var destVal = destPi.GetValue(_media, null);
                        if (destVal != null && !destVal.Equals(copyPi.GetValue(this, null)))
                            destPi.SetValue(_media, copyPi.GetValue(this, null), null);
                    }
                }
                if (_media is PersistentMedia)
                    ((PersistentMedia)_media).Save();
                Modified = false;
                Load();
            }
        }

        void _load(object o)
        {
            if (_media != null)
            {
                IEnumerable<PropertyInfo> copiedProperties = this.GetType().GetProperties().Where(pi => pi.CanWrite);
                foreach (PropertyInfo copyPi in copiedProperties)
                {
                    PropertyInfo sourcePi = _media.GetType().GetProperty(copyPi.Name);
                    if (sourcePi != null)
                        copyPi.SetValue(this, sourcePi.GetValue(_media, null), null);
                }
            }
            else // _media is null
            {
                IEnumerable<PropertyInfo> zeroedProperties = this.GetType().GetProperties().Where(pi => pi.CanWrite);
                foreach (PropertyInfo zeroPi in zeroedProperties)
                {
                    PropertyInfo sourcePi = typeof(Media).GetProperty(zeroPi.Name);
                    if (sourcePi != null)
                        zeroPi.SetValue(this, null, null);
                }
            }
            Modified = false;
            NotifyPropertyChanged(null);
        }


        public void Load() { _load(null); }

        void _refreshStatus(object o)
        {
            Media m = _media;
            if (m != null)
            {
                m.MediaStatus = TMediaStatus.Unknown;
                m.Verified = false;
                m.Verify();
            }
        }


        void _getTCFromPreview(object o)
        {
            Media media = _media;
            if (_previewVm != null && media != null)
            {
                Media previewMedia = _previewVm.LoadedMedia;
                TCPlay = _previewVm.TCIn;
                DurationPlay = _previewVm.DurationSelection;
            }
        }
        
        private void OnMediaPropertyChanged(object media, PropertyChangedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action) (() =>
                {
                    if (media == _media && !string.IsNullOrEmpty(e.PropertyName))
                    {
                        PropertyInfo sourcePi = _media.GetType().GetProperty(e.PropertyName);
                        PropertyInfo destPi = this.GetType().GetProperty(e.PropertyName);
                        if (sourcePi != null 
                            && destPi != null
                            && sourcePi.CanRead
                            && destPi.CanWrite)
                        {
                            bool oldModified = Modified;
                            destPi.SetValue(this, sourcePi.GetValue(_media, null), null);
                            Modified = oldModified;
                            NotifyPropertyChanged(e.PropertyName);
                        }
                        if (e.PropertyName == "MediaStatus")
                        {
                            NotifyPropertyChanged("IsIngestDataShown");
                            NotifyPropertyChanged("MediaStatus");
                        }
                        if (e.PropertyName == "MediaGuid")
                        {
                            NotifyPropertyChanged("MediaGuid");
                        }
                    }
                }),
            null);
        }

        private void _onPreviewPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "LoadedMedia")
                NotifyPropertyChanged("CommandGetTCFromPreview");
        }

        public void Delete()
        {
            if (_media != null)
                _media.Delete();
        }

        private bool _modified;
        public bool Modified { get {return _modified;}
            private set
            {
                if (_modified != value)
                {
                    _modified = value;

                }
                NotifyPropertyChanged("CommandCancelEdit");
                NotifyPropertyChanged("CommandSaveEdit");
            }
        }
        private string _folder;
        public string Folder
        {
            get { return _folder; }
            set
            {
                if (value != _folder)
                {
                    _folder = value;
                    Modified = true;
                }
            }
        }
        private string _fileName;
        public string FileName 
        {
            get { return _fileName; }
            set
            {
                if (SetField(ref _fileName, value, "FileName"))
                    NotifyPropertyChanged("IsValid");
            }
        }

        private DateTime _lastUpdated;
        public DateTime LastUpdated
        {
            get { return _lastUpdated; }
            set { SetField(ref _lastUpdated, value, "LastUpdated"); }
        }

        private DateTime _lastAccess;
        public DateTime LastAccess
        {
            get { return _lastAccess; }
            set { SetField(ref _lastAccess, value, "LastAccess"); }
        }

        private TMediaType _mediaType;
        public TMediaType MediaType
        {
            get { return _mediaType; }
            set { SetField(ref _mediaType, value, "MediaType"); }
        }

        private TimeSpan _duration;
        public TimeSpan Duration
        {
            get { return _duration; }
            set { SetField(ref _duration, value, "Duration"); }
        }

        private TimeSpan _durationPlay;
        public TimeSpan DurationPlay
        {
            get { return _durationPlay; }
            set { SetField(ref _durationPlay, value, "DurationPlay"); }
        }

        private TimeSpan _tCStart;
        public TimeSpan TCStart
        {
            get { return _tCStart; }
            set { SetField(ref _tCStart, value, "TCStart"); }
        }

        private TimeSpan _tCPlay;
        public TimeSpan TCPlay
        {
            get { return _tCPlay; }
            set { SetField(ref _tCPlay, value, "TCPlay"); }
        }

        readonly Array _videoFormats = Enum.GetValues(typeof(TVideoFormat));
        public Array VideoFormats { get { return _videoFormats; } }
        private TVideoFormat _videoFormat;
        public TVideoFormat VideoFormat
        {
            get { return _videoFormat; }
            set { SetField(ref _videoFormat, value, "VideoFormat"); }
        }

        readonly Array _audioChannelMappings = Enum.GetValues(typeof(TAudioChannelMapping)); 
        public Array AudioChannelMappings { get { return _audioChannelMappings;} }
        private TAudioChannelMapping _audioChannelMapping;
        public TAudioChannelMapping AudioChannelMapping
        {
            get { return _audioChannelMapping; }
            set { SetField(ref _audioChannelMapping, value, "AudioChannelMapping"); }
        }

        private decimal _audioVolume;
        public decimal AudioVolume
        {
            get { return _audioVolume; }
            set { SetField(ref _audioVolume, value, "AudioVolume"); }
        }

        private string _mediaName;
        public string MediaName
        {
            get { return _mediaName; }
            set {
                if (SetField(ref _mediaName, value, "MediaName"))
                {
                    if (MediaStatus == TMediaStatus.Required)
                        FileName = FileUtils.SanitizeFileName(value) + MediaDirectory.DefaultFileExtension(MediaType);
                };
            }
        }

        readonly Array _mediaEmphasises = Enum.GetValues(typeof(TAS.Server.TMediaEmphasis)); 
        public Array MediaEmphasises { get { return _mediaEmphasises;} }
        private TMediaEmphasis _mediaEmphasis;
        public TMediaEmphasis MediaEmphasis
        {
            get { return _mediaEmphasis; }
            set { SetField(ref _mediaEmphasis, value, "MediaEmphasis"); }
        }
        
        private DateTime? _killDate;
        public DateTime? KillDate
        {
            get { return _killDate; }
            set
            {
                if (_killDate != value)
                {
                    if (value == default(DateTime))
                        _killDate = null;
                    else
                        _killDate = value;
                    Modified = true;
                    NotifyPropertyChanged("IsKillDate");
                    NotifyPropertyChanged("KillDate");
                }
            }
        }

        public bool IsKillDate
        {
            get { return _killDate != null; }
            set
            {
                if (value != IsKillDate)
                {
                    if (value)
                        _killDate = DateTime.UtcNow + TimeSpan.FromDays(30);
                    else
                        _killDate = null;
                    Modified = true;
                    NotifyPropertyChanged("KillDate");
                    NotifyPropertyChanged("IsKillDate");
                }
            }
        }

        public TMediaStatus MediaStatus { get { return (_media == null) ? TMediaStatus.Unknown : _media.MediaStatus; }}
        public Guid MediaGuid { get { return (_media == null) ? Guid.Empty : _media.MediaGuid; }}


        private bool _doNotArchive;
        public bool DoNotArchive
        {
            get { return _doNotArchive; }
            set { SetField(ref _doNotArchive, value, "DoNotArchive"); }
        }

        readonly Array _parentals = Enum.GetValues(typeof(TParental)); 
        public Array Parentals { get { return _parentals; } }
        private TParental _parental;
        public TParental Parental
        {
            get { return _parental; }
            set { SetField(ref _parental, value, "Parental"); }
        }

        readonly Array _mediaCategories = Enum.GetValues(typeof(TMediaCategory)); 
        public Array MediaCategories { get { return _mediaCategories; } }
        private TMediaCategory _mediaCategory;
        public TMediaCategory MediaCategory
        {
            get { return _mediaCategory; }
            set { SetField(ref _mediaCategory, value, "MediaCategory"); }
        }

        private string _idAux;
        public string IdAux
        {
            get { return _idAux; }
            set { SetField(ref _idAux, value, "IdAux"); }
        }

        private bool _isAutoSave;
        public bool IsAutoSave
        {
            get { return _isAutoSave; }
            set
            {
                if (SetField(ref _isAutoSave, value, "IsAutoSave"))
                    NotifyPropertyChanged("IsNoAutoSave");
            }
        }

        public bool IsNoAutoSave
        {
            get { return !_isAutoSave; }
        }

        public bool IsPersistentMedia
        {
            get { return _media is PersistentMedia; }
        }

        public bool IsServerMedia
        {
            get { return _media is ServerMedia; }
        }

        public bool IsIngestDataShown
        {
            get
            {
                return (_media is PersistentMedia && _media.MediaStatus != TMediaStatus.Required);
            }
        }

        private bool _canGetTCFormPreview(object o)
        {
            return _previewVm != null
                && _media != null
                && _previewVm.LoadedMedia != null
                && _media.MediaGuid.Equals(_previewVm.LoadedMedia.MediaGuid);
        }

        public string Error
        {
            get { throw new NotImplementedException(); }
        }

        public string this[string propertyName]
        {
            get
            {
                string validationResult = null;
                switch (propertyName)
                {
                    case "FileName":
                        validationResult = _validateFileName();
                        break;
                    case "TCPlay":
                        validationResult = _validateTCPlay();
                        break;
                    case "DurationPlay":
                        validationResult = _validateDurationPlay();
                        break;
                }
                return validationResult;
            }
        }

        private string _validateFileName()
        {
            string validationResult = string.Empty;
            Media media = _media;
            if (media != null)
            {
                MediaDirectory dir = media.Directory;
                string newName = _fileName;
                if (dir != null && _fileName != null)
                {
                    if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) > 0)
                        validationResult = Properties.Resources._validate_FileNameCanNotContainSpecialCharacters;
                    else
                    {
                        newName = newName.ToLowerInvariant();
                        if ((media.MediaStatus == TMediaStatus.Required || newName != media.FileName.ToLowerInvariant())
                            && dir.FileExists(newName, media.Folder))
                            validationResult = Properties.Resources._validate_FileAlreadyExists;
                        else
                            if (media is PersistentMedia)
                            {
                                if (media.MediaType == TMediaType.Movie
                                    && !MediaDirectory.VideoFileTypes.Contains(Path.GetExtension(newName).ToLower()))
                                    validationResult = string.Format(Properties.Resources._validate_FileMustHaveExtension, string.Join(Properties.Resources._or_, MediaDirectory.VideoFileTypes));
                                if (media.MediaType == TMediaType.Still
                                    && !MediaDirectory.StillFileTypes.Contains(Path.GetExtension(newName).ToLower()))
                                    validationResult = string.Format(Properties.Resources._validate_FileMustHaveExtension, string.Join(Properties.Resources._or_, MediaDirectory.StillFileTypes));
                            }
                    }
                    //if (dir is ArchiveDirectory)
                    //{
                    //    if (DatabaseConnector.ArchiveFileExists(dir, _fileName))
                    //        validationResult = "Plik o takiej nazwie archiwizowano już w tym miesiącu";
                    //}
                    //else
                    //    if (dir.Files.Where(m => m != media && m.FileName == _fileName).Count() > 0)
                    //        validationResult = "Plik o takiej nazwie już istnieje";
                }
            }
            return validationResult;
        }

        private string _validateTCPlay()
        {
            string validationResult = string.Empty;
            Media media = _media;
            if (media != null)
            {
                if (TCPlay < TCStart
                    || TCPlay > TCStart + Duration)
                    validationResult = Properties.Resources._validateStartPlayMustBeInsideFile;
            }
            return validationResult;
        }

        private string _validateDurationPlay()
        {
            string validationResult = string.Empty;
            Media media = _media;
            if (media != null)
            {
                if (DurationPlay + TCPlay  > Duration + TCStart)
                    validationResult = Properties.Resources._validate_DurationInvalid;
            }
            return validationResult;
        }

        public bool IsValid
        {
            get { return (from pi in this.GetType().GetProperties() select this[pi.Name]).Where(s => !string.IsNullOrEmpty(s)).Count() == 0; }
        }

        protected override bool SetField<T>(ref T field, T value, string propertyName)
        {
            if (base.SetField(ref field, value, propertyName))
            {
                Modified = true;
                return true;
            }
            return false;
        }


    }
}
