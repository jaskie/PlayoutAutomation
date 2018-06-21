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
            get => _isVolumeChecking;
            set
            {
                if (_isVolumeChecking == value)
                    return;
                _isVolumeChecking = value;
                NotifyPropertyChanged(nameof(IsVolumeChecking));
                InvalidateRequerySuggested();
            }
        }

        public void Delete()
        {
            Model?.Delete();
        }

        public bool ShowButtons { get; }

        public string Folder
        {
            get => _folder;
            set => SetField(ref _folder, value);
        }

        public string FileName 
        {
            get => _fileName;
            set
            {
                if (SetField(ref _fileName, value))
                    NotifyPropertyChanged(nameof(IsValid));
            }
        }

        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set => SetField(ref _lastUpdated, value);
        }

        public DateTime LastAccess
        {
            get => _lastAccess;
            set => SetField(ref _lastAccess, value);
        }

        public TMediaType MediaType
        {
            get => _mediaType;
            set => SetField(ref _mediaType, value);
        }

        public TimeSpan Duration
        {
            get => _duration;
            set => SetField(ref _duration, value);
        }

        public TimeSpan DurationPlay
        {
            get => _durationPlay;
            set => SetField(ref _durationPlay, value);
        }

        public TimeSpan TcStart
        {
            get => _tcStart;
            set => SetField(ref _tcStart, value);
        }

        public TimeSpan TcPlay
        {
            get => _tcPlay;
            set => SetField(ref _tcPlay, value);
        }
        
        public Array VideoFormats { get; } = Enum.GetValues(typeof(TVideoFormat));

        public TVideoFormat VideoFormat
        {
            get => _videoFormat;
            set
            {
                if (SetField(ref _videoFormat, value))
                    NotifyPropertyChanged(nameof(IsInterlaced));
            }
        }

        public bool FieldOrderInverted
        {
            get => _fieldOrderInverted;
            set => SetField(ref _fieldOrderInverted, value);
        }

        public Array AudioChannelMappings { get; } = Enum.GetValues(typeof(TAudioChannelMapping));

        public TAudioChannelMapping AudioChannelMapping
        {
            get => _audioChannelMapping;
            set => SetField(ref _audioChannelMapping, value);
        }

        public double AudioVolume
        {
            get => _audioVolume;
            set => SetField(ref _audioVolume, value);
        }

        public string MediaName
        {
            get => _mediaName;
            set
            {
                if (!SetField(ref _mediaName, value))
                    return;
                if (MediaStatus == TMediaStatus.Required)
                    FileName = FileUtils.SanitizeFileName(value) + FileUtils.DefaultFileExtension(MediaType);
            }
        }

        public Array MediaEmphasises { get; } = Enum.GetValues(typeof(TMediaEmphasis));
        public TMediaEmphasis MediaEmphasis
        {
            get => _mediaEmphasis;
            set => SetField(ref _mediaEmphasis, value);
        }
        
        public DateTime? KillDate
        {
            get => _killDate;
            set
            {
                if (_killDate == value)
                    return;
                _killDate = value == default(DateTime) ? null : value;
                IsModified = true;
                NotifyPropertyChanged(nameof(IsKillDate));
                NotifyPropertyChanged();
            }
        }

        public bool IsKillDate
        {
            get => _killDate != null;
            set
            {
                if (value == IsKillDate)
                    return;
                if (value)
                    _killDate = DateTime.UtcNow.Date + TimeSpan.FromDays(30);
                else
                    _killDate = null;
                IsModified = true;
                NotifyPropertyChanged(nameof(KillDate));
                NotifyPropertyChanged();
            }
        }

        public bool Protected
        {
            get => _protected;
            set => SetField(ref _protected, value);
        }

        public TMediaStatus MediaStatus => Model.MediaStatus;

        public Guid MediaGuid => Model.MediaGuid;
        
        public bool DoNotArchive
        {
            get => _doNotArchive;
            set => SetField(ref _doNotArchive, value);
        }

        public bool ShowParentalCombo => _mediaManager?.CGElementsController?.Parentals!= null;

        public IEnumerable<ICGElement> Parentals => _mediaManager?.CGElementsController?.Parentals;

        public byte Parental
        {
            get => _parental;
            set => SetField(ref _parental, value);
        }

        public Array MediaCategories { get; } = Enum.GetValues(typeof(TMediaCategory));

        public TMediaCategory MediaCategory
        {
            get => _mediaCategory;
            set => SetField(ref _mediaCategory, value);
        }

        public string IdAux
        {
            get => _idAux;
            set => SetField(ref _idAux, value);
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
                    case nameof(MediaName):
                        validationResult = _validateMediaName();
                        break;
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
            return $"{Infralution.Localization.Wpf.ResourceEnumConverter.ConvertToString(MediaType)} - {MediaName}";
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
        
        private string _validateMediaName()
        {
            if (Model is IPersistentMedia pm && pm.FieldLengths.TryGetValue(nameof(IMedia.MediaName), out var mnLength) && MediaName.Length > mnLength)
                return resources._validate_TextTooLong;
            return string.Empty;
        }

        private string _validateFileName()
        {
            var dir = Model.Directory;
            if (dir == null || _fileName == null)
                return string.Empty;
            if (FileName.StartsWith(" ") || FileName.EndsWith(" "))
                return resources._validate_FileNameCanNotStartOrEndWithSpace;
            if (FileName.IndexOfAny(Path.GetInvalidFileNameChars()) > 0)
                return resources._validate_FileNameCanNotContainSpecialCharacters;
            FileName = FileName.ToLowerInvariant();
            if ((Model.MediaStatus == TMediaStatus.Required || FileName != Model.FileName.ToLowerInvariant())
                && dir.FileExists(FileName, Model.Folder))
                return resources._validate_FileAlreadyExists;
            if (Model is IPersistentMedia pm)
            {
                if (pm.FieldLengths.TryGetValue(nameof(IMedia.FileName), out var length) && FileName.Length > length)
                    return resources._validate_TextTooLong;
                if (pm.MediaType == TMediaType.Movie
                    && !FileUtils.VideoFileTypes.Contains(Path.GetExtension(FileName).ToLower()))
                    return string.Format(resources._validate_FileMustHaveExtension, string.Join(resources._or_, FileUtils.VideoFileTypes));
                if (pm.MediaType == TMediaType.Still
                    && !FileUtils.StillFileTypes.Contains(Path.GetExtension(FileName).ToLower()))
                    return string.Format(resources._validate_FileMustHaveExtension, string.Join(resources._or_, FileUtils.StillFileTypes));
            }
            //if (dir is ArchiveDirectory)
            //{
            //    if (DatabaseConnector.ArchiveFileExists(dir, _fileName))
            //        validationResult = "Plik o takiej nazwie archiwizowano już w tym miesiącu";
            //}
            //else
            //    if (dir.Files.Where(m => m != media && m.FileName == _fileName).Count() > 0)
            //        validationResult = "Plik o takiej nazwie już istnieje";
            return string.Empty;
        }

        private string _validateTcPlay()
        {
            var validationResult = string.Empty;
            if (TcPlay < TcStart
                || TcPlay > TcStart + Duration)
                validationResult = resources._validateStartPlayMustBeInsideFile;
            return validationResult;
        }

        private string _validateDurationPlay()
        {
            var validationResult = string.Empty;
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
            if (SelectedField == null)
                return;
            var selected = (KeyValuePair<string, string>)SelectedField;
            _fields.Remove(selected.Key);
            SelectedField = null;
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
                    if (string.IsNullOrEmpty(e.PropertyName))
                        return;
                    var sourcePi = Model.GetType().GetProperty(e.PropertyName);
                    var destPi = GetType().GetProperty(e.PropertyName);
                    if (sourcePi == null || destPi == null || !sourcePi.CanRead || !destPi.CanWrite)
                        return;
                    var oldModified = IsModified;
                    destPi.SetValue(this, sourcePi.GetValue(Model, null), null);
                    IsModified = oldModified;
                    NotifyPropertyChanged(e.PropertyName);
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
