using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TAS.Server;
using System.Windows;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.Remoting.Messaging;
using System.Windows.Input;
using TAS.Common;
using TAS.Client.Common;
using TAS.Server.Interfaces;
using TAS.Server.Common;
using resources = TAS.Client.Common.Properties.Resources;
using System.Collections.ObjectModel;

namespace TAS.Client.ViewModels
{
    public class EventEditViewmodel : ViewmodelBase, IDataErrorInfo
    {
        private readonly IEngine _engine;
        private readonly EngineViewmodel _engineViewModel;
        private readonly PreviewViewmodel _previewViewModel;
        public EventEditViewmodel(EngineViewmodel engineViewModel, PreviewViewmodel previewViewModel)
        {
            _engineViewModel = engineViewModel;
            _previewViewModel = previewViewModel; 
            if (previewViewModel != null)
                previewViewModel.PropertyChanged += PreviewViewModel_PropertyChanged;
            _engine = engineViewModel.Engine;
            _fields.CollectionChanged += _fields_CollectionChanged;
            CommandSaveEdit = new UICommand() { ExecuteDelegate = _save, CanExecuteDelegate = _canSave };
            CommandUndoEdit = new UICommand() { ExecuteDelegate = _load, CanExecuteDelegate = o => Modified };
            CommandChangeMovie = new UICommand() { ExecuteDelegate = _changeMovie, CanExecuteDelegate = _isEditableMovie };
            CommandEditMovie = new UICommand() { ExecuteDelegate = _editMovie, CanExecuteDelegate = _isEditableMovie };
            CommandCheckVolume = new UICommand() { ExecuteDelegate = _checkVolume, CanExecuteDelegate = _canCheckVolume };
            CommandAddField = new UICommand { ExecuteDelegate = _addField, CanExecuteDelegate = _canAddField };
            CommandDeleteField = new UICommand { ExecuteDelegate = _deleteField, CanExecuteDelegate = _canDeleteField };
            CommandEditField = new UICommand { ExecuteDelegate = _editField, CanExecuteDelegate = _canDeleteField };
        }

        private void _fields_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Modified = true;
        }

        protected override void OnDispose()
        {
            if (_event != null)
                Event = null;
            if (_previewViewModel != null)
                _previewViewModel.PropertyChanged -= PreviewViewModel_PropertyChanged;
            _fields.CollectionChanged -= _fields_CollectionChanged;
        }

        private void PreviewViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_previewViewModel.LoadedMedia == this.Media
                && (e.PropertyName == "TcIn" || e.PropertyName == "TcOut")
                && _previewViewModel.SelectedSegment == null)
            {
                ScheduledTc = _previewViewModel.TcIn;
                Duration = _previewViewModel.DurationSelection;
            }
        }
        
        public ICommand CommandUndoEdit { get; private set; }
        public ICommand CommandSaveEdit { get; private set; }
        public ICommand CommandChangeMovie { get; private set; }
        public ICommand CommandEditMovie { get; private set; }
        public ICommand CommandCheckVolume { get; private set; }
        public ICommand CommandToggleEnabled { get; private set; }
        public ICommand CommandToggleHold { get; private set; }
        public ICommand CommandAddField { get; private set; }
        public ICommand CommandDeleteField { get; private set; }
        public ICommand CommandEditField { get; private set; }


        private IEvent _event;
        public IEvent Event
        {
            get { return _event; }
            set
            {
                IEvent ev = _event;
                if (ev != null && ev.Engine != _engine)
                    throw new InvalidOperationException("Edit event engine invalid");
                if (value != ev)
                {
                    if (this.Modified
                    && MessageBox.Show(resources._query_SaveChangedData, resources._caption_Confirmation, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        _save(null);
                    if (ev != null)
                    {
                        ev.PropertyChanged -= _eventPropertyChanged;
                        ev.SubEventChanged -= _onSubeventChanged;
                        ev.Relocated -= _onRelocated;
                    }
                    _event = value;
                    if (value != null)
                    {
                        value.PropertyChanged += _eventPropertyChanged;
                        value.SubEventChanged += _onSubeventChanged;
                        value.Relocated += _onRelocated;
                    }
                    _load(null);
                }
            }
        }

        void _save(object o)
        {
            IEvent e2Save = Event;
            if (Modified && e2Save != null)
            {
                PropertyInfo[] copiedProperties = this.GetType().GetProperties();
                foreach (PropertyInfo copyPi in copiedProperties)
                {
                    PropertyInfo destPi = e2Save.GetType().GetProperty(copyPi.Name);
                    if (destPi != null)
                    {
                        if (destPi.GetValue(e2Save, null) != copyPi.GetValue(this, null)
                            && destPi.CanWrite
                            && destPi.PropertyType.Equals(copyPi.PropertyType))
                            destPi.SetValue(e2Save, copyPi.GetValue(this, null), null);
                    }
                }
                Modified = false;
            }
            if (e2Save != null && e2Save.Modified)
            {
                e2Save.Save();
                _load(null);
            }
        }

        void _load(object o)
        {
            _isLoading = true;
            try
            {
                IEvent e2Load = _event;
                if (e2Load != null)
                {
                    PropertyInfo[] copiedProperties = this.GetType().GetProperties();
                    foreach (PropertyInfo copyPi in copiedProperties)
                    {
                        PropertyInfo sourcePi = e2Load.GetType().GetProperty(copyPi.Name);
                        if (sourcePi != null 
                            && copyPi.Name != "Modified"
                            && sourcePi.PropertyType.Equals(copyPi.PropertyType)
                            && copyPi.CanWrite
                            && sourcePi.CanRead)
                            copyPi.SetValue(this, sourcePi.GetValue(e2Load, null), null);
                    }
                }
                else // _event is null
                {
                    PropertyInfo[] zeroedProperties = this.GetType().GetProperties();
                    foreach (PropertyInfo zeroPi in zeroedProperties)
                    {
                        PropertyInfo sourcePi = typeof(IEvent).GetProperty(zeroPi.Name);
                        if (sourcePi != null)
                            zeroPi.SetValue(this, null, null);
                    }
                }
            }
            finally
            {
                _isLoading = false;
                Modified = false;
            }
            NotifyPropertyChanged(null);
        }

        private void _readProperty(string propertyName)
        {
            IEvent e2Read = _event;
            PropertyInfo writingProperty = this.GetType().GetProperty(propertyName);
            if (e2Read != null)
            {
                PropertyInfo sourcePi = e2Read.GetType().GetProperty(propertyName);
                if (sourcePi != null
                    && writingProperty.Name != "Modified"
                    && sourcePi.PropertyType.Equals(writingProperty.PropertyType))
                    writingProperty.SetValue(this, sourcePi.GetValue(e2Read, null), null);
            }
            else
                writingProperty.SetValue(this, null, null);
        }

        bool _isLoading;
        protected override bool SetField<T>(ref T field, T value, string propertyName)
        {
            if (base.SetField(ref field, value, propertyName))
            {
                if (!_isLoading &&
                    (propertyName != "ScheduledTime" || IsScheduledTimeEnabled))
                    Modified = true;
                return true;
            }
            return false;
        }

        public string Error
        {
            get { return String.Empty;}
        }

        public string this[string propertyName]
        {
            get
            {
                string validationResult = null;
                switch (propertyName)
                {
                    case "Duration":
                        validationResult = _validateDuration();
                        break;
                    case "ScheduledTc":
                        validationResult = _validateScheduledTc();
                        break;
                    case "ScheduledTime":
                        validationResult = _validateScheduledTime();
                        break;
                    case "TransitionTime":
                        validationResult = _validateTransitionTime();
                        break;
                    case "ScheduledDelay":
                        validationResult = _validateScheduledDelay();
                        break;
                }
                return validationResult;
            }
        }

        private string _validateScheduledDelay()
        {
            var ev = _event;
            if (ev != null && ev.EventType == TEventType.StillImage)
            {
                IEvent parent = ev.Parent;
                if (parent != null && _duration + _scheduledDelay > parent.Duration)
                    return resources._validate_ScheduledDelayInvalid;
            }
            return null;
        }

        private string _validateScheduledTime()
        {
            IEvent ev = _event;
            if (ev != null && (_startType == TStartType.OnFixedTime || _startType == TStartType.Manual) && ev.PlayState == TPlayState.Scheduled && _scheduledTime < ev.Engine.CurrentTime)
                return resources._validate_StartTimePassed;
            else 
                return string.Empty;
        }

        private string _validateScheduledTc()
        {
            IEvent ev = _event;
            if (ev != null)
            {
                IMedia media = _event.Media;
                if (ev.EventType == TEventType.Movie && media != null)
                {
                    if (_scheduledTc > media.Duration + media.TcStart)
                        return string.Format(resources._validate_StartTCAfterFile, (media.Duration + media.TcStart).ToSMPTETimecodeString(_engine.VideoFormat));
                    if (_scheduledTc < media.TcStart)
                        return string.Format(resources._validate_StartTCBeforeFile, media.TcStart.ToSMPTETimecodeString(_engine.VideoFormat));
                }
            }
            return null;
        }

        private string _validateDuration()
        {
            IEvent ev = _event;
            if (ev != null)
            {
                IMedia media = _event.Media;
                if (ev.EventType == TEventType.Movie && media != null
                    && _duration + _scheduledTc > media.Duration + media.TcStart)
                    return resources._validate_DurationInvalid;
                if (ev.EventType == TEventType.StillImage)
                {
                    IEvent parent = ev.Parent;
                    if (parent != null && _duration + _scheduledDelay > parent.Duration)
                        return resources._validate_ScheduledDelayInvalid;
                }
            }
            return null;
        }


        private string _validateTransitionTime()
        {
            string validationResult = string.Empty;
            if (_transitionTime > _duration)
                    validationResult = resources._validate_TransitionTimeInvalid;
            return validationResult;
        }

        MediaSearchViewmodel _mediaSearchViewModel;
        private void _chooseMedia(TMediaType mediaType, IEvent baseEvent, TStartType startType, Action<MediaSearchEventArgs> executeOnChoose, VideoFormatDescription videoFormatDescription = null)
        {
            if (_mediaSearchViewModel == null)
            {
                _mediaSearchViewModel = new MediaSearchViewmodel(_engineViewModel.Engine, _event.Engine.MediaManager, mediaType, true, videoFormatDescription);
                _mediaSearchViewModel.BaseEvent = baseEvent;
                _mediaSearchViewModel.NewEventStartType = startType;
                _mediaSearchViewModel.MediaChoosen += new EventHandler<MediaSearchEventArgs>((o, e) => executeOnChoose(e));
                _mediaSearchViewModel.SearchWindowClosed += _searchWindowClosed;
            }
        }


        private void _searchWindowClosed(object sender, EventArgs e)
        {
            MediaSearchViewmodel mvs = (MediaSearchViewmodel)sender;
            mvs.SearchWindowClosed -= _searchWindowClosed;
            _mediaSearchViewModel.Dispose();
            _mediaSearchViewModel = null;
        }        


        private IMedia _media;
        public IMedia Media
        {
            get { return _media; }
            set
            {
                SetField(ref _media, value, "Media");
            }
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
                Fields.Remove(selected.Key);
                SelectedField = null;
            }
        }

        private bool _canAddField(object obj)
        {
            return IsAnimation;
        }

        private void _addField(object obj)
        {
            KeyValueEditViewmodel kve = new KeyValueEditViewmodel(new KeyValuePair<string, string>(string.Empty, string.Empty), false);
            kve.OKCallback = (o) => {
                var co = o as KeyValueEditViewmodel;
                return (!string.IsNullOrWhiteSpace(co.Key) && !string.IsNullOrWhiteSpace(co.Value) && !co.Key.Contains(' ') && !_fields.ContainsKey(co.Key));
            };
            if (kve.ShowDialog() == true)
                _fields.Add(kve.Result);
        }

        private void _editField(object obj)
        {
            if (SelectedField != null)
            {
                KeyValueEditViewmodel kve = new KeyValueEditViewmodel((KeyValuePair<string, string>)SelectedField, true);
                if (kve.ShowDialog() == true)
                    _fields[kve.Key] = kve.Value;
            }
        }


        void _changeMovie(object o)
        {
            IEvent ev = _event;
            if (ev != null
                && ev.EventType == TEventType.Movie)
            {
                _chooseMedia(TMediaType.Movie, ev, ev.StartType, new Action<MediaSearchEventArgs>((e) =>
                    {
                        if (e.Media != null)
                        {
                            if (e.Media.MediaType == TMediaType.Movie)
                            {
                                Media = e.Media;
                                Duration = e.Duration;
                                ScheduledTc = e.TCIn;
                                AudioVolume = null;
                                EventName = e.MediaName;
                                _gpi = _setGPI(e.Media);
                                NotifyPropertyChanged("CanTriggerGPI");
                                NotifyPropertyChanged("GPICrawl");
                                NotifyPropertyChanged("GPILogo");
                                NotifyPropertyChanged("GPIParental");
                            }
                        }
                    }));
            }
        }

        private void _editMovie(object obj)
        {
            using (var evm = new MediaEditWindowViewmodel(_event.Media, _engine.MediaManager))
                evm.ShowDialog();
        }

        private void _checkVolume(object obj)
        {
            if (_media == null)
                return;
            IsVolumeChecking = true;
            var fileManager = _engine.MediaManager.FileManager;
            var operation = fileManager.CreateLoudnessOperation();
            operation.SourceMedia = _event.Media;
            operation.MeasureStart = _event.StartTc - _media.TcStart;
            operation.MeasureDuration = _event.Duration;
            operation.AudioVolumeMeasured += _audioVolumeMeasured;
            operation.Finished += _audioVolumeFinished;
            fileManager.Queue(operation, true);
        }

        private void _audioVolumeFinished(object sender, EventArgs e)
        {
            IsVolumeChecking = false;
            ((ILoudnessOperation)sender).Finished -= _audioVolumeFinished;
            ((ILoudnessOperation)sender).AudioVolumeMeasured -= _audioVolumeFinished;
        }

        private void _audioVolumeMeasured(object sender, AudioVolumeEventArgs e)
        {
            AudioVolume = e.AudioVolume;
        }

        bool _canReschedule(object o)
        {
            IEvent ev = _event;
            return ev != null
                && (ev.PlayState == TPlayState.Played || ev.PlayState == TPlayState.Aborted);
        }
        bool _isEditableMovie(object o)
        {
            IEvent ev = _event;
            return ev != null
                && ev.PlayState == TPlayState.Scheduled
                && ev.EventType == TEventType.Movie;
        }
        bool _canCheckVolume(object o)
        {
            return !_isVolumeChecking && _isEditableMovie(o);
        }
        bool _canSave(object o)
        {
            IEvent ev = _event;
            return ev != null
                && (Modified || ev.Modified);
        }
        
        EventGPI _setGPI(IMedia media)
        {
            EventGPI GPI = new EventGPI();
            GPI.CanTrigger = _engine.EnableGPIForNewEvents;
            if (media != null)
            {
                var category = media.MediaCategory;
                GPI.Logo = category == TMediaCategory.Fill || category == TMediaCategory.Show || category == TMediaCategory.Promo || category == TMediaCategory.Insert || category == TMediaCategory.Jingle ? TLogo.Normal : TLogo.NoLogo;
                GPI.Parental = media.Parental;
            }
            return GPI;
        }

        #endregion // command methods

        private bool _isVolumeChecking;
        public bool IsVolumeChecking
        {
            get { return _isVolumeChecking; }
            set
            {
                if (base.SetField(ref _isVolumeChecking, value, "IsVolumeChecking")) //not set Modified
                    InvalidateRequerySuggested();
            }
        }
        
        private bool _modified;
        public bool Modified
        {
            get { return _modified; }
            private set
            {
                if (_modified != value)
                    _modified = value;
                if (value)
                    InvalidateRequerySuggested();
            }
        }

        private TEventType _eventType;
        public TEventType EventType
        {
            get { return _eventType; }
            set { SetField(ref _eventType, value, "EventType"); }
        }

        private string _eventName;
        public string EventName
        {
            get { return _eventName; }
            set { SetField(ref _eventName, value, "EventName"); }
        }

        public bool IsEditEnabled
        {
            get
            {
                var ev = _event;
                return ev != null && ev.PlayState == TPlayState.Scheduled;
            }
        }

        public bool IsMovieOrLive
        {
            get
            {
                var ev = _event;
                return ev != null 
                    && (ev.EventType == TEventType.Movie || ev.EventType == TEventType.Live);
            }
        }

        public bool IsOverlay
        {
            get
            {
                var ev = _event;
                return ev != null
                    && (ev.EventType == TEventType.StillImage || ev.EventType == TEventType.Animation);
            }
        }


        public bool IsAnimation
        {
            get
            {
                var ev = _event;
                return ev != null
                    && ev is IAnimatedEvent; 
            }
        }

        private int _templateLayer;
        public int TemplateLayer { get { return _templateLayer; } set { SetField(ref _templateLayer, value, "TemplateLayer"); } }

        private ObservableDictionary<string, string> _fields = new ObservableDictionary<string, string>();
        public IDictionary<string, string> Fields
        {
            get { return _fields; }
            set
            {
                _fields.Clear();
                if (value != null)
                    _fields.AddRange(value);
            }
        }
        public object SelectedField { get; set; }

        public bool IsMovie
        {
            get
            {
                var ev = _event;
                return ev != null 
                    && ev.EventType == TEventType.Movie;
            }
        }

        public bool IsStillImage
        {
            get
            {
                var ev = _event;
                return ev != null
                    && ev.EventType == TEventType.StillImage;
            }
        }

        public bool IsTransitionPanelEnabled
        {
            get { 
                var ev = _event;
                return ev != null && !_isHold && (ev.EventType == TEventType.Live || ev.EventType == TEventType.Movie);
                }
        }

        public bool IsNotContainer
        {
            get { 
                var ev = _event;
                return ev != null && ev.EventType != TEventType.Container;
                }
        }

        public bool CanHold { get { return _event != null && _event.Prior != null; } }
        public bool CanLoop { get { return _event != null && _event.GetSuccessor() == null; } }

        private bool _isEnabled;
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { SetField(ref _isEnabled, value, "IsEnabled"); }
        }

        private bool _isHold;
        public bool IsHold
        {
            get { return _isHold; }
            set
            {
                if (SetField(ref _isHold, value, "IsHold"))
                {
                    if (value)
                        TransitionTime = TimeSpan.Zero;
                    NotifyPropertyChanged("IsTransitionPanelEnabled");
                }
            }
        }

        private bool _isLoop;
        public bool IsLoop
        {
            get { return _isLoop; }
            set { SetField(ref _isLoop, value, "IsLoop"); }
        }

        private TStartType _startType;
        public TStartType StartType
        {
            get { return _startType; }
            set { SetField(ref _startType, value, "StartType"); }
        }

        public string BoundEventName
        {
            get
            {
                IEvent ev = Event;
                IEvent boundEvent = ev == null ? null : (ev.StartType == TStartType.With) ? ev.Parent : (ev.StartType == TStartType.After) ? ev.Prior : null;
                return boundEvent == null ? string.Empty : boundEvent.EventName;
            }
        }

        private TimeSpan _scheduledTc;
        public TimeSpan ScheduledTc
        {
            get { return _scheduledTc; }
            set { 
                SetField(ref _scheduledTc, value, "ScheduledTc");
                NotifyPropertyChanged("Duration");
            }
        }

        readonly Array _transitionTypes = Enum.GetValues(typeof(TTransitionType));
        public Array TransitionTypes { get { return _transitionTypes; } }

        private TTransitionType _transitionType;
        public TTransitionType TransitionType
        {
            get { return _transitionType; }
            set { SetField(ref _transitionType, value, "TransitionType"); }
        }

        private TimeSpan _transitionTime;
        public TimeSpan TransitionTime
        {
            get { return _transitionTime; }
            set { SetField(ref _transitionTime, value, "TransitionTime"); }
        }

        private decimal? _audioVolume;
        public decimal? AudioVolume
        {
            get { return _audioVolume; }
            set
            {
                if (SetField(ref _audioVolume, value, "AudioVolume"))
                {
                    NotifyPropertyChanged("HasAudioVolume");
                    NotifyPropertyChanged("AudioVolumeLevel");
                }
            }
        }
        
        public decimal AudioVolumeLevel
        {
            get { return _audioVolume != null ? (decimal)_audioVolume : _media != null ? _media.AudioVolume : 0m; }
            set
            {
                if (SetField(ref _audioVolume, value, "AudioVolumeLevel"))
                {
                    NotifyPropertyChanged("HasAudioVolume");
                    NotifyPropertyChanged("AudioVolume");
                }
            }
        }

        public bool HasAudioVolume
        {
            get { return _audioVolume != null; }
            set
            {
                if (SetField(ref _audioVolume, value? (_media != null ? (decimal?)_media.AudioVolume : 0m) : null, "HasAudioVolume"))
                {
                    NotifyPropertyChanged("AudioVolume");
                    NotifyPropertyChanged("AudioVolumeLevel");
                }
            }
        }

        private DateTime _scheduledTime;
        public DateTime ScheduledTime
        {
            get { return _scheduledTime; }
            set { SetField(ref _scheduledTime, value, "ScheduledTime"); }
        }

        private TimeSpan? _requestedStartTime;
        public TimeSpan? RequestedStartTime
        {
            get { return _requestedStartTime; }
            set { SetField(ref _requestedStartTime, value, "RequestedStartTime"); }
        }

        private TimeSpan _duration;
        public TimeSpan Duration
        {
            get { return _duration; }
            set
            {
                SetField(ref _duration, value, "Duration");
                NotifyPropertyChanged("ScheduledTc");
            }
        }

        private TimeSpan _scheduledDelay;
        public TimeSpan ScheduledDelay
        {
            get { return _scheduledDelay; }
            set { SetField(ref _scheduledDelay, value, "ScheduledDelay"); }
        }

        private sbyte _layer;
        public sbyte Layer
        {
            get { return _layer; }
            set { SetField(ref _layer, value, "Layer"); }
        }

        public bool HasSubItemOnLayer1
        {
            get
            {
                IEvent ev = Event;
                return (ev == null) ? false : (ev.EventType == TEventType.StillImage) ? ev.Layer == VideoLayer.CG1 : ev.SubEvents.Any(e => e.Layer == VideoLayer.CG1 && e.EventType == TEventType.StillImage);
            }
        }
        public bool HasSubItemOnLayer2
        {
            get
            {
                IEvent ev = Event;
                return (ev == null) ? false : (ev.EventType == TEventType.StillImage) ? ev.Layer == VideoLayer.CG2 : ev.SubEvents.Any(e => e.Layer == VideoLayer.CG2 && e.EventType == TEventType.StillImage);
            }
        }
        public bool HasSubItemOnLayer3
        {
            get
            {
                IEvent ev = Event;
                return (ev == null) ? false : (ev.EventType == TEventType.StillImage) ? ev.Layer == VideoLayer.CG3 : ev.SubEvents.Any(e => e.Layer == VideoLayer.CG3 && e.EventType == TEventType.StillImage);
            }
        }

        public bool HasSubItems
        {
            get
            {
                IEvent ev = Event;
                return (ev == null || ev.EventType == TEventType.Live || ev.EventType == TEventType.Movie) ? false : ev.SubEvents.Any(e => e.EventType == TEventType.StillImage);
            }
        }

        public bool IsScheduledTimeEnabled
        {
            get
            {
                IEvent ev = Event;
                return !((ev == null) || ev.StartType == TStartType.After || ev.StartType == TStartType.With);
            }
        }

        public bool IsDurationEnabled
        {
            get
            {
                IEvent ev = Event;
                return (ev != null) && ev.EventType != TEventType.Rundown;
            }
        }

        #region GPI

        public bool IsGPIEnabled
        {
            get
            {
                IEvent ev = Event;
                if (ev != null)
                {
                    IEngine engine = ev.Engine;
                    return (engine != null
                        && (ev.EventType == TEventType.Live || ev.EventType == TEventType.Movie)
                        && (engine.Gpi != null || engine.LocalGpi != null));
                }
                return false;
            }
        }

        private EventGPI _gpi;
        public EventGPI GPI { get { return _gpi; } set { _gpi = value; } }

        public bool CanTriggerGPI
        {
            get { return _gpi.CanTrigger; }
            set { SetField(ref _gpi.CanTrigger, value, "CanTriggerGPI"); }
        }

        readonly Array _gPIParentals = Enum.GetValues(typeof(TParental));
        public Array GPIParentals { get { return _gPIParentals; } }
        public TParental GPIParental
        {
            get { return _gpi.Parental; }
            set { SetField(ref _gpi.Parental, value, "GPIParental"); }
        }

        readonly Array _gPILogos = Enum.GetValues(typeof(TLogo));
        public Array GPILogos { get { return _gPILogos; } }
        public TLogo GPILogo
        {
            get { return _gpi.Logo; }
            set { SetField(ref _gpi.Logo, value, "GPILogo"); }
        }

        readonly Array _gPICrawls = Enum.GetValues(typeof(TCrawl));
        public Array GPICrawls { get { return _gPICrawls; } }
        public TCrawl GPICrawl
        {
            get { return _gpi.Crawl; }
            set { SetField(ref _gpi.Crawl, value, "GPICrawl"); }
        }

        #endregion // GPI

        internal void _previewPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Media")
                InvalidateRequerySuggested();
        }

        private void _eventPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate()
            {
                bool oldModified = _modified;
                PropertyInfo sourcePi = sender.GetType().GetProperty(e.PropertyName);
                PropertyInfo destPi = this.GetType().GetProperty(e.PropertyName);
                if (sourcePi != null && destPi != null
                    && sourcePi.PropertyType.Equals(destPi.PropertyType))
                    destPi.SetValue(this, sourcePi.GetValue(sender, null), null);
                _modified = oldModified;
            });
            if (e.PropertyName == "GPI")
            {
                NotifyPropertyChanged("CanTriggerGPI");
                NotifyPropertyChanged("GPIParental");
                NotifyPropertyChanged("GPILogo");
                NotifyPropertyChanged("GPICrawl");
            }
            if (e.PropertyName == "PlayState")
            {
                NotifyPropertyChanged("IsEditEnabled");
                NotifyPropertyChanged("IsMovieOrLive");
                InvalidateRequerySuggested();
            }
            if (e.PropertyName == "AudioVolume")
            {
                NotifyPropertyChanged("AudioVolumeLevel");
                NotifyPropertyChanged("HasAudioVolume");
                NotifyPropertyChanged("AudioVolume");
            }
            if (e.PropertyName == "IsLoop")
            {
                InvalidateRequerySuggested();
            }
            if (e.PropertyName == "Next")
            {
                IsLoop = false;
                NotifyPropertyChanged("CanLoop");
            }
        }

        private void _onSubeventChanged(object o, CollectionOperationEventArgs<IEvent> e)
        {
        }

        private void _onRelocated(object o, EventArgs e)
        {
            NotifyPropertyChanged("StartType");
            NotifyPropertyChanged("BoundEventName");
            NotifyPropertyChanged("ScheduledTime");
            NotifyPropertyChanged("IsScheduledTimeEnabled");
        }

    }

}

