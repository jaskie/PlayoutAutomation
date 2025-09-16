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
using TAS.Common.Interfaces.Media;
using System.Threading.Tasks;
using resources = TAS.Client.Common.Properties.Resources;

namespace TAS.Client.ViewModels
{
    public class EventEditViewmodel : EditViewmodelBase<IEvent>, IDataErrorInfo
    {
        private readonly EngineViewmodel _engineViewModel;
        private IMedia _media;
        private bool _isVolumeChecking;
        private string _eventName;
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
        private bool _isEventNameFocused;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static readonly Regex RegexMixerFill = new Regex(TAS.Common.IEventExtensions.MixerFillCommand, RegexOptions.IgnoreCase);
        public static readonly Regex RegexMixerClip = new Regex(TAS.Common.IEventExtensions.MixerClipCommand, RegexOptions.IgnoreCase);
        public static readonly Regex RegexMixerClear = new Regex(TAS.Common.IEventExtensions.MixerClearCommand, RegexOptions.IgnoreCase);
        public static readonly Regex RegexPlay = new Regex(TAS.Common.IEventExtensions.PlayCommand, RegexOptions.IgnoreCase);
        public static readonly Regex RegexCg = new Regex(TAS.Common.IEventExtensions.CgCommand, RegexOptions.IgnoreCase);

        public EventEditViewmodel(IEvent @event, EngineViewmodel engineViewModel): base(@event)
        {
            _engineViewModel = engineViewModel;
            Model.PropertyChanged += ModelPropertyChanged;
            if (@event.EventType == TEventType.Container)
            {
                EventRightsEditViewmodel = new EventRightsEditViewmodel(@event, engineViewModel.Engine.AuthenticationService);
                EventRightsEditViewmodel.ModifiedChanged += RightsModifiedChanged;
            }
            Router = engineViewModel.Router;
            if (Router != null)
            {
                InputPorts.Add(new DummyRouterPort()); // "do not change the input" value
                foreach (var input in Router.InputPorts)
                    InputPorts.Add(input);
                _selectedInputPort = InputPorts.FirstOrDefault(x => x.PortId == RouterPort);
            }

            if (@event.EventType == TEventType.Live && Model.Engine.MediaManager.Recorders.Count() > 0)
            {
                RecordingInfoViewmodel = new RecordingInfoViewModel(@event.Engine, @event.RecordingInfo);
                RecordingInfoViewmodel.ModifiedChanged += RecordingInfoViewmodel_ModifiedChanged;
            }

            CommandSaveEdit = new UiCommand(CommandName(nameof(Save)), _ => Save(), _ => CanSave);
            CommandUndoEdit = new UiCommand(CommandName(nameof(UndoEdit)), _ => UndoEdit(), _ => IsModified);
            CommandChangeMovie = new UiCommand(CommandName(nameof(ChangeMovie)), ChangeMovie, CanChangeMovie);
            CommandEditMovie = new UiCommand(CommandName(nameof(EditMovie)), EditMovie, CanEditMovie);
            CommandCheckVolume = new UiCommand(CommandName(nameof(CheckVolume)), CheckVolume, CanCheckVolume);
            CommandTriggerStartType = new UiCommand(CommandName(nameof(TriggerStartType)), TriggerStartType, CanTriggerStartType);
            CommandMoveUp = new UiCommand(CommandName(nameof(Model.MoveUp)), _ => Model.MoveUp(), _ => Model.CanMoveUp());
            CommandMoveDown = new UiCommand(CommandName(nameof(Model.MoveDown)), _ => Model.MoveDown(), _ => Model.CanMoveDown());
            CommandDelete = new UiCommand
            (
                CommandName(nameof(Model.Delete)),
                _ =>
                {
                    if (MessageBox.Show(resources._query_DeleteItem, resources._caption_Confirmation, MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                        return;
                    EventClipboard.SaveUndo(new List<IEvent> {Model},
                        Model.StartType == TStartType.After ? Model.GetPrior() : Model.GetParent());
                    Task.Run(() =>
                    {
                        Model.Delete();
                        Logger.LogEventDeletion(Model);
                    });
                },
                _ => Model.HaveRight(EventRight.Delete) && Model.AllowDelete()
            );
            if (@event is ITemplated templated)
            {
                TemplatedEditViewmodel = new TemplatedEditViewmodel(templated, true, true, engineViewModel.VideoFormat);
                TemplatedEditViewmodel.ModifiedChanged += TemplatedEditViewmodel_ModifiedChanged;
            }
        }

        private void RecordingInfoViewmodel_ModifiedChanged(object sender, EventArgs e)
        {
            IsModified = true;
        }

        public void Save()
        {
            Update();
        }

        public void UndoEdit()
        {
            RecordingInfoViewmodel?.Load();
            TemplatedEditViewmodel?.UndoEdit();
            EventRightsEditViewmodel?.UndoEdit();
            Load();
        }
        
        protected override void Update(object destObject = null)
        {
            Model.RecordingInfo = RecordingInfoViewmodel?.GetRecordingInfo();
            base.Update(Model);
            EventRightsEditViewmodel?.Save();
            TemplatedEditViewmodel?.Save();
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
                if (!IsModified)
                    return null;
                switch (propertyName)
                {
                    case nameof(Duration):
                        return ValidateDuration();
                    case nameof(ScheduledTc):
                        return ValidateScheduledTc();
                    case nameof(ScheduledTime):
                    case nameof(ScheduledTimeOfDay):
                    case nameof(ScheduledDate):
                        return ValidateScheduledTime();
                    case nameof(TransitionTime):
                        return ValidateTransitionTime();
                    case nameof(TransitionPauseTime):
                        return ValidateTransitionPauseTime();
                    case nameof(ScheduledDelay):
                        return ValidateScheduledDelay();
                    case nameof(EventName):
                        return ValidateEventName();
                    case nameof(RecordingInfoViewmodel):
                        return ValidateRecordingInfo();
                    case nameof(Command):
                        return IsValidCommand(_command)
                            ? string.Empty
                            : resources._validate_CommandSyntax;
                    default:
                        return null;
                }
            }
        }        

        public IMedia Media
        {
            get => _media;
            set
            {
                var oldMedia = _media;
                if (!SetField(ref _media, value))
                    return;
                if (oldMedia != null)
                    oldMedia.PropertyChanged -= OnMediaPropertyChanged;
                if (value != null)
                    value.PropertyChanged += OnMediaPropertyChanged;
            }
        }

        public bool IsVolumeChecking
        {
            get => _isVolumeChecking;
            set
            {
                if (_isVolumeChecking != value)
                    return;
                _isVolumeChecking = value;
                NotifyPropertyChanged();
                InvalidateRequerySuggested();
            }
        }

        public TEventType EventType => Model.EventType;

        public string EventName
        {
            get => _eventName;
            set => SetField(ref _eventName, value);
        }

        public bool IsEditEnabled => Model.PlayState == TPlayState.Scheduled && Model.HaveRight(EventRight.Modify);

        public bool IsAutoStartEvent => _startType == TStartType.OnFixedTime;

        public bool IsMovieOrLive => Model.IsMovieOrLive();

        public bool IsLive => Model.EventType == TEventType.Live;

        public bool IsMovieOrLiveOrRundown => Model.IsMovieOrLiveOrRundown();

        public bool IsCommandScript => Model is ICommandScript;

        #region ICGElementsState

        bool _isCGEnabled;
        byte _crawl;
        byte _logo;
        byte _parental;

        public bool IsCGEnabled
        {
            get => _isCGEnabled;
            set => SetField(ref _isCGEnabled, value);
        }

        public byte Crawl
        {
            get => _crawl;
            set => SetField(ref _crawl, value);
        }

        public byte Logo
        {
            get => _logo;
            set => SetField(ref _logo, value);
        }

        public byte Parental
        {
            get => _parental;
            set => SetField(ref _parental, value);
        }

        #endregion ICGElementsState

        
        public string Command
        {
            get => _command;
            set => SetField(ref _command, value);
        }

        public bool IsMovie => Model.EventType == TEventType.Movie;

        public bool IsStillImage => Model.EventType == TEventType.StillImage;

        public bool IsTransitionPanelEnabled => !_isHold && (Model.IsMovieOrLive());

        public bool IsTransitionPropertiesVisible => _transitionType != TTransitionType.Cut;

        public bool IsContainer => Model.EventType == TEventType.Container;

        public bool CanHold => Model.GetPrior() != null;

        public bool CanLoop => Model.GetSuccessor() == null;

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetField(ref _isEnabled, value);
        }

        public bool IsHold
        {
            get => _isHold;
            set
            {
                if (!SetField(ref _isHold, value))
                    return;
                if (value)
                    TransitionTime = TimeSpan.Zero;
                NotifyPropertyChanged(nameof(IsTransitionPanelEnabled));
            }
        }

        public bool IsLoop
        {
            get => _isLoop;
            set => SetField(ref _isLoop, value);
        }

        public TStartType StartType
        {
            get => _startType;
            set
            {
                if (!SetField(ref _startType, value))
                    return;
                _bindToEnd = value == TStartType.WithParentFromEnd;
                NotifyPropertyChanged(nameof(BindToEnd));
                NotifyPropertyChanged(nameof(IsAutoStartEvent));
            }
        }

        public AutoStartFlags AutoStartFlags
        {
            get => _autoStartFlags;
            set
            {
                if (!SetField(ref _autoStartFlags, value))
                    return;
                NotifyPropertyChanged(nameof(AutoStartForced));
                NotifyPropertyChanged(nameof(AutoStartDaily));
                NotifyPropertyChanged(nameof(IsScheduledDateVisible));
            }
        }

        public bool AutoStartForced
        {
            get => (_autoStartFlags & AutoStartFlags.Force) != AutoStartFlags.None;
            set
            {
                if (value == AutoStartForced)
                    return;
                if (value)
                    AutoStartFlags = AutoStartFlags | AutoStartFlags.Force;
                else
                    AutoStartFlags = AutoStartFlags & ~AutoStartFlags.Force;
            }
        }

        public bool AutoStartDaily
        {
            get => (_autoStartFlags & AutoStartFlags.Daily) != AutoStartFlags.None;
            set
            {
                if (value == AutoStartDaily)
                    return;
                if (value)
                    AutoStartFlags = AutoStartFlags | AutoStartFlags.Daily;
                else
                    AutoStartFlags = AutoStartFlags & ~AutoStartFlags.Daily;
            }
        }

        public string BoundEventName
        {
            get
            {
                var st = Model.StartType;
                var boundEvent = st == TStartType.WithParent || st == TStartType.WithParentFromEnd
                    ? Model.GetParent()
                    : (st == TStartType.After)
                        ? Model.GetPrior()
                        : null;
                return boundEvent == null ? string.Empty : boundEvent.EventName;
            }
        }

        public bool BindToEnd
        {
            get => _bindToEnd;
            set
            {
                if (!SetField(ref _bindToEnd, value)) return;
                if (_startType != TStartType.WithParent && _startType != TStartType.WithParentFromEnd) return;
                StartType = value ? TStartType.WithParentFromEnd : TStartType.WithParent;
            }
        }

        public bool IsDisplayBindToEnd => (Model.EventType == TEventType.StillImage || Model.EventType == TEventType.CommandScript)
                                          && (_startType == TStartType.WithParent || _startType == TStartType.WithParentFromEnd);

        public bool IsEventNameFocused
        {
            get => _isEventNameFocused;
            set
            {
                if (_isEventNameFocused == value)
                    return;
                _isEventNameFocused = value;
                NotifyPropertyChanged();
            }
        }

        public TimeSpan ScheduledTc
        {
            get => _scheduledTc;
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
            get => _transitionType;
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
            get => _transitionEasing;
            set => SetField(ref _transitionEasing, value);
        }

        public TimeSpan TransitionTime
        {
            get => _transitionTime;
            set => SetField(ref _transitionTime, value);
        }

        public TimeSpan TransitionPauseTime
        {
            get => _transitionPauseTime;
            set => SetField(ref _transitionPauseTime, value);
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
            get => _scheduledTime;
            set
            {
                if (!SetField(ref _scheduledTime, value.ToUniversalTime()))
                    return;
                NotifyPropertyChanged(nameof(ScheduledDate));
                NotifyPropertyChanged(nameof(ScheduledTimeOfDay));
            }
        }

        public DateTime ScheduledDate
        {
            get => _scheduledTime.ToLocalTime().Date;
            set
            {
                if (!value.Equals(ScheduledDate))
                    ScheduledTime = (value.Date + ScheduledTimeOfDay).ToUniversalTime();
            }
        }

        public TimeSpan ScheduledTimeOfDay
        {
            get => _scheduledTime.ToLocalTime().TimeOfDay;
            set
            {
                if (!value.Equals(ScheduledTimeOfDay))
                    ScheduledTime = new DateTime(value.Ticks + ScheduledDate.Ticks).ToUniversalTime();
            }
        }

        public bool IsScheduledDateVisible => _startType != TStartType.OnFixedTime || !AutoStartDaily;

        public TimeSpan? RequestedStartTime
        {
            get => _requestedStartTime;
            set => SetField(ref _requestedStartTime, value);
        }

        public TimeSpan Duration
        {
            get => _duration;
            set
            {
                SetField(ref _duration, value);
                NotifyPropertyChanged(nameof(ScheduledTc));
            }
        }

        public TVideoFormat VideoFormat => _engineViewModel.VideoFormat;

        #region Router

        public IRouter Router { get; }

        public int RouterPort { get; set; }

        public IList<IRouterPort> InputPorts { get; } = new List<IRouterPort>();

        private IRouterPort _selectedInputPort;

        public IRouterPort SelectedInputPort
        {
            get => _selectedInputPort;
            set
            {
                if (!SetField(ref _selectedInputPort, value))
                    return;
                RouterPort = value.PortId;
            }
        }

        #endregion

        public TimeSpan ScheduledDelay
        {
            get => _scheduledDelay;
            set => SetField(ref _scheduledDelay, value);
        }

        public bool IsStartEvent
        {
            get
            {
                var st = Model.StartType;
                return (st == TStartType.OnFixedTime || st == TStartType.Manual);
            }
        }

        public bool IsDurationEnabled => Model.EventType != TEventType.Rundown;

        public bool IsCGElementsEnabled => Model.IsMovieOrLive();

        public bool IsDisplayCGElements => Model.Engine.CGElementsController != null;

        public ICGElement[] Logos => Model.Engine.CGElementsController?.Logos.ToArray() ?? new ICGElement[0];

        public ICGElement[] Crawls => Model.Engine.CGElementsController?.Crawls.ToArray() ?? new ICGElement[0];

        public ICGElement[] Parentals => Model.Engine.CGElementsController?.Parentals.ToArray() ?? new ICGElement[0];
        
        public EventRightsEditViewmodel EventRightsEditViewmodel { get; }

        public TemplatedEditViewmodel TemplatedEditViewmodel { get; }
        public RecordingInfoViewModel RecordingInfoViewmodel { get; }

        public override string ToString() => $"{Infralution.Localization.Wpf.ResourceEnumConverter.ConvertToString(EventType)} - {EventName}";

        internal void Preview_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Media))
                InvalidateRequerySuggested();
        }

        private void OnMediaPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(IMedia.TcStart):
                case nameof(IMedia.Duration):
                    NotifyPropertyChanged(nameof(ScheduledTc));
                    NotifyPropertyChanged(nameof(Duration));
                    break;
            }
        }


        protected override void OnDispose()
        {
            Model.PropertyChanged -= ModelPropertyChanged;
            if (TemplatedEditViewmodel != null)
            {
                TemplatedEditViewmodel.ModifiedChanged -= TemplatedEditViewmodel_ModifiedChanged;
                TemplatedEditViewmodel?.Dispose();
            }
            if (EventRightsEditViewmodel != null)
            {
                EventRightsEditViewmodel.ModifiedChanged -= RightsModifiedChanged;
                EventRightsEditViewmodel.Dispose();
            }
            if (_media != null)
                _media.PropertyChanged -= OnMediaPropertyChanged;
        }

        #region Command methods

        private void TriggerStartType(object _)
        {
            if (StartType == TStartType.Manual)
                StartType = TStartType.OnFixedTime;
            else if (StartType == TStartType.OnFixedTime)
                StartType = TStartType.Manual;
        }

        private bool CanTriggerStartType(object _)
        {
            return (StartType == TStartType.Manual || StartType == TStartType.OnFixedTime)
                   && Model.HaveRight(EventRight.Modify);
        }

        private void ChangeMovie(object _)
        {
            if (Model.EventType == TEventType.Movie)
            {
                ChooseMedia(TMediaType.Movie, Model, Model.StartType);
            }
        }

        private void EditMovie(object _)
        {
            using (var evm = new MediaEditWindowViewmodel(Model.Media, Model.Engine.MediaManager) )
            {
                if (UiServices.ShowDialog<Views.MediaEditWindowView>(evm) == true)
                    evm.Editor.Save();
            }
        }

        private void CheckVolume(object _)
        {
            if (_media == null)
                return;
            IsVolumeChecking = true;
            var fileManager = Model.Engine.MediaManager.FileManager;
            var operation = (ILoudnessOperation)fileManager.CreateFileOperation(TFileOperationKind.Loudness);
            operation.Source = Model.Media;
            operation.MeasureStart = Model.ScheduledTc - _media.TcStart;
            operation.MeasureDuration =  Model.Duration;
            operation.AudioVolumeMeasured += AudioVolumeMeasured;
            operation.Finished += AudioVolumeFinished;
            fileManager.Queue(operation);
        }

        private void AudioVolumeFinished(object sender, EventArgs e)
        {
            IsVolumeChecking = false;
            ((ILoudnessOperation) sender).Finished -= AudioVolumeFinished;
            ((ILoudnessOperation) sender).AudioVolumeMeasured -= AudioVolumeFinished;
        }

        private void AudioVolumeMeasured(object _, AudioVolumeEventArgs e) => AudioVolume = e.AudioVolume;

        private bool CanChangeMovie(object _)
        {
            return Model.PlayState == TPlayState.Scheduled
                   && Model.EventType == TEventType.Movie
                   && Model.HaveRight(EventRight.Modify);
        }

        private bool CanEditMovie(object _)
        {
            return Model.Media != null
                   && Model.PlayState == TPlayState.Scheduled
                   && Model.EventType == TEventType.Movie
                   && Model.Engine.HaveRight(EngineRight.MediaEdit);
        }

        private bool CanCheckVolume(object o) => !_isVolumeChecking && CanChangeMovie(o);

        public bool CanSave => IsModified && IsValid && Model.HaveRight(EventRight.Modify);

        private void SetCGElements(IMedia media)
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


        private void ModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!(sender is IEvent s))
                return;
            OnUiThread(() =>
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
                        _eventName = s.EventName;
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
                        NotifyPropertyChanged(nameof(IsEnabled));
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
                    case nameof(IEvent.RouterPort):
                        RouterPort = s.RouterPort;
                        _selectedInputPort = InputPorts?.FirstOrDefault(p => p is IRouterPort routerPort && routerPort.PortId == s.RouterPort) ?? InputPorts?[0];
                        NotifyPropertyChanged(nameof(SelectedInputPort));
                        break;
                    case nameof(IEvent.CurrentUserRights):
                        InvalidateRequerySuggested();
                        break;
                    case nameof(IEvent.RecordingInfo):
                        RecordingInfoViewmodel?.UpdateInfo(s.RecordingInfo);
                        break;
                }
            });
        }

        private void TemplatedEditViewmodel_ModifiedChanged(object sender, EventArgs e)
        {
            if (sender is TemplatedEditViewmodel templatedEditViewmodel && templatedEditViewmodel.IsModified)
                IsModified = true;
        }


        private string ValidateEventName()
        {
            if (Model.FieldLengths.TryGetValue(nameof(IEvent.EventName), out var length) && EventName.Length > length)
                return resources._validate_TextTooLong;
            return null;
        }

        private string ValidateScheduledDelay()
        {
            if (Model.EventType != TEventType.StillImage && Model.EventType != TEventType.CommandScript)
                return null;
            var parent = Model.GetParent();
            if (parent != null && _duration + _scheduledDelay > parent.Duration)
                return resources._validate_ScheduledDelayInvalid;
            return null;
        }

        private string ValidateScheduledTime()
        {
            if (((_startType == TStartType.OnFixedTime && (_autoStartFlags & AutoStartFlags.Daily) == AutoStartFlags.None)
                || _startType == TStartType.Manual) && Model.PlayState == TPlayState.Scheduled && _scheduledTime < Model.Engine.CurrentTime)
                return resources._validate_StartTimePassed;
            return null;
        }

        private string ValidateScheduledTc()
        {
            var media = Media;
            if (Model.EventType != TEventType.Movie || media == null)
                return null;
            if (_scheduledTc > media.Duration + media.TcStart)
                return string.Format(resources._validate_StartTCAfterFile,
                    (media.Duration + media.TcStart).ToSmpteTimecodeString(Model.Engine.VideoFormat));
            if (_scheduledTc < media.TcStart)
                return string.Format(resources._validate_StartTCBeforeFile,
                    media.TcStart.ToSmpteTimecodeString(Model.Engine.VideoFormat));
            return null;
        }

        private string ValidateRecordingInfo()
        {
            if (RecordingInfoViewmodel != null && RecordingInfoViewmodel.IsRecordingScheduled && RecordingInfoViewmodel.SelectedRecorder != null && RecordingInfoViewmodel.SelectedRecorderChannel != null)
                return null;
            else if (RecordingInfoViewmodel != null && !RecordingInfoViewmodel.IsRecordingScheduled)
                return null;
            else if (RecordingInfoViewmodel == null)
                return null;

            return resources._validateRecordingInfo;
        }

        private string ValidateDuration()
        {
            var media = Media;
            if (Model.EventType == TEventType.Movie && media != null
                && _duration + _scheduledTc > media.Duration + media.TcStart)
                return resources._validate_DurationInvalid;
            if (Model.EventType != TEventType.StillImage && Model.EventType != TEventType.CommandScript)
                return null;
            var parent = Model.GetParent();
            if (parent != null && _duration + _scheduledDelay > parent.Duration)
                return resources._validate_ScheduledDelayInvalid;
            return null;
        }

        private string ValidateTransitionPauseTime()
        {
            if (_transitionPauseTime > _transitionTime)
                return resources._validate_TransitionPauseTimeInvalid;
            return null;
        }

        private string ValidateTransitionTime()
        {
            if (_transitionTime > _duration)
                return resources._validate_TransitionTimeInvalid;
            return null;
        }

        private void ChooseMedia(TMediaType mediaType, IEvent baseEvent, TStartType startType,
            VideoFormatDescription videoFormatDescription = null)
        {
            using (var vm = new MediaSearchViewmodel(
                _engineViewModel.Engine.HaveRight(EngineRight.Preview) ? _engineViewModel.Engine.Preview : null,
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
                    SetCGElements(media);
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

        private void RightsModifiedChanged(object sender, EventArgs e) => IsModified = true;

        public void SetFocusOnEventName() => IsEventNameFocused = true;

        /// <summary>
        /// class to represent a dummy router port "do not change the input"
        /// </summary>
        private class DummyRouterPort: IRouterPort
        {
            public int PortId => -1;
            public string PortName => string.Empty;
            public bool? IsSignalPresent => null;
#pragma warning disable CS0067
            public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore
        }
    }
}

