using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.IO;
using System.Windows.Input;
using TAS.Client.Common;
using System.Threading;
using System.Threading.Tasks;
using TAS.Common;
using TAS.Common.Interfaces;
using resources = TAS.Client.Common.Properties.Resources;

namespace TAS.Client.ViewModels
{
    public class MediaEditViewmodel: EditViewmodelBase<IMedia>, ITemplatedEdit, IDataErrorInfo
    {
        private readonly IMediaManager _mediaManager;

        private bool _isVolumeChecking;
        private string _folder;
        private string _fileName;
        private DateTime _lastUpdated;
        private DateTime _lastAccess;
        private TMediaType _mediaType;
        private TimeSpan _duration;
        private TimeSpan _durationPlay;
        private TimeSpan _tcStart;
        private TimeSpan _tcPlay;
        private TVideoFormat _videoFormat;
        private bool _fieldOrderInverted;
        private TAudioChannelMapping _audioChannelMapping;
        private double _audioVolume;
        private string _mediaName;
        private TMediaEmphasis _mediaEmphasis;
        private DateTime? _killDate;
        private bool _protected;
        private bool _doNotArchive;
        private byte _parental;
        private TMediaCategory _mediaCategory;
        private string _idAux;

        private TemplateMethod _method;
        private readonly ObservableDictionary<string, string> _fields = new ObservableDictionary<string, string>();
        private int _templateLayer;



        public MediaEditViewmodel(IMedia media, IMediaManager mediaManager, bool showButtons) : base(media)
        {
            CommandSaveEdit = new UICommand { ExecuteDelegate = Update, CanExecuteDelegate = o => CanSave() };
            CommandCancelEdit = new UICommand { ExecuteDelegate = Load, CanExecuteDelegate = o => IsModified };
            CommandRefreshStatus = new UICommand { ExecuteDelegate = _refreshStatus };
            CommandCheckVolume = new UICommand { ExecuteDelegate = _checkVolume, CanExecuteDelegate = (o) => !_isVolumeChecking };
            _mediaManager = mediaManager;
            ShowButtons = showButtons;
            Model.PropertyChanged += OnMediaPropertyChanged;
            if (Model is IAnimatedMedia)
            {
                _fields.CollectionChanged += _fields_CollectionChanged;
                CommandAddField = new UICommand { ExecuteDelegate = _addField, CanExecuteDelegate = _canAddField };
                CommandDeleteField = new UICommand { ExecuteDelegate = _deleteField, CanExecuteDelegate = _canDeleteField };
                CommandEditField = new UICommand { ExecuteDelegate = _editField, CanExecuteDelegate = _canDeleteField };
            }
        }
        
        public ICommand CommandSaveEdit { get; }
        public ICommand CommandCancelEdit { get; }
        public ICommand CommandRefreshStatus { get; }
        public ICommand CommandCheckVolume { get; }

        public override void Update(object destObject = null)
        {
            if (IsModified)
            {
                PropertyInfo[] copiedProperties = GetType().GetProperties();
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

        public bool CanSave()
        {
            return IsModified && IsValid && Model.MediaStatus == TMediaStatus.Available;
        }

        public void Revert()
        {
            Load();
        }

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

        public bool ShowButtons { get; }

        public string Folder
        {
            get { return _folder; }
            set { SetField(ref _folder, value); }
        }

        public string FileName 
        {
            get { return _fileName; }
            set
            {
                if (SetField(ref _fileName, value))
                    NotifyPropertyChanged(nameof(IsValid));
            }
        }

        public DateTime LastUpdated
        {
            get { return _lastUpdated; }
            set { SetField(ref _lastUpdated, value); }
        }

        public DateTime LastAccess
        {
            get { return _lastAccess; }
            set { SetField(ref _lastAccess, value); }
        }

        public TMediaType MediaType
        {
            get { return _mediaType; }
            set { SetField(ref _mediaType, value); }
        }

        public TimeSpan Duration
        {
            get { return _duration; }
            set { SetField(ref _duration, value); }
        }

        public TimeSpan DurationPlay
        {
            get { return _durationPlay; }
            set { SetField(ref _durationPlay, value); }
        }

        public TimeSpan TcStart
        {
            get { return _tcStart; }
            set { SetField(ref _tcStart, value); }
        }

        public TimeSpan TcPlay
        {
            get { return _tcPlay; }
            set { SetField(ref _tcPlay, value); }
        }
        
        public Array VideoFormats { get; } = Enum.GetValues(typeof(TVideoFormat));

        public TVideoFormat VideoFormat
        {
            get { return _videoFormat; }
            set
            {
                if (SetField(ref _videoFormat, value))
                    NotifyPropertyChanged(nameof(IsInterlaced));
            }
        }

        public bool FieldOrderInverted
        {
            get { return _fieldOrderInverted; }
            set { SetField(ref _fieldOrderInverted, value); }
        }

        public Array AudioChannelMappings { get; } = Enum.GetValues(typeof(TAudioChannelMapping));

        public TAudioChannelMapping AudioChannelMapping
        {
            get { return _audioChannelMapping; }
            set { SetField(ref _audioChannelMapping, value); }
        }

        public double AudioVolume
        {
            get { return _audioVolume; }
            set { SetField(ref _audioVolume, value); }
        }

        public string MediaName
        {
            get { return _mediaName; }
            set {
                if (SetField(ref _mediaName, value))
                {
                    if (MediaStatus == TMediaStatus.Required)
                        FileName = FileUtils.SanitizeFileName(value) + FileUtils.DefaultFileExtension(MediaType);
                }
            }
        }

        public Array MediaEmphasises { get; } = Enum.GetValues(typeof(TMediaEmphasis));
        public TMediaEmphasis MediaEmphasis
        {
            get { return _mediaEmphasis; }
            set { SetField(ref _mediaEmphasis, value); }
        }
        
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

        public bool Protected
        {
            get { return _protected; }
            set { SetField(ref _protected, value); }
        }

        public TMediaStatus MediaStatus => Model.MediaStatus;

        public Guid MediaGuid => Model.MediaGuid;
        
        public bool DoNotArchive
        {
            get { return _doNotArchive; }
            set { SetField(ref _doNotArchive, value); }
        }

        public bool ShowParentalCombo => _mediaManager?.CGElementsController?.Parentals!= null;

        public IEnumerable<ICGElement> Parentals => _mediaManager?.CGElementsController?.Parentals;

        public byte Parental
        {
            get { return _parental; }
            set { SetField(ref _parental, value); }
        }

        public Array MediaCategories { get; } = Enum.GetValues(typeof(TMediaCategory));

        public TMediaCategory MediaCategory
        {
            get { return _mediaCategory; }
            set { SetField(ref _mediaCategory, value); }
        }

        public string IdAux
        {
            get { return _idAux; }
            set { SetField(ref _idAux, value); }
        }

        #region ITemplatedEdit

        public IDictionary<string, string> Fields
        {
            get => _fields;
            set
            {
                _fields.Clear();
                if (value != null)
                    _fields.AddRange(value);
            }
        }

        public object SelectedField { get; set; }

        public Array Methods { get; } = Enum.GetValues(typeof(TemplateMethod));

        public TemplateMethod Method { get => _method; set => SetField(ref _method, value); }

        public int TemplateLayer { get => _templateLayer; set => SetField(ref _templateLayer, value); }

        public ICommand CommandEditField { get; }

        public ICommand CommandAddField { get; }

        public ICommand CommandDeleteField { get; }
        
        public bool IsKeyReadOnly => false;

        #endregion // ITemplatedEdit

        public bool IsDisplayCgMethod { get; } = false;

        public bool IsPersistentMedia => Model is IPersistentMedia;

        public bool IsServerMedia => Model is IServerMedia;

        public bool IsAnimatedMedia => Model is IAnimatedMedia;

        public bool IsIngestDataShown => Model is IPersistentMedia && Model.MediaStatus != TMediaStatus.Required;

        public bool IsMovie => Model.MediaType == TMediaType.Movie;

        public bool IsInterlaced
        {
            get
            {
                var format = _videoFormat;
                return VideoFormatDescription.Descriptions.ContainsKey(format) && VideoFormatDescription.Descriptions[format].Interlaced;
            }
        }

        public string Error => string.Empty;
        
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

        public bool IsValid => (from pi in GetType().GetProperties() select this[pi.Name]).All(string.IsNullOrEmpty);

        public override string ToString()
        {
            return $"{Infralution.Localization.Wpf.ResourceEnumConverter.ConvertToString(MediaType)} - {_mediaName}";
        }

        protected override void OnDispose()
        {
            Model.PropertyChanged -= OnMediaPropertyChanged;
            if (Model is IAnimatedMedia)
                _fields.CollectionChanged -= _fields_CollectionChanged;
        }

        private void _fields_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            IsModified = true;
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
            using (var kve = new KeyValueEditViewmodel(new KeyValuePair<string, string>(string.Empty, string.Empty), true))
            {
                if (UiServices.ShowDialog<Views.KeyValueEditView>(kve) == true)
                    _fields.Add(kve.Key, kve.Value);
                //kve.OnOk += (o) =>
                //{
                //    var co = (KeyValueEditViewmodel)o;
                //    return (!string.IsNullOrWhiteSpace(co.Key) && !string.IsNullOrWhiteSpace(co.Value) && !co.Key.Contains(' ') && !_fields.ContainsKey(co.Key));
                //};
            }
        }

        private void _editField(object obj)
        {
            if (SelectedField == null)
                return;
            var selected = (KeyValuePair<string, string>)SelectedField;
            using (var kve = new KeyValueEditViewmodel(selected, true))
            {
                if (UiServices.ShowDialog<Views.KeyValueEditView>(kve) == true)
                    _fields[kve.Key]= kve.Value;
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
            operation.Source = Model;
            operation.MeasureStart = TcPlay - TcStart;
            operation.MeasureDuration = DurationPlay;
            operation.AudioVolumeMeasured += _audioVolumeMeasured;
            operation.Finished += _audioVolumeFinished;
            _checkVolumeSignal = new AutoResetEvent(false);
            fileManager.Queue(operation, true);
        }

        private void _audioVolumeFinished(object sender, EventArgs e)
        {
            Task.Run(() =>
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
            AudioVolume = e.AudioVolume;
            AutoResetEvent signal = _checkVolumeSignal;
            signal?.Set();
        }

        #endregion //Command methods

        private void OnMediaPropertyChanged(object media, PropertyChangedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (!string.IsNullOrEmpty(e.PropertyName))
                    {
                        PropertyInfo sourcePi = Model.GetType().GetProperty(e.PropertyName);
                        PropertyInfo destPi = GetType().GetProperty(e.PropertyName);
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
        

    }


}
