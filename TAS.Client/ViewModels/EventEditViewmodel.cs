using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.ComponentModel;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;
using System.Text.RegularExpressions;
using TAS.Common.Interfaces;
using resources = TAS.Client.Common.Properties.Resources;

namespace TAS.Client.ViewModels
{
    public class EventEditViewmodel : EditViewmodelBase<IEvent>, ITemplatedEdit, IDataErrorInfo
    {
        private readonly EngineViewmodel _engineViewModel;
        private IMedia _media;
        private bool _isVolumeChecking;
        private TEventType ModelType;
        private string ModelName;
        private string _command;
        private bool _isEnabled;
        private bool _isHold;
        private bool _isLoop;
        private TStartType _startType;
        private AutoStartFlags _autoStartFlags;
        private bool _bindToEnd;
        private TimeSpan _scheduledTc;
        private TTransitionType _transitionType;
        private TEasing _transitionEasing;
        private TimeSpan _transitionTime;
        private TimeSpan _transitionPauseTime;
        private double? _audioVolume;
        private DateTime _scheduledTime;
        private TimeSpan? _requestedStartTime;
        private TimeSpan _duration;
        private TimeSpan _scheduledDelay;
        private sbyte _layer;

        public static readonly Regex RegexMixerFill = new Regex(EventExtensions.MixerFillCommand, RegexOptions.IgnoreCase);
        public static readonly Regex RegexMixerClip = new Regex(EventExtensions.MixerClipCommand, RegexOptions.IgnoreCase);
        public static readonly Regex RegexMixerClear = new Regex(EventExtensions.MixerClearCommand, RegexOptions.IgnoreCase);
        public static readonly Regex RegexPlay = new Regex(EventExtensions.PlayCommand, RegexOptions.IgnoreCase);
        public static readonly Regex RegexCg = new Regex(EventExtensions.CgCommand, RegexOptions.IgnoreCase);

        public EventEditViewmodel(IEvent @event, EngineViewmodel engineViewModel): base(@event)
        {
            _engineViewModel = engineViewModel;
            _fields.CollectionChanged += _fields_or_commands_CollectionChanged;
            Model.PropertyChanged += ModelPropertyChanged;
            if (@event.EventType == TEventType.Container)
            {
                EventRightsEditViewmodel = new EventRightsEditViewmodel(@event, engineViewModel.Engine.AuthenticationService);
                EventRightsEditViewmodel.ModifiedChanged += RightsModifiedChanged;
            }
            CommandSaveEdit = new UICommand {ExecuteDelegate = Update, CanExecuteDelegate = _canSave};
            CommandUndoEdit = new UICommand {ExecuteDelegate = Load, CanExecuteDelegate = o => IsModified};
            CommandChangeMovie = new UICommand {ExecuteDelegate = _changeMovie, CanExecuteDelegate = _canChangeMovie};
            CommandEditMovie = new UICommand {ExecuteDelegate = _editMovie, CanExecuteDelegate = _canEditMovie};
            CommandCheckVolume = new UICommand {ExecuteDelegate = _checkVolume, CanExecuteDelegate = _canCheckVolume};
            CommandEditField = new UICommand {ExecuteDelegate = _editField};
            CommandTriggerStartType = new UICommand
            {
                ExecuteDelegate = _triggerStartType,
                CanExecuteDelegate = _canTriggerStartType
            };
            CommandMoveUp = new UICommand
            {
                ExecuteDelegate = o => Model?.MoveUp(),
                CanExecuteDelegate = o => Model.CanMoveUp()
            };
            CommandMoveDown = new UICommand
            {
                ExecuteDelegate = o => Model?.MoveDown(),
                CanExecuteDelegate = o => Model.CanMoveDown()
            };
            CommandDelete = new UICommand
            {
                ExecuteDelegate = o =>
                {
                    if (MessageBox.Show(resources._query_DeleteItem, resources._caption_Confirmation, MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                        return;
                    EventClipboard.SaveUndo(new List<IEvent> {Model},
                        Model.StartType == TStartType.After ? Model.Prior : Model.Parent);
                    Model.Delete();
                },
                CanExecuteDelegate = o => Model?.AllowDelete() == true
            };
        }

        public void Save()
        {
            Update();
        }

        public void UndoEdit()
        {
            Load();
        }
        
        protected override void Update(object destObject = null)
        {
            base.Update(Model);
            if (EventRightsEditViewmodel?.IsModified == true)
                EventRightsEditViewmodel.Save();
            Model.Save();
        }

        public ICommand CommandUndoEdit { get; }
        public ICommand CommandSaveEdit { get; }
        public ICommand CommandMoveUp { get; }
        public ICommand CommandMoveDown { get; }
        public ICommand CommandChangeMovie { get; }
        public ICommand CommandEditMovie { get; }
        public ICommand CommandCheckVolume { get; }
        public ICommand CommandTriggerStartType { get; }
        public ICommand CommandDelete { get; }

        public string Error => null;

        public string this[string propertyName]
        {
            get
            {
                string validationResult = null;
                switch (propertyName)
                {
                    case nameof(Duration):
                        validationResult = _validateDuration();
                        break;
                    case nameof(ScheduledTc):
                        validationResult = _validateScheduledTc();
                        break;
                    case nameof(ScheduledTime):
                    case nameof(ScheduledTimeOfDay):
                    case nameof(ScheduledDate):
                        validationResult = _validateScheduledTime();
                        break;
                    case nameof(TransitionTime):
                        validationResult = _validateTransitionTime();
                        break;
                    case nameof(TransitionPauseTime):
                        validationResult = _validateTransitionPauseTime();
                        break;
                    case nameof(ScheduledDelay):
                        validationResult = _validateScheduledDelay();
                        break;
                    case nameof(EventName):
                        validationResult = _validateEventName();
                        break;
                    case nameof(Command):
                        validationResult = IsValidCommand(_command)
                            ? string.Empty
                            : resources._validate_CommandSyntax;
                        break;
                }
                return validationResult;
            }
        }

        public IMedia Media
        {
            get => _media;
            set => SetField(ref _media, value);
        }

        public bool IsVolumeChecking
        {
            get => _isVolumeChecking;
            set
            {
                if (base.SetField(ref _isVolumeChecking, value)) //not set Modified
                    InvalidateRequerySuggested();
            }
        }

        public TEventType EventType
        {
            get => ModelType;
            set => SetField(ref ModelType, value);
        }

        public string EventName
        {
            get => ModelName;
            set => SetField(ref ModelName, value);
        }

        public bool IsEditEnabled
        {
            get
            {
                var ev = Model;
                return ev != null && ev.PlayState == TPlayState.Scheduled && Model.HaveRight(EventRight.Modify);
            }
        }

        public bool IsAutoStartEvent => _startType == TStartType.OnFixedTime;

        public bool IsMovieOrLive
        {
            get
            {
                var et = Model?.EventType;
                return et == TEventType.Movie || et == TEventType.Live;
            }
        }

        public bool IsMovieOrLiveOrRundown
        {
            get
            {
                var et = Model?.EventType;
                var st = Model?.StartType;
                return (et == TEventType.Movie || et == TEventType.Live || et == TEventType.Rundown)
                       && (st == TStartType.After || st == TStartType.WithParent || st == TStartType.WithParentFromEnd);
            }
        }

        public bool IsAnimation
        {
            get { return ModelType == TEventType.Animation; }
        }

        public bool IsCommandScript => Model is ICommandScript;

        #region ICGElementsState

        bool _isCGEnabled;
        byte _crawl;
        byte _logo;
        byte _parental;

        public bool IsCGEnabled
        {
            get { return _isCGEnabled; }
            set { SetField(ref _isCGEnabled, value); }
        }

        public byte Crawl
        {
            get { return _crawl; }
            set { SetField(ref _crawl, value); }
        }

        public byte Logo
        {
            get { return _logo; }
            set { SetField(ref _logo, value); }
        }

        public byte Parental
        {
            get { return _parental; }
            set { SetField(ref _parental, value); }
        }

        #endregion ICGElementsState

        #region ITemplatedEdit

        private int _templateLayer;

        public bool IsDisplayCgMethod { get; } = true;

        public int TemplateLayer
        {
            get { return _templateLayer; }
            set { SetField(ref _templateLayer, value); }
        }

        public object SelectedField { get; set; }

        private readonly ObservableDictionary<string, string> _fields = new ObservableDictionary<string, string>();

        public Dictionary<string, string> Fields
        {
            get => new Dictionary<string, string>(_fields);
            set
            {
                _fields.Clear();
                if (value != null)
                    _fields.AddRange(value);
            }
        }

        public Array Methods { get; } = Enum.GetValues(typeof(TemplateMethod));

        private TemplateMethod _method;

        public TemplateMethod Method
        {
            get => _method;
            set => SetField(ref _method, value);
        }

        public bool IsKeyReadOnly => true;

        public ICommand CommandEditField { get; }

        public ICommand CommandAddField { get; } = null;

        public ICommand CommandDeleteField { get; } = null;

        #endregion //ITemplatedEdit

        public string Command
        {
            get { return _command; }
            set { SetField(ref _command, value); }
        }

        public bool IsMovie => Model?.EventType == TEventType.Movie;

        public bool IsStillImage => Model?.EventType == TEventType.StillImage;

        public bool IsTransitionPanelEnabled
        {
            get
            {
                var et = Model?.EventType;
                return !_isHold && (et == TEventType.Live || et == TEventType.Movie);
            }
        }

        public bool IsTransitionPropertiesVisible => _transitionType != TTransitionType.Cut;

        public bool IsEmpty => Model == null;

        public bool IsContainer => Model?.EventType == TEventType.Container;

        public bool CanHold => Model?.Prior != null;

        public bool CanLoop => Model!= null && Model.GetSuccessor() == null;

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { SetField(ref _isEnabled, value); }
        }

        public bool IsHold
        {
            get { return _isHold; }
            set
            {
                if (SetField(ref _isHold, value))
                {
                    if (value)
                        TransitionTime = TimeSpan.Zero;
                    NotifyPropertyChanged(nameof(IsTransitionPanelEnabled));
                }
            }
        }

        public bool IsLoop
        {
            get { return _isLoop; }
            set { SetField(ref _isLoop, value); }
        }

        public TStartType StartType
        {
            get { return _startType; }
            set
            {
                if (SetField(ref _startType, value))
                {
                    BindToEnd = value == TStartType.WithParentFromEnd;
                    NotifyPropertyChanged(nameof(IsAutoStartEvent));
                }
            }
        }

        public AutoStartFlags AutoStartFlags
        {
            get { return _autoStartFlags; }
            set
            {
                if (SetField(ref _autoStartFlags, value))
                {
                    NotifyPropertyChanged(nameof(AutoStartForced));
                    NotifyPropertyChanged(nameof(AutoStartDaily));
                    NotifyPropertyChanged(nameof(IsScheduledDateVisible));
                }
            }
        }

        public bool AutoStartForced
        {
            get => (_autoStartFlags & AutoStartFlags.Force) != AutoStartFlags.None;
            set
            {
                if (value)
                    AutoStartFlags = AutoStartFlags | AutoStartFlags.Force;
                else
                    AutoStartFlags = AutoStartFlags & ~AutoStartFlags.Force;
            }
        }

        public bool AutoStartDaily
        {
            get { return (_autoStartFlags & AutoStartFlags.Daily) != AutoStartFlags.None; }
            set
            {
                if (value != AutoStartDaily)
                {
                    if (value)
                        AutoStartFlags = AutoStartFlags | AutoStartFlags.Daily;
                    else
                        AutoStartFlags = AutoStartFlags & ~AutoStartFlags.Daily;
                }
            }
        }

        public string BoundEventName
        {
            get
            {
                var st = Model.StartType;
                var boundEvent = st == TStartType.WithParent || st == TStartType.WithParentFromEnd
                    ? Model.Parent
                    : (st == TStartType.After)
                        ? Model.Prior
                        : null;
                return boundEvent == null ? string.Empty : boundEvent.EventName;
            }
        }

        public bool BindToEnd
        {
            get { return _bindToEnd; }
            set
            {
                if (SetField(ref _bindToEnd, value))
                {
                    if (_startType == TStartType.WithParent || _startType == TStartType.WithParentFromEnd)
                    {
                        if (value)
                        {
                            StartType = TStartType.WithParentFromEnd;
                        }
                        else
                        {
                            StartType = TStartType.WithParent;
                        }
                    }
                }
            }
        }

        public bool IsDisplayBindToEnd
        {
            get
            {
                return (ModelType == TEventType.Animation || ModelType == TEventType.CommandScript ||
                        ModelType == TEventType.StillImage)
                       && (_startType == TStartType.WithParent || _startType == TStartType.WithParentFromEnd);
            }
        }

        public TimeSpan ScheduledTc
        {
            get { return _scheduledTc; }
            set
            {
                SetField(ref _scheduledTc, value);
                NotifyPropertyChanged(nameof(Duration));
            }
        }

        public Array TransitionTypes { get; } = Enum.GetValues(typeof(TTransitionType));

        public Array TransitionEasings { get; } = Enum.GetValues(typeof(TEasing));

        public TTransitionType TransitionType
        {
            get { return _transitionType; }
            set
            {
                if (SetField(ref _transitionType, value))
                {
                    if (value == TTransitionType.Cut)
                    {
                        TransitionTime = TimeSpan.Zero;
                        TransitionPauseTime = TimeSpan.Zero;
                    }
                    NotifyPropertyChanged(nameof(IsTransitionPropertiesVisible));
                }
            }
        }

        public TEasing TransitionEasing
        {
            get { return _transitionEasing; }
            set { SetField(ref _transitionEasing, value); }
        }

        public TimeSpan TransitionTime
        {
            get { return _transitionTime; }
            set { SetField(ref _transitionTime, value); }
        }

        public TimeSpan TransitionPauseTime
        {
            get { return _transitionPauseTime; }
            set { SetField(ref _transitionPauseTime, value); }
        }

        public double? AudioVolume
        {
            get => _audioVolume;
            set
            {
                if (!SetField(ref _audioVolume, value))
                    return;
                NotifyPropertyChanged(nameof(HasAudioVolume));
                NotifyPropertyChanged(nameof(AudioVolumeLevel));
            }
        }

        public double AudioVolumeLevel
        {
            get => _audioVolume ?? (_media?.AudioVolume ?? 0);
            set
            {
                if (!SetField(ref _audioVolume, value))
                    return;
                NotifyPropertyChanged(nameof(HasAudioVolume));
                NotifyPropertyChanged(nameof(AudioVolume));
            }
        }

        public bool HasAudioVolume
        {
            get => _audioVolume != null;
            set
            {
                if (!SetField(ref _audioVolume,
                    value ? (_media != null ? (double?) _media.AudioVolume : 0) : null)) return;
                NotifyPropertyChanged(nameof(AudioVolume));
                NotifyPropertyChanged(nameof(AudioVolumeLevel));
            }
        }

        public DateTime ScheduledTime
        {
            get { return _scheduledTime; }
            set
            {
                if (SetField(ref _scheduledTime, value))
                {
                    NotifyPropertyChanged(nameof(ScheduledDate));
                    NotifyPropertyChanged(nameof(ScheduledTimeOfDay));
                }
            }
        }

        public DateTime ScheduledDate
        {
            get { return _scheduledTime.ToLocalTime().Date; }
            set
            {
                if (!value.Equals(ScheduledDate))
                    ScheduledTime = (value.Date + ScheduledTimeOfDay).ToUniversalTime();
            }
        }

        public TimeSpan ScheduledTimeOfDay
        {
            get { return _scheduledTime.ToLocalTime().TimeOfDay; }
            set
            {
                if (!value.Equals(ScheduledTimeOfDay))
                    ScheduledTime = new DateTime(value.Ticks + ScheduledDate.Ticks).ToUniversalTime();
            }
        }

        public bool IsScheduledDateVisible => _startType != TStartType.OnFixedTime || !AutoStartDaily;

        public TimeSpan? RequestedStartTime
        {
            get { return _requestedStartTime; }
            set { SetField(ref _requestedStartTime, value); }
        }

        public TimeSpan Duration
        {
            get { return _duration; }
            set
            {
                SetField(ref _duration, value);
                NotifyPropertyChanged(nameof(ScheduledTc));
            }
        }

        public TVideoFormat VideoFormat => _engineViewModel.VideoFormat;

        public TimeSpan ScheduledDelay
        {
            get { return _scheduledDelay; }
            set { SetField(ref _scheduledDelay, value); }
        }

        public sbyte Layer
        {
            get { return _layer; }
            set { SetField(ref _layer, value); }
        }

        public bool IsStartEvent
        {
            get
            {
                var st = Model?.StartType;
                return (st == TStartType.OnFixedTime || st == TStartType.Manual);
            }
        }

        public bool IsDurationEnabled => Model.EventType != TEventType.Rundown;

        public bool IsCGElementsEnabled => Model.EventType == TEventType.Live || Model.EventType == TEventType.Movie;

        public bool IsDisplayCGElements => Model.Engine.CGElementsController != null;

        public IEnumerable<ICGElement> Logos => Model.Engine.CGElementsController?.Logos;

        public IEnumerable<ICGElement> Crawls => Model.Engine.CGElementsController?.Crawls;

        public IEnumerable<ICGElement> Parentals => Model.Engine.CGElementsController?.Parentals;

        public EventRightsEditViewmodel EventRightsEditViewmodel { get; }

        public override string ToString()
        {
            return $"{Infralution.Localization.Wpf.ResourceEnumConverter.ConvertToString(EventType)} - {EventName}";
        }

        internal void _previewPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Media))
                InvalidateRequerySuggested();
        }

        protected override void OnDispose()
        {
            Model.PropertyChanged -= ModelPropertyChanged;
            if (EventRightsEditViewmodel == null)
                return;
            EventRightsEditViewmodel.ModifiedChanged -= RightsModifiedChanged;
            EventRightsEditViewmodel.Dispose();
        }
       
        #region Command methods

        private void _triggerStartType(object obj)
        {
            if (StartType == TStartType.Manual)
                StartType = TStartType.OnFixedTime;
            else if (StartType == TStartType.OnFixedTime)
                StartType = TStartType.Manual;
        }

        private bool _canTriggerStartType(object obj)
        {
            return (StartType == TStartType.Manual || StartType == TStartType.OnFixedTime)
                   && Model.HaveRight(EventRight.Modify);
        }

        private void _editField(object obj)
        {
            var editObject = obj ?? SelectedField;
            if (editObject != null)
            {
                using (var kve = new KeyValueEditViewmodel((KeyValuePair<string, string>) editObject, false))
                {
                    if (UiServices.ShowDialog<Views.KeyValueEditView>(kve) == true)
                        _fields[kve.Key] = kve.Value;
                }
            }
        }

        private void _changeMovie(object o)
        {
            IEvent ev = Model;
            if (ev != null
                && ev.EventType == TEventType.Movie)
            {
                _chooseMedia(TMediaType.Movie, ev, ev.StartType);
            }
        }

        private void _editMovie(object obj)
        {
            using (var evm = new MediaEditWindowViewmodel(Model.Media, Model.Engine.MediaManager) )
            {
                if (UiServices.ShowDialog<Views.MediaEditWindowView>(evm) == true)
                    evm.Editor.Save();
            }
        }

        private void _checkVolume(object obj)
        {
            if (_media == null)
                return;
            IsVolumeChecking = true;
            var fileManager = Model.Engine.MediaManager.FileManager;
            var operation = fileManager.CreateLoudnessOperation();
            operation.Source = Model.Media;
            operation.MeasureStart = Model.ScheduledTc - _media.TcStart;
            operation.MeasureDuration = Model.Duration;
            operation.AudioVolumeMeasured += _audioVolumeMeasured;
            operation.Finished += _audioVolumeFinished;
            fileManager.Queue(operation, true);
        }

        private void _audioVolumeFinished(object sender, EventArgs e)
        {
            IsVolumeChecking = false;
            ((ILoudnessOperation) sender).Finished -= _audioVolumeFinished;
            ((ILoudnessOperation) sender).AudioVolumeMeasured -= _audioVolumeFinished;
        }

        private void _audioVolumeMeasured(object sender, AudioVolumeEventArgs e)
        {
            AudioVolume = e.AudioVolume;
        }

        private bool _canChangeMovie(object o)
        {
            return Model != null
                   && Model.PlayState == TPlayState.Scheduled
                   && Model.EventType == TEventType.Movie
                   && Model.HaveRight(EventRight.Modify);
        }

        private bool _canEditMovie(object o)
        {
            return Model.Media != null
                   && Model.PlayState == TPlayState.Scheduled
                   && Model.EventType == TEventType.Movie
                   && Model.Engine.HaveRight(EngineRight.MediaEdit);
        }

        private bool _canCheckVolume(object o)
        {
            return !_isVolumeChecking && _canChangeMovie(o);
        }

        private bool _canSave(object o)
        {
            return Model != null
                   && (IsModified || Model.IsModified)
                   && IsValid
                   && Model.HaveRight(EventRight.Modify);
        }

        private void _setCGElements(IMedia media)
        {
            IsCGEnabled = Model.Engine.EnableCGElementsForNewEvents;
            if (media != null)
            {
                var category = media.MediaCategory;
                Logo = (byte) (category == TMediaCategory.Fill || category == TMediaCategory.Show ||
                               category == TMediaCategory.Promo || category == TMediaCategory.Insert ||
                               category == TMediaCategory.Jingle
                    ? 1
                    : 0);
                Parental = media.Parental;
            }
        }

        #endregion // command methods

        private void _fields_or_commands_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            IsModified = true;
        }

        private void ModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!(sender is IEvent s))
                return;
            Application.Current.Dispatcher.BeginInvoke((Action) delegate
            {
                switch (e.PropertyName)
                {
                    case nameof(IEvent.AudioVolume):
                        _audioVolume = s.AudioVolume;
                        NotifyPropertyChanged(nameof(AudioVolume));
                        NotifyPropertyChanged(nameof(AudioVolumeLevel));
                        NotifyPropertyChanged(nameof(HasAudioVolume));
                        break;
                    case nameof(IEvent.AutoStartFlags):
                        _autoStartFlags = s.AutoStartFlags;
                        NotifyPropertyChanged(nameof(AutoStartForced));
                        NotifyPropertyChanged(nameof(AutoStartDaily));
                        break;
                    case nameof(IEvent.Crawl):
                        _crawl = s.Crawl;
                        NotifyPropertyChanged(nameof(Crawl));
                        break;
                    case nameof(IEvent.Logo):
                        _logo = s.Logo;
                        NotifyPropertyChanged(nameof(Logo));
                        break;
                    case nameof(IEvent.Parental):
                        _parental = s.Parental;
                        NotifyPropertyChanged(nameof(Parental));
                        break;
                    case nameof(IEvent.Duration):
                        _duration = s.Duration;
                        NotifyPropertyChanged(nameof(Duration));
                        break;
                    case nameof(IEvent.EventName):
                        ModelName = s.EventName;
                        NotifyPropertyChanged(nameof(EventName));
                        break;
                    case nameof(IEvent.IdAux):
                    case nameof(IEvent.IdProgramme):
                        break;
                    case nameof(IEvent.IsCGEnabled):
                        _isCGEnabled = s.IsCGEnabled;
                        NotifyPropertyChanged(nameof(IsCGEnabled));
                        break;
                    case nameof(IEvent.IsEnabled):
                        _isEnabled = s.IsEnabled;
                        NotifyPropertyChanged(nameof(_isEnabled));
                        break;
                    case nameof(IEvent.IsHold):
                        _isHold = s.IsHold;
                        NotifyPropertyChanged(nameof(IsHold));
                        break;
                    case nameof(IEvent.Media):
                        _media = s.Media;
                        NotifyPropertyChanged(nameof(Media));
                        break;
                    case nameof(IEvent.IsLoop):
                        _isLoop = s.IsLoop;
                        NotifyPropertyChanged(nameof(IsLoop));
                        InvalidateRequerySuggested();
                        break;
                    case nameof(IEvent.RequestedStartTime):
                        _requestedStartTime = s.RequestedStartTime;
                        NotifyPropertyChanged(nameof(RequestedStartTime));
                        break;
                    case nameof(IEvent.Offset):
                        break;
                    case nameof(IEvent.ScheduledTime):
                        _scheduledTime = s.ScheduledTime;
                        NotifyPropertyChanged(nameof(ScheduledTime));
                        break;
                    case nameof(IEvent.ScheduledTc):
                        _scheduledTc = s.ScheduledTc;
                        NotifyPropertyChanged(nameof(ScheduledTc));
                        break;
                    case nameof(IEvent.ScheduledDelay):
                        _scheduledDelay = s.ScheduledDelay;
                        NotifyPropertyChanged(nameof(ScheduledDelay));
                        break;
                    case nameof(IEvent.PlayState):
                        NotifyPropertyChanged(nameof(IsEditEnabled));
                        break;
                    case nameof(IEvent.StartType):
                        _startType = s.StartType;
                        NotifyPropertyChanged(nameof(StartType));
                        NotifyPropertyChanged(nameof(IsAutoStartEvent));
                        break;
                    case nameof(IEvent.TransitionEasing):
                        _transitionEasing = s.TransitionEasing;
                        NotifyPropertyChanged(nameof(TransitionEasing));
                        break;
                    case nameof(IEvent.TransitionType):
                        _transitionType = s.TransitionType;
                        NotifyPropertyChanged(nameof(TransitionType));
                        break;
                    case nameof(IEvent.TransitionTime):
                        _transitionTime = s.TransitionTime;
                        NotifyPropertyChanged(nameof(TransitionTime));
                        break;
                    case nameof(IEvent.TransitionPauseTime):
                        _transitionPauseTime = s.TransitionPauseTime;
                        NotifyPropertyChanged(nameof(TransitionPauseTime));
                        break;
                    case nameof(IEvent.Prior):
                    case nameof(IEvent.Parent):
                        NotifyPropertyChanged(nameof(BoundEventName));
                        break;
                }
                if (s is ITemplated t)
                    switch (e.PropertyName)
                    {
                        case nameof(ITemplated.Method):
                            _method = t.Method;
                            NotifyPropertyChanged(nameof(Method));
                            break;
                        case nameof(ITemplated.TemplateLayer):
                            _templateLayer = t.TemplateLayer;
                            NotifyPropertyChanged(nameof(TemplateLayer));
                            break;
                        case nameof(ITemplated.Fields):
                            _fields.Clear();
                            if (t.Fields != null)
                                _fields.AddRange(t.Fields);
                            break;
                    }
            });
        }


        private string _validateEventName()
        {
            var ev = Model;
            if (ev != null && ev.FieldLengths.TryGetValue(nameof(IEvent.EventName), out var length) && EventName.Length > length)
                return resources._validate_TextTooLong;
            return null;
        }

        private string _validateScheduledDelay()
        {
            var ev = Model;
            if (ev == null || 
                (ev.EventType != TEventType.StillImage && ev.EventType != TEventType.CommandScript))
                return null;
            var parent = ev.Parent;
            if (parent != null && _duration + _scheduledDelay > parent.Duration)
                return resources._validate_ScheduledDelayInvalid;
            return null;
        }

        private string _validateScheduledTime()
        {
            var ev = Model;
            if (ev != null && ((_startType == TStartType.OnFixedTime && (_autoStartFlags & AutoStartFlags.Daily) == AutoStartFlags.None)
                               || _startType == TStartType.Manual) && ev.PlayState == TPlayState.Scheduled && _scheduledTime < ev.Engine.CurrentTime)
                return resources._validate_StartTimePassed;
            return null;
        }

        private string _validateScheduledTc()
        {
            var ev = Model;
            if (ev == null)
                return null;
            var media = Model.Media;
            if (ev.EventType != TEventType.Movie || media == null)
                return null;
            if (_scheduledTc > media.Duration + media.TcStart)
                return string.Format(resources._validate_StartTCAfterFile,
                    (media.Duration + media.TcStart).ToSMPTETimecodeString(Model.Engine.VideoFormat));
            if (_scheduledTc < media.TcStart)
                return string.Format(resources._validate_StartTCBeforeFile,
                    media.TcStart.ToSMPTETimecodeString(Model.Engine.VideoFormat));
            return null;
        }

        private string _validateDuration()
        {
            var ev = Model;
            if (ev == null)
                return null;
            var media = Model.Media;
            if (ev.EventType == TEventType.Movie && media != null
                && _duration + _scheduledTc > media.Duration + media.TcStart)
                return resources._validate_DurationInvalid;
            if (ev.EventType != TEventType.StillImage && ev.EventType != TEventType.CommandScript)
                return null;
            var parent = ev.Parent;
            if (parent != null && _duration + _scheduledDelay > parent.Duration)
                return resources._validate_ScheduledDelayInvalid;
            return null;
        }

        private string _validateTransitionPauseTime()
        {
            if (_transitionPauseTime > _transitionTime)
                return resources._validate_TransitionPauseTimeInvalid;
            return null;
        }

        private string _validateTransitionTime()
        {
            if (_transitionTime > _duration)
                return resources._validate_TransitionTimeInvalid;
            return null;
        }

        private void _chooseMedia(TMediaType mediaType, IEvent baseEvent, TStartType startType,
            VideoFormatDescription videoFormatDescription = null)
        {
            using (var vm = new MediaSearchViewmodel(
                _engineViewModel.Engine.HaveRight(EngineRight.Preview) ? _engineViewModel.Engine : null,
                Model.Engine,
                mediaType, VideoLayer.Program, true, videoFormatDescription)
            {
                BaseEvent = baseEvent,
                NewEventStartType = startType
            })
            {
                if (UiServices.ShowDialog<Views.MediaSearchView>(vm) == true)
                {
                    if (!(vm.SelectedMedia is IServerMedia media))
                        return;
                    Media = media;
                    Duration = media.DurationPlay;
                    ScheduledTc = media.TcPlay;
                    AudioVolume = null;
                    EventName = media.MediaName;
                    _setCGElements(media);
                }
            }

        }

        public bool IsValid => (from pi in GetType().GetProperties() select this[pi.Name]).All(string.IsNullOrEmpty);

        private bool IsValidCommand(string commandText)
        {
            return string.IsNullOrWhiteSpace(commandText)
                   || RegexPlay.IsMatch(commandText)
                   || RegexMixerFill.IsMatch(commandText)
                   || RegexMixerClip.IsMatch(commandText)
                   || RegexMixerClear.IsMatch(commandText)
                   || RegexCg.IsMatch(commandText)
                ;
        }

        private void RightsModifiedChanged(object sender, EventArgs e)
        {
            IsModified = true;
        }

    }

}

