using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.IO;
using System.Windows.Input;
using TAS.Common;
using TAS.Client.Common;
using System.Threading;
using TAS.Server.Interfaces;
using TAS.Server.Common;
using resources = TAS.Client.Common.Properties.Resources;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace TAS.Client.ViewModels
{
    public class MediaEditViewmodel: EditViewmodelBase<IMedia>, ITemplatedEdit, IDataErrorInfo
    {
        private readonly PreviewViewmodel _previewVm;
        private readonly bool _showButtons;
        private readonly IMediaManager _mediaManager;
        public MediaEditViewmodel(IMedia media, IMediaManager mediaManager, PreviewViewmodel previewVm, bool showButtons) : base(media, new MediaEditView(media.FrameRate))
        {
            CommandSaveEdit = new UICommand() { ExecuteDelegate = ModelUpdate, CanExecuteDelegate = _canSave };
            CommandCancelEdit = new UICommand() { ExecuteDelegate = ModelLoad, CanExecuteDelegate = o => IsModified };
            CommandRefreshStatus = new UICommand() { ExecuteDelegate = _refreshStatus };
            CommandCheckVolume = new UICommand() { ExecuteDelegate = _checkVolume, CanExecuteDelegate = (o) => !_isVolumeChecking };
            _previewVm = previewVm;
            _mediaManager = mediaManager;
            _showButtons = showButtons;
            if (previewVm != null)
                previewVm.PropertyChanged += _onPreviewPropertyChanged;
            Model.PropertyChanged += OnMediaPropertyChanged;
            if (Model is IAnimatedMedia)
            {
                _fields.CollectionChanged += _fields_CollectionChanged;
                CommandAddField = new UICommand { ExecuteDelegate = _addField, CanExecuteDelegate = _canAddField };
                CommandDeleteField = new UICommand { ExecuteDelegate = _deleteField, CanExecuteDelegate = _canDeleteField };
                CommandEditField = new UICommand { ExecuteDelegate = _editField, CanExecuteDelegate = _canDeleteField };
            }
        }
        
        private void _fields_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            IsModified = true;
        }

        protected override void OnDispose()
        {
            Model.PropertyChanged -= OnMediaPropertyChanged;
            if (_previewVm != null)
                _previewVm.PropertyChanged -= _onPreviewPropertyChanged;
            if (Model is IAnimatedMedia)
                _fields.CollectionChanged -= _fields_CollectionChanged;
        }

        public ICommand CommandSaveEdit { get; private set; }
        public ICommand CommandCancelEdit { get; private set; }
        public ICommand CommandRefreshStatus { get; private set; }
        public ICommand CommandCheckVolume { get; private set; }

        public override void ModelUpdate(object destObject = null)
        {
            if (IsModified)
            {
                PropertyInfo[] copiedProperties = this.GetType().GetProperties();
                foreach (PropertyInfo copyPi in copiedProperties)
                {
                    PropertyInfo destPi = (destObject ?? Model).GetType().GetProperty(copyPi.Name);
                    if (destPi != null)
                    {
                        if (destPi.GetValue(destObject ?? Model, null) != copyPi.GetValue(this, null)
                            && destPi.CanWrite)
                            destPi.SetValue(destObject ?? Model, copyPi.GetValue(this, null), null);
                    }
                }
                IsModified = false;
            }
            if (Model is IPersistentMedia)
                ((IPersistentMedia)Model).Save();
        }

        private bool _canSave(object obj)
        {
            return IsModified && IsValid && Model.MediaStatus == TMediaStatus.Available;
        }

        protected override void ModelLoad(object source = null)
        {
            base.ModelLoad(source);
        }

        public void Revert()
        {
            ModelLoad(null);
        }

        #region Command methods

        private bool _canDeleteField(object obj)
        {
            return SelectedField != null;
        }

        private void _deleteField(object obj)
        {
            if (SelectedField != null)
            {
                var selected = (KeyValuePair<string, string>)SelectedField;
                _fields.Remove(selected.Key);
                SelectedField = null;
            }
        }

        private bool _canAddField(object obj)
        {
            return IsAnimatedMedia;
        }

        private void _addField(object obj)
        {
            using (var kve = new KeyValueEditViewmodel(new KeyValuePair<string, string>(string.Empty, string.Empty), false))
            {
                kve.OnOk += (o) =>
                {
                    var co = (KeyValueEditViewmodel)o;
                    return (!string.IsNullOrWhiteSpace(co.Key) && !string.IsNullOrWhiteSpace(co.Value) && !co.Key.Contains(' ') && !_fields.ContainsKey(co.Key));
                };
                if (kve.ShowDialog() == true)
                    _fields.Add(kve.Key, kve.Value);
            }
        }

        private void _editField(object obj)
        {
            if (SelectedField != null)
            {
                var selected = (KeyValuePair<string, string>)SelectedField;
                var kve = new KeyValueEditViewmodel(selected, false);
                if (kve.ShowDialog() == true)
                    _fields[kve.Key] = kve.Value;
            }
        }

        void _refreshStatus(object o)
        {
            Model.ReVerify();
        }

        AutoResetEvent _checkVolumeSignal;
        void _checkVolume(object o)
        {
            if (_isVolumeChecking)
                return;
            IsVolumeChecking = true;
            IFileManager fileManager = _mediaManager.FileManager;
            ILoudnessOperation operation = fileManager.CreateLoudnessOperation();
            operation.SourceMedia = this.Model;
            operation.MeasureStart = this.TcPlay - this.TcStart;
            operation.MeasureDuration = this.DurationPlay;
            operation.AudioVolumeMeasured += _audioVolumeMeasured;
            operation.Finished += _audioVolumeFinished;
            _checkVolumeSignal = new AutoResetEvent(false);
            fileManager.Queue(operation, true);
        }

        private void _audioVolumeFinished(object sender, EventArgs e)
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                _checkVolumeSignal.WaitOne(5000);
                IsVolumeChecking = false; // finishCallback
                ((ILoudnessOperation)sender).Finished -= _audioVolumeFinished;
                ((ILoudnessOperation)sender).AudioVolumeMeasured -= _audioVolumeMeasured;
                _checkVolumeSignal.Dispose();
                _checkVolumeSignal = null;
            });
        }

        private void _audioVolumeMeasured(object sender, AudioVolumeEventArgs e)
        {
            this.AudioVolume = e.AudioVolume;
            AutoResetEvent signal = _checkVolumeSignal;
            if (signal != null)
                signal.Set();
        }

        #endregion //Command methods

        private void OnMediaPropertyChanged(object media, PropertyChangedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (!string.IsNullOrEmpty(e.PropertyName))
                    {
                        PropertyInfo sourcePi = Model.GetType().GetProperty(e.PropertyName);
                        PropertyInfo destPi = this.GetType().GetProperty(e.PropertyName);
                        if (sourcePi != null
                            && destPi != null
                            && sourcePi.CanRead
                            && destPi.CanWrite)
                        {
                            bool oldModified = IsModified;
                            destPi.SetValue(this, sourcePi.GetValue(Model, null), null);
                            IsModified = oldModified;
                            NotifyPropertyChanged(e.PropertyName);
                        }
                        if (e.PropertyName == nameof(IMedia.FrameRate))
                        {
                            ((MediaEditView)Editor).SetFrameRate(Model.FrameRate);
                            NotifyPropertyChanged(nameof(TcStart));
                            NotifyPropertyChanged(nameof(TcPlay));
                            NotifyPropertyChanged(nameof(Duration));
                            NotifyPropertyChanged(nameof(DurationPlay));
                        }
                    }
                }),
            null);

            if (e.PropertyName == nameof(IMedia.MediaStatus))
            {
                NotifyPropertyChanged(e.PropertyName);
                NotifyPropertyChanged(nameof(IsIngestDataShown));
            }
            if (e.PropertyName == nameof(IMedia.MediaGuid))
            {
                NotifyPropertyChanged(e.PropertyName);
            }
        }

        private void _onPreviewPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_previewVm.LoadedMedia == Model
                && (e.PropertyName == nameof(PreviewViewmodel.TcIn) || e.PropertyName == nameof(PreviewViewmodel.TcOut))
                && _previewVm.SelectedSegment == null )
            {
                TcPlay = _previewVm.TcIn;
                DurationPlay = _previewVm.DurationSelection;
            }
        }

        private bool _isVolumeChecking;
        public bool IsVolumeChecking
        {
            get { return _isVolumeChecking; }
            set
            {
                if (_isVolumeChecking != value)
                {
                    _isVolumeChecking = value;
                    NotifyPropertyChanged(nameof(IsVolumeChecking));
                    InvalidateRequerySuggested();
                }
            }
        }

        public void Delete()
        {
            if (Model != null)
                Model.Delete();
        }

        public bool ShowButtons { get { return _showButtons; } }

        private string _folder;
        public string Folder { get { return _folder; } set { SetField(ref _folder, value, nameof(Folder)); } }
        
        private string _fileName;
        public string FileName 
        {
            get { return _fileName; }
            set
            {
                if (SetField(ref _fileName, value, nameof(FileName)))
                    NotifyPropertyChanged(nameof(IsValid));
            }
        }

        private DateTime _lastUpdated;
        public DateTime LastUpdated
        {
            get { return _lastUpdated; }
            set { SetField(ref _lastUpdated, value, nameof(LastUpdated)); }
        }

        private DateTime _lastAccess;
        public DateTime LastAccess
        {
            get { return _lastAccess; }
            set { SetField(ref _lastAccess, value, nameof(LastAccess)); }
        }

        private TMediaType _mediaType;
        public TMediaType MediaType
        {
            get { return _mediaType; }
            set { SetField(ref _mediaType, value, nameof(MediaType)); }
        }

        private TimeSpan _duration;
        public TimeSpan Duration
        {
            get { return _duration; }
            set { SetField(ref _duration, value, nameof(Duration)); }
        }

        private TimeSpan _durationPlay;
        public TimeSpan DurationPlay
        {
            get { return _durationPlay; }
            set { SetField(ref _durationPlay, value, nameof(DurationPlay)); }
        }

        private TimeSpan _tcStart;
        public TimeSpan TcStart
        {
            get { return _tcStart; }
            set { SetField(ref _tcStart, value, nameof(TcStart)); }
        }

        private TimeSpan _tcPlay;
        public TimeSpan TcPlay
        {
            get { return _tcPlay; }
            set { SetField(ref _tcPlay, value, nameof(TcPlay)); }
        }

        static readonly Array _videoFormats = Enum.GetValues(typeof(TVideoFormat));
        public Array VideoFormats { get { return _videoFormats; } }
        private TVideoFormat _videoFormat;
        public TVideoFormat VideoFormat
        {
            get { return _videoFormat; }
            set
            {
                if (SetField(ref _videoFormat, value, nameof(VideoFormat)))
                    NotifyPropertyChanged(nameof(IsInterlaced));
            }
        }

        private bool _fieldOrderInverted;
        public bool FieldOrderInverted
        {
            get { return _fieldOrderInverted; }
            set { SetField(ref _fieldOrderInverted, value, nameof(FieldOrderInverted)); }
        }

        static readonly Array _audioChannelMappings = Enum.GetValues(typeof(TAudioChannelMapping)); 
        public Array AudioChannelMappings { get { return _audioChannelMappings;} }
        private TAudioChannelMapping _audioChannelMapping;
        public TAudioChannelMapping AudioChannelMapping
        {
            get { return _audioChannelMapping; }
            set { SetField(ref _audioChannelMapping, value, nameof(AudioChannelMapping)); }
        }

        private decimal _audioVolume;
        public decimal AudioVolume
        {
            get { return _audioVolume; }
            set { SetField(ref _audioVolume, value, nameof(AudioVolume)); }
        }

        private string _mediaName;
        public string MediaName
        {
            get { return _mediaName; }
            set {
                if (SetField(ref _mediaName, value, nameof(MediaName)))
                {
                    if (MediaStatus == TMediaStatus.Required)
                        FileName = FileUtils.SanitizeFileName(value) + FileUtils.DefaultFileExtension(MediaType);
                };
            }
        }

        static readonly Array _mediaEmphasises = Enum.GetValues(typeof(TMediaEmphasis)); 
        public Array MediaEmphasises { get { return _mediaEmphasises;} }
        private TMediaEmphasis _mediaEmphasis;
        public TMediaEmphasis MediaEmphasis
        {
            get { return _mediaEmphasis; }
            set { SetField(ref _mediaEmphasis, value, nameof(MediaEmphasis)); }
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
                    IsModified = true;
                    NotifyPropertyChanged(nameof(IsKillDate));
                    NotifyPropertyChanged(nameof(KillDate));
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
                    IsModified = true;
                    NotifyPropertyChanged(nameof(KillDate));
                    NotifyPropertyChanged(nameof(IsKillDate));
                }
            }
        }

        private bool _protected;
        public bool Protected
        {
            get { return _protected; }
            set { SetField(ref _protected, value, nameof(Protected)); }
        }

        public TMediaStatus MediaStatus { get { return Model.MediaStatus; }}
        public Guid MediaGuid { get { return Model.MediaGuid; }}


        private bool _doNotArchive;
        public bool DoNotArchive
        {
            get { return _doNotArchive; }
            set { SetField(ref _doNotArchive, value, nameof(DoNotArchive)); }
        }

        public bool ShowParentalCombo { get { return _mediaManager?.CGElementsController?.Parentals!= null; } }

        public IEnumerable<ICGElement> Parentals { get { return _mediaManager?.CGElementsController?.Parentals; } }
        private byte _parental;
        public byte Parental
        {
            get { return _parental; }
            set { SetField(ref _parental, value, nameof(Parental)); }
        }

        static readonly Array _mediaCategories = Enum.GetValues(typeof(TMediaCategory)); 
        public Array MediaCategories { get { return _mediaCategories; } }
        private TMediaCategory _mediaCategory;
        public TMediaCategory MediaCategory
        {
            get { return _mediaCategory; }
            set { SetField(ref _mediaCategory, value, nameof(MediaCategory)); }
        }

        private string _idAux;
        public string IdAux
        {
            get { return _idAux; }
            set { SetField(ref _idAux, value, nameof(IdAux)); }
        }

        #region ITemplatedEdit

        private ObservableDictionary<string, string> _fields = new ObservableDictionary<string, string>();
        public IDictionary<string, string> Fields
        {
            get { return _fields; }
            set
            {
                if (_fields != null)
                {
                    _fields.Clear();
                    if (value != null)
                        _fields.AddRange(value);
                }

            }
        }
        public object SelectedField { get; set; }

        static readonly Array _methods = Enum.GetValues(typeof(TemplateMethod));
        public Array Methods { get { return _methods; } }
        private TemplateMethod _method;
        public TemplateMethod Method { get { return _method; } set { SetField(ref _method, value, nameof(Method)); } }

        private int _templateLayer;
        public int TemplateLayer { get { return _templateLayer; } set { SetField(ref _templateLayer, value, nameof(TemplateLayer)); } }
        public ICommand CommandEditField { get; private set; }
        public ICommand CommandAddField { get; private set; }
        public ICommand CommandDeleteField { get; private set; }


        public bool KeyIsReadOnly { get { return false; } }

        #endregion // ITemplatedEdit

        public bool IsPersistentMedia
        {
            get { return Model is IPersistentMedia; }
        }

        public bool IsServerMedia
        {
            get { return Model is IServerMedia; }
        }

        public bool IsAnimatedMedia
        {
            get { return Model is IAnimatedMedia; }
        }

        public bool IsIngestDataShown
        {
            get
            {
                return (Model is IPersistentMedia && Model.MediaStatus != TMediaStatus.Required);
            }
        }

        public bool IsMovie
        {
            get { return Model.MediaType == TMediaType.Movie; }
        }

        public bool IsInterlaced
        {
            get
            {
                var format = VideoFormat;
                if (VideoFormatDescription.Descriptions.ContainsKey(format))
                    return VideoFormatDescription.Descriptions[format].Interlaced;
                else
                    return false;
            }
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
                    case nameof(FileName):
                        validationResult = _validateFileName();
                        break;
                    case nameof(TcPlay):
                        validationResult = _validateTcPlay();
                        break;
                    case nameof(DurationPlay):
                        validationResult = _validateDurationPlay();
                        break;
                }
                return validationResult;
            }
        }

        private string _validateFileName()
        {
            string validationResult = string.Empty;
                var dir = Model.Directory;
                string newName = _fileName;
                if (dir != null && _fileName != null)
                {
                if (newName.StartsWith(" ") || newName.EndsWith(" "))
                    validationResult = resources._validate_FileNameCanNotStartOrEndWithSpace;
                else
                if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) > 0)
                        validationResult = resources._validate_FileNameCanNotContainSpecialCharacters;
                    else
                    {
                        newName = newName.ToLowerInvariant();
                        if ((Model.MediaStatus == TMediaStatus.Required || newName != Model.FileName.ToLowerInvariant())
                            && dir.FileExists(newName, Model.Folder))
                            validationResult = resources._validate_FileAlreadyExists;
                        else
                            if (Model is IPersistentMedia)
                            {
                                if (Model.MediaType == TMediaType.Movie
                                    && !FileUtils.VideoFileTypes.Contains(Path.GetExtension(newName).ToLower()))
                                    validationResult = string.Format(resources._validate_FileMustHaveExtension, string.Join(resources._or_, FileUtils.VideoFileTypes));
                                if (Model.MediaType == TMediaType.Still
                                    && !FileUtils.StillFileTypes.Contains(Path.GetExtension(newName).ToLower()))
                                    validationResult = string.Format(resources._validate_FileMustHaveExtension, string.Join(resources._or_, FileUtils.StillFileTypes));
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
            return validationResult;
        }

        private string _validateTcPlay()
        {
            string validationResult = string.Empty;
            if (TcPlay < TcStart
                || TcPlay > TcStart + Duration)
                validationResult = resources._validateStartPlayMustBeInsideFile;
            return validationResult;
        }

        private string _validateDurationPlay()
        {
            string validationResult = string.Empty;
            if (DurationPlay + TcPlay > Duration + TcStart)
                validationResult = resources._validate_DurationInvalid;
            return validationResult;
        }

        public bool IsValid
        {
            get { return (from pi in this.GetType().GetProperties() select this[pi.Name]).Where(s => !string.IsNullOrEmpty(s)).Count() == 0; }
        }

        public override string ToString()
        {
            return $"{Infralution.Localization.Wpf.ResourceEnumConverter.ConvertToString(MediaType)} - {_mediaName}";
        }

    }


}
