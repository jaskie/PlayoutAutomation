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
using resources = TAS.Client.Common.Properties.Resources;

namespace TAS.Client.ViewModels
{
    public class EventEditViewmodel : EditViewmodelBase<IEvent>, IDataErrorInfo
    {
        private readonly EngineViewmodel _engineViewModel;
        private IMedia _media;
        private bool _isVolumeChecking;
        private TEventType _eventType;
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
        private sbyte _layer;

        public static readonly Regex RegexMixerFill = new Regex(TAS.Common.EventExtensions.MixerFillCommand, RegexOptions.IgnoreCase);
        public static readonly Regex RegexMixerClip = new Regex(TAS.Common.EventExtensions.MixerClipCommand, RegexOptions.IgnoreCase);
        public static readonly Regex RegexMixerClear = new Regex(TAS.Common.EventExtensions.MixerClearCommand, RegexOptions.IgnoreCase);
        public static readonly Regex RegexPlay = new Regex(TAS.Common.EventExtensions.PlayCommand, RegexOptions.IgnoreCase);
        public static readonly Regex RegexCg = new Regex(TAS.Common.EventExtensions.CgCommand, RegexOptions.IgnoreCase);

        public EventEditViewmodel(IEvent @event, EngineViewmodel engineViewModel): base(@event)
        {
            _engineViewModel = engineViewModel;
            Model.PropertyChanged += ModelPropertyChanged;
            if (@event.EventType == TEventType.Container)
            {
                EventRightsEditViewmodel = new EventRightsEditViewmodel(@event, engineViewModel.Engine.AuthenticationService);
                EventRightsEditViewmodel.ModifiedChanged += RightsModifiedChanged;
            }
            CommandSaveEdit = new UiCommand(o => Save(), _canSave);
            CommandUndoEdit = new UiCommand(o => UndoEdit(), o => IsModified);
            CommandChangeMovie = new UiCommand(_changeMovie, _canChangeMovie);
            CommandEditMovie = new UiCommand(_editMovie, _canEditMovie);
            CommandCheckVolume = new UiCommand(_checkVolume, _canCheckVolume);
            CommandTriggerStartType = new UiCommand
            (
                _triggerStartType,
                _canTriggerStartType
            );
            CommandMoveUp = new UiCommand
            (
                o => Model.MoveUp(),
                o => Model.CanMoveUp()
            );
            CommandMoveDown = new UiCommand
            (
                o => Model.MoveDown(),
                o => Model.CanMoveDown()
            );
            CommandDelete = new UiCommand
            (
                async o =>
                {
                    if (MessageBox.Show(resources._query_DeleteItem, resources._caption_Confirmation, MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                        return;
                    await EventClipboard.SaveUndo(new List<IEvent> {Model},
                        Model.StartType == TStartType.After ? Model.Prior : Model.Parent);
                    Model.Delete();
                },
                o => Model.HaveRight(EventRight.Delete) && Model.AllowDelete()
            );
            if (@event is ITemplated templated)
            {
                TemplatedEditViewmodel = new TemplatedEditViewmodel(templated, true, true, engineViewModel.VideoFormat);
                TemplatedEditViewmodel.ModifiedChanged += TemplatedEditViewmodel_ModifiedChanged;
            }
        }

        public void Save()
        {
            Update();
        }

        public void UndoEdit()
        {
            TemplatedEditViewmodel?.UndoEdit();
            EventRightsEditViewmodel?.UndoEdit();
            Load();
        }
        
        protected override void Update(object destObject = null)
        {
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

        public TEventType EventType
        {
            get => _eventType;
            set => SetField(ref _eventType, value);
        }

        public string EventName
        {
            get => _eventName;
            set => SetField(ref _eventName, value);
        }

        public bool IsEditEnabled => Model.PlayState == TPlayState.Scheduled && Model.HaveRight(EventRight.Modify);

        public bool IsAutoStartEvent => _startType == TStartType.OnFixedTime;

        public bool IsMovieOrLive => Model.EventType == TEventType.Movie || Model.EventType == TEventType.Live;

        public bool IsMovieOrLiveOrRundown
        {
            get
            {
                var et = Model.EventType;
                return et == TEventType.Movie || et == TEventType.Live || et == TEventType.Rundown;
            }
        }

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

        public bool IsTransitionPanelEnabled
        {
            get
            {
                var et = Model.EventType;
                return !_isHold && (et == TEventType.Live || et == TEventType.Movie);
            }
        }

        public bool IsTransitionPropertiesVisible => _transitionType != TTransitionType.Cut;

        public bool IsContainer => Model.EventType == TEventType.Container;

        public bool CanHold => Model.Prior != null;

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
                    ? Model.Parent
                    : (st == TStartType.After)
                        ? Model.Prior
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

        public bool IsDisplayBindToEnd => (_eventType == TEventType.CommandScript || _eventType == TEventType.StillImage)
                                          && (_startType == TStartType.WithParent || _startType == TStartType.WithParentFromEnd);

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
                if (!SetField(ref _scheduledTime, value))
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

        public TimeSpan ScheduledDelay
        {
            get => _scheduledDelay;
            set => SetField(ref _scheduledDelay, value);
        }

        public sbyte Layer
        {
            get => _layer;
            set => SetField(ref _layer, value);
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

        public bool IsCGElementsEnabled => Model.EventType == TEventType.Live || Model.EventType == TEventType.Movie;

        public bool IsDisplayCGElements => Model.Engine.CGElementsController != null;

        public ICGElement[] Logos => Model.Engine.CGElementsController?.Logos.ToArray() ?? new ICGElement[0];

        public ICGElement[] Crawls => Model.Engine.CGElementsController?.Crawls.ToArray() ?? new ICGElement[0];

        public ICGElement[] Parentals => Model.Engine.CGElementsController?.Parentals.ToArray() ?? new ICGElement[0];

        public EventRightsEditViewmodel EventRightsEditViewmodel { get; }

        public TemplatedEditViewmodel TemplatedEditViewmodel { get; }

        public override string ToString()
        {
            return $"{Infralution.Localization.Wpf.ResourceEnumConverter.ConvertToString(EventType)} - {EventName}";
        }

        internal void _previewPropertyChanged(object sender, PropertyChangedEventArgs e)
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

        private void _changeMovie(object o)
        {
            if (Model.EventType == TEventType.Movie)
            {
                _chooseMedia(TMediaType.Movie, Model, Model.StartType);
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
            var operation = (ILoudnessOperation)fileManager.CreateFileOperation(TFileOperationKind.Loudness);
            operation.Source = Model.Media;
            operation.MeasureStart = Model.ScheduledTc - _media.TcStart;
            operation.MeasureDuration =  Model.Duration;
            operation.AudioVolumeMeasured += _audioVolumeMeasured;
            operation.Finished += _audioVolumeFinished;
            fileManager.Queue(operation);
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
            return Model.PlayState == TPlayState.Scheduled
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
            return IsModified
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
                    case nameof(IEvent.Prior):
                    case nameof(IEvent.Parent):
                        NotifyPropertyChanged(nameof(BoundEventName));
                        break;
                    case nameof(IEvent.CurrentUserRights):
                        InvalidateRequerySuggested();
                        break;
                }
            });
        }

        private void TemplatedEditViewmodel_ModifiedChanged(object sender, EventArgs e)
        {
            if (sender is TemplatedEditViewmodel templatedEditViewmodel && templatedEditViewmodel.IsModified)
                IsModified = true;
        }


        private string _validateEventName()
        {
            if (Model.FieldLengths.TryGetValue(nameof(IEvent.EventName), out var length) && EventName.Length > length)
                return resources._validate_TextTooLong;
            return null;
        }

        private string _validateScheduledDelay()
        {
            if (Model.EventType != TEventType.StillImage && Model.EventType != TEventType.CommandScript)
                return null;
            var parent = Model.Parent;
            if (parent != null && _duration + _scheduledDelay > parent.Duration)
                return resources._validate_ScheduledDelayInvalid;
            return null;
        }

        private string _validateScheduledTime()
        {
            if (((_startType == TStartType.OnFixedTime && (_autoStartFlags & AutoStartFlags.Daily) == AutoStartFlags.None)
                || _startType == TStartType.Manual) && Model.PlayState == TPlayState.Scheduled && _scheduledTime < Model.Engine.CurrentTime)
                return resources._validate_StartTimePassed;
            return null;
        }

        private string _validateScheduledTc()
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

        private string _validateDuration()
        {
            var media = Media;
            if (Model.EventType == TEventType.Movie && media != null
                && _duration + _scheduledTc > media.Duration + media.TcStart)
                return resources._validate_DurationInvalid;
            if (Model.EventType != TEventType.StillImage && Model.EventType != TEventType.CommandScript)
                return null;
            var parent = Model.Parent;
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

