using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using TAS.Client.Common;
using TAS.Common;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using TAS.Client.Common.Plugin;
using TAS.Common.Interfaces;
using resources = TAS.Client.Common.Properties.Resources;

namespace TAS.Client.ViewModels
{
    public class EngineViewmodel : ViewModelBase
    {
        private EventPanelViewmodelBase _selectedEvent;
        private DateTime _currentTime;
        private TimeSpan _timeToAttention;
        private Views.EngineDebugView _debugWindow;
        private int _audioLevelPri = -100;
        private bool _trackPlayingEvent = true;
        private bool _isPropertiesPanelVisible = true;

        private MediaSearchViewmodel _mediaSearchViewModel;
        private readonly ObservableCollection<IEvent> _visibleEvents = new ObservableCollection<IEvent>();
        private readonly ObservableCollection<IEvent> _runningEvents = new ObservableCollection<IEvent>();
        private readonly ObservableCollection<EventPanelViewmodelBase> _multiSelectedEvents;


        public EngineViewmodel(IEngine engine, IPreview preview)
        {
            Debug.WriteLine($"Creating EngineViewmodel for {engine}");
            Engine = engine;
            VideoFormat = engine.VideoFormat;
            IsInterlacedFormat = engine.FormatDescription.Interlaced;

            RootEventViewModel = new EventPanelRootViewmodel(this);
            Engine.EngineTick += _engineTick;
            Engine.EngineOperation += _engineOperation;
            Engine.PropertyChanged += _enginePropertyChanged;
            Engine.VisibleEventAdded += _engine_VisibleEventAdded;
            Engine.VisibleEventRemoved += _engine_VisibleEventRemoved;
            Engine.RunningEventsOperation += OnEngineRunningEventsOperation;
            _composePlugins();


            if (preview != null && engine.HaveRight(EngineRight.Preview))
                PreviewViewmodel = new PreviewViewmodel(engine, preview) { IsSegmentsVisible = true };


            _createCommands();

            _multiSelectedEvents = new ObservableCollection<EventPanelViewmodelBase>();
            _multiSelectedEvents.CollectionChanged += _selectedEvents_CollectionChanged;
            EventClipboard.ClipboardChanged += _engineViewmodel_ClipboardChanged;
            if (engine.PlayoutChannelPRI != null)
                engine.PlayoutChannelPRI.PropertyChanged += OnServerChannelPropertyChanged;
            if (engine.PlayoutChannelSEC != null)
                engine.PlayoutChannelSEC.PropertyChanged += OnServerChannelPropertyChanged;
            if (engine.PlayoutChannelPRV != null)
                engine.PlayoutChannelPRV.PropertyChanged += OnServerChannelPropertyChanged;
            var _cGElementsController = engine.CGElementsController;
            if (_cGElementsController != null)
            {
                _cGElementsController.PropertyChanged += _cGElementsController_PropertyChanged;
                CGElementsControllerViewmodel = new EngineCGElementsControllerViewmodel(engine.CGElementsController);
            }
        }

        private void _engine_VisibleEventRemoved(object sender, EventEventArgs e)
        {
            Application.Current?.Dispatcher.BeginInvoke((Action)(() => _visibleEvents.Remove(e.Event)));
        }

        private void _engine_VisibleEventAdded(object sender, EventEventArgs e)
        {
            Application.Current?.Dispatcher.BeginInvoke((Action)(() => _visibleEvents.Add(e.Event)));
        }

        public ICommand CommandClearAll { get; private set; }
        public ICommand CommandClearMixer { get; private set; }
        public ICommand CommandClearLayer { get; private set; }
        public ICommand CommandRestart { get; private set; }
        public ICommand CommandStartSelected { get; private set; }
        public ICommand CommandForceNextSelected { get; private set; }
        public ICommand CommandStartLoaded { get; private set; }
        public ICommand CommandLoadSelected { get; private set; }
        public ICommand CommandScheduleSelected { get; private set; }
        public ICommand CommandRescheduleSelected { get; private set; }
        public ICommand CommandTrackingToggle { get; private set; }
        public ICommand CommandRestartRundown { get; private set; }
        public ICommand CommandNewRootRundown { get; private set; }
        public ICommand CommandNewContainer { get; private set; }
        public ICommand CommandDebugToggle { get; private set; }
        public ICommand CommandSearchMissingEvents { get; private set; }
        public ICommand CommandDeleteSelected { get; private set; }
        public ICommand CommandCopySelected { get; private set; }
        public ICommand CommandPasteSelected { get; private set; }
        public ICommand CommandCutSelected { get; private set; }
        public ICommand CommandExportMedia { get; private set; }
        public ICommand CommandUndelete { get; private set; }
        public ICommand CommandSaveRundown { get; private set; }
        public ICommand CommandLoadRundown { get; private set; }
        public ICommand CommandRestartLayer { get; private set; }
        public ICommand CommandSearchDo { get; private set; }
        public ICommand CommandSearchShowPanel { get; private set; }
        public ICommand CommandSearchHidePanel { get; private set; }
        public ICommand CommandSetTimeCorrection { get; private set; }


        #region Single selected commands
        public ICommand CommandEventHide { get; private set; }
        public ICommand CommandAddNextMovie { get; private set; }
        public ICommand CommandAddNextEmptyMovie { get; private set; }
        public ICommand CommandAddNextRundown { get; private set; }
        public ICommand CommandAddNextLive { get; private set; }
        public ICommand CommandAddSubMovie { get; private set; }
        public ICommand CommandAddSubRundown { get; private set; }
        public ICommand CommandAddSubLive { get; private set; }
        public ICommand CommandAddAnimation { get; private set; }
        public ICommand CommandToggleEnabled { get; private set; }
        public ICommand CommandToggleHold { get; private set; }
        public ICommand CommandToggleLayer { get; private set; }
        public ICommand CommandMoveUp { get; private set; }
        public ICommand CommandMoveDown { get; private set; }
        #endregion // Single selected commands
        #region Editor commands
        public ICommand CommandSaveEdit { get; private set; }
        public ICommand CommandUndoEdit { get; private set; }
        #endregion // Editor commands
        public ICommand CommandUserManager { get; private set; }
        public ICommand CommandEngineRights { get; private set; }
        public ICommand CommandTogglePropertiesPanel { get; private set; }


        #region PreviewCommands

        public ICommand CommandPreviewPlay => PreviewViewmodel?.CommandPlay;
        public ICommand CommandPreviewUnload => PreviewViewmodel?.CommandUnload;
        public ICommand CommandPreviewFastForward => PreviewViewmodel?.CommandFastForward;
        public ICommand CommandPreviewBackward => PreviewViewmodel?.CommandBackward;
        public ICommand CommandPreviewFastForwardOneFrame => PreviewViewmodel?.CommandFastForwardOneFrame;
        public ICommand CommandPreviewBackwardOneFrame => PreviewViewmodel?.CommandBackwardOneFrame;

        public ICommand CommandPreviewTrimSource => PreviewViewmodel?.CommandTrimSource;
        #endregion

        public bool IsDebugBuild
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }

        public IEngine Engine { get; }

        public EventPanelViewmodelBase RootEventViewModel { get; }

        public PreviewViewmodel PreviewViewmodel { get; }

        public bool IsSearchPanelVisible { get => _isSearchPanelVisible; set => SetField(ref _isSearchPanelVisible, value); }

        public bool IsSearchBoxFocused { get => _isSearchBoxFocused; set => SetField(ref _isSearchBoxFocused, value); }

        public EngineCGElementsControllerViewmodel CGElementsControllerViewmodel { get; }

        public bool IsPropertiesPanelVisible
        {
            get => _isPropertiesPanelVisible;
            set => SetField(ref _isPropertiesPanelVisible, value);
        }

        public bool TrackPlayingEvent
        {
            get => _trackPlayingEvent;
            set
            {
                if (!SetField(ref _trackPlayingEvent, value))
                    return;
                if (!value)
                    return;
                IEvent cp = Engine.Playing;
                if (cp != null)
                    SetOnTopView(cp);
            }
        }

        public bool IsInterlacedFormat { get; }


        protected override void OnDispose()
        {
            Engine.EngineTick -= _engineTick;
            Engine.EngineOperation -= _engineOperation;
            Engine.PropertyChanged -= _enginePropertyChanged;
            Engine.VisibleEventAdded -= _engine_VisibleEventAdded;
            Engine.VisibleEventRemoved -= _engine_VisibleEventRemoved;
            Engine.RunningEventsOperation -= OnEngineRunningEventsOperation;

            _multiSelectedEvents.CollectionChanged -= _selectedEvents_CollectionChanged;
            EventClipboard.ClipboardChanged -= _engineViewmodel_ClipboardChanged;
            if (Engine.PlayoutChannelPRI != null)
                Engine.PlayoutChannelPRI.PropertyChanged -= OnServerChannelPropertyChanged;
            if (Engine.PlayoutChannelSEC != null)
                Engine.PlayoutChannelSEC.PropertyChanged -= OnServerChannelPropertyChanged;
            if (Engine.PlayoutChannelPRV != null)
                Engine.PlayoutChannelPRV.PropertyChanged -= OnServerChannelPropertyChanged;
            _videoPreview?.Dispose();
        }


        #region Command methods

        private void _createCommands()
        {
            CommandClearAll = new UICommand() { ExecuteDelegate = o => Engine.Clear(), CanExecuteDelegate = _canClear };
            CommandClearLayer = new UICommand() { ExecuteDelegate = layer => Engine.Clear((VideoLayer)int.Parse((string)layer)), CanExecuteDelegate = _canClear };
            CommandClearMixer = new UICommand() { ExecuteDelegate = o => Engine.ClearMixer(), CanExecuteDelegate = _canClear };
            CommandRestart = new UICommand() { ExecuteDelegate = ev => Engine.Restart(), CanExecuteDelegate = _canClear };
            CommandStartSelected = new UICommand() { ExecuteDelegate = _startSelected, CanExecuteDelegate = _canStartSelected };
            CommandLoadSelected = new UICommand() { ExecuteDelegate = _loadSelected, CanExecuteDelegate = _canLoadSelected };
            CommandScheduleSelected = new UICommand() { ExecuteDelegate = o => Engine.Schedule(_selectedEvent.Event), CanExecuteDelegate = _canScheduleSelected };
            CommandRescheduleSelected = new UICommand() { ExecuteDelegate = o => Engine.ReSchedule(_selectedEvent.Event), CanExecuteDelegate = _canRescheduleSelected };
            CommandForceNextSelected = new UICommand() { ExecuteDelegate = _forceNext, CanExecuteDelegate = _canForceNextSelected };
            CommandTrackingToggle = new UICommand() { ExecuteDelegate = o => TrackPlayingEvent = !TrackPlayingEvent };
            CommandDebugToggle = new UICommand() { ExecuteDelegate = _debugShow };
            CommandRestartRundown = new UICommand() { ExecuteDelegate = _restartRundown, CanExecuteDelegate = _canClear };
            CommandRestartLayer = new UICommand { ExecuteDelegate = _restartLayer, CanExecuteDelegate = o => IsPlayingMovie && Engine.HaveRight(EngineRight.Play) };
            CommandNewRootRundown = new UICommand() { ExecuteDelegate = _addNewRootRundown };
            CommandNewContainer = new UICommand() { ExecuteDelegate = _newContainer };
            CommandSearchMissingEvents = new UICommand() { ExecuteDelegate = _searchMissingEvents };
            CommandStartLoaded = new UICommand() { ExecuteDelegate = o => Engine.StartLoaded(), CanExecuteDelegate = o => Engine.EngineState == TEngineState.Hold };
            CommandDeleteSelected = new UICommand() { ExecuteDelegate = _deleteSelected, CanExecuteDelegate = _canDeleteSelected };
            CommandCopySelected = new UICommand() { ExecuteDelegate = _copySelected, CanExecuteDelegate = o => _multiSelectedEvents.Count > 0 };
            CommandCutSelected = new UICommand() { ExecuteDelegate = _cutSelected, CanExecuteDelegate = _canDeleteSelected };
            CommandPasteSelected = new UICommand() { ExecuteDelegate = _pasteSelected, CanExecuteDelegate = o => EventClipboard.CanPaste(_selectedEvent, (EventClipboard.PasteLocation)Enum.Parse(typeof(EventClipboard.PasteLocation), o.ToString(), true)) };
            CommandExportMedia = new UICommand() { ExecuteDelegate = _exportMedia, CanExecuteDelegate = _canExportMedia };
            CommandUndelete = new UICommand() { ExecuteDelegate = _undelete, CanExecuteDelegate = _canUndelete };

            CommandEventHide = new UICommand { ExecuteDelegate = _eventHide };
            CommandMoveUp = new UICommand { ExecuteDelegate = _moveUp };
            CommandMoveDown = new UICommand { ExecuteDelegate = _moveDown };
            CommandAddNextMovie = new UICommand { ExecuteDelegate = _addNextMovie, CanExecuteDelegate = _canAddNextMovie };
            CommandAddNextEmptyMovie = new UICommand { ExecuteDelegate = _addNextEmptyMovie, CanExecuteDelegate = _canAddNextEmptyMovie };
            CommandAddNextRundown = new UICommand { ExecuteDelegate = _addNextRundown, CanExecuteDelegate = _canAddNextRundown };
            CommandAddNextLive = new UICommand { ExecuteDelegate = _addNextLive, CanExecuteDelegate = _canAddNextLive };
            CommandAddSubMovie = new UICommand { ExecuteDelegate = _addSubMovie, CanExecuteDelegate = _canAddSubMovie };
            CommandAddSubRundown = new UICommand { ExecuteDelegate = _addSubRundown, CanExecuteDelegate = _canAddSubRundown };
            CommandAddSubLive = new UICommand { ExecuteDelegate = _addSubLive, CanExecuteDelegate = _canAddSubLive };
            CommandAddAnimation = new UICommand { ExecuteDelegate = _addAnimation, CanExecuteDelegate = _canAddAnimation };
            CommandToggleLayer = new UICommand { ExecuteDelegate = _toggleLayer };
            CommandToggleEnabled = new UICommand { ExecuteDelegate = _toggleEnabled };
            CommandToggleHold = new UICommand { ExecuteDelegate = _toggleHold };
            CommandTogglePropertiesPanel = new UICommand { ExecuteDelegate = o => IsPropertiesPanelVisible = !IsPropertiesPanelVisible };

            CommandSearchDo = new UICommand { ExecuteDelegate = _search, CanExecuteDelegate = _canSearch };
            CommandSearchShowPanel = new UICommand { ExecuteDelegate = _showSearchPanel };
            CommandSearchHidePanel = new UICommand { ExecuteDelegate = _hideSearchPanel };
            CommandSetTimeCorrection = new UICommand { ExecuteDelegate = _setTimeCorrection };

            CommandSaveEdit = new UICommand { ExecuteDelegate = _saveEdit };
            CommandUndoEdit = new UICommand { ExecuteDelegate = _undoEdit };

            CommandSaveRundown = new UICommand { ExecuteDelegate = _saveRundown, CanExecuteDelegate = o => SelectedEvent != null && SelectedEvent.Event.EventType == TEventType.Rundown };
            CommandLoadRundown = new UICommand { ExecuteDelegate = _loadRundown, CanExecuteDelegate = o => o.Equals("Under") ? _canAddSubRundown(o) : _canAddNextRundown(o) };
            CommandUserManager = new UICommand { ExecuteDelegate = _userManager, CanExecuteDelegate = _canUserManager };

            CommandEngineRights = new UICommand { ExecuteDelegate = _engineRights, CanExecuteDelegate = _canEngineRights };
        }

        private void _undoEdit(object obj)
        {
            SelectedEventEditViewmodel?.UndoEdit();
        }

        private void _saveEdit(object obj)
        {
            if (SelectedEventEditViewmodel?.IsValid == true)
                SelectedEventEditViewmodel.Save();
        }

        private void _moveUp(object obj)
        {
            var e = SelectedEvent?.Event;
            if (e == null || !e.CanMoveUp())
                return;
            SelectedEvent?.Event.MoveUp();
        }

        private void _moveDown(object obj)
        {
            var e = SelectedEvent?.Event;
            if (e == null || !e.CanMoveDown())
                return;
            SelectedEvent?.Event.MoveDown();
        }

        private void _setTimeCorrection(object obj)
        {
            if (!(obj is string strValue && int.TryParse(strValue, out var intValue)))
                return;
            Engine.TimeCorrection += intValue;
        }

        private bool _canDeleteSelected(object obj) => _multiSelectedEvents.Count > 0 && _multiSelectedEvents.All(e => e.Event.AllowDelete());

        private void _engineRights(object obj)
        {
            using (var vm = new EngineRightsEditViewmodel(Engine, Engine.AuthenticationService))
                UiServices.ShowDialog<Views.EngineRightsEditView>(vm);
        }

        private bool _canEngineRights(object obj) => CurrentUser.IsAdmin;

        private void _userManager(object obj)
        {
            var vm = new UserManagerViewmodel(Engine.AuthenticationService);
            UiServices.ShowWindow<Views.UserManagerView>(vm).Closed += (s, e) =>
                vm.Dispose();
        }

        private bool _canUserManager(object obj) => CurrentUser.IsAdmin;

        private bool _canUndelete(object obj) => EventClipboard.CanUndo();

        private void _undelete(object obj)
        {
            if (MessageBox.Show(string.Format(resources._query_Undelete), resources._caption_Confirmation, MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                EventClipboard.Undo();
        }

        private bool _canClear(object obj) => Engine.HaveRight(EngineRight.Play);

        private void _loadRundown(object obj)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog()
            {
                DefaultExt = FileUtils.RundownFileExtension,
                Filter = string.Format("{0}|*{1}|{2}|*.*", resources._rundowns, FileUtils.RundownFileExtension, resources._allFiles)
            };
            if (dlg.ShowDialog() == true)
            {
                UiServices.SetBusyState();
                using (var reader = File.OpenText(dlg.FileName))
                using (var jreader = new Newtonsoft.Json.JsonTextReader(reader))
                {
                    var proxy = (new Newtonsoft.Json.JsonSerializer { DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Populate })
                        .Deserialize<EventProxy>(jreader);
                    if (proxy != null)
                    {
                        var mediaFiles = (Engine.MediaManager.MediaDirectoryPRI ?? Engine.MediaManager.MediaDirectorySEC)?.GetFiles();
                        var animationFiles = (Engine.MediaManager.AnimationDirectoryPRI ?? Engine.MediaManager.AnimationDirectorySEC)?.GetFiles();
                        var newEvent = obj.Equals("Under") ? proxy.InsertUnder(SelectedEvent.Event, false, mediaFiles, animationFiles) : proxy.InsertAfter(SelectedEvent.Event, mediaFiles, animationFiles);
                        LastAddedEvent = newEvent;
                    }

                }
            }
        }

        private void _saveRundown(object obj)
        {
            EventProxy proxy = EventProxy.FromEvent(SelectedEvent.Event);
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog()
            {
                FileName = proxy.EventName,
                DefaultExt = FileUtils.RundownFileExtension,
                Filter = $"{resources._rundowns}|*{FileUtils.RundownFileExtension}|{resources._allFiles}|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                using (var writer = File.CreateText(dlg.FileName))
                    new Newtonsoft.Json.JsonSerializer() { Formatting = Newtonsoft.Json.Formatting.Indented, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore, DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore, TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto }
                    .Serialize(writer, proxy);
            }
        }

        private bool _canAddSubLive(object obj) => SelectedEvent is EventPanelRundownViewmodel ep && ep.CommandAddSubLive.CanExecute(obj);

        private bool _canAddSubRundown(object obj)
        {
            if (SelectedEvent is EventPanelRundownViewmodel ep)
                return ep.CommandAddSubRundown.CanExecute(obj);
            return SelectedEvent is EventPanelContainerViewmodel ec && ec.CommandAddSubRundown.CanExecute(obj);
        }

        private bool _canAddSubMovie(object obj) => SelectedEvent is EventPanelRundownViewmodel ep && ep.CommandAddSubMovie.CanExecute(obj);

        private bool _canAddNextLive(object obj) => SelectedEvent is EventPanelRundownElementViewmodelBase ep && ep.CommandAddNextLive.CanExecute(obj);

        private bool _canAddNextRundown(object obj) => SelectedEvent is EventPanelRundownElementViewmodelBase ep && ep.CommandAddNextRundown.CanExecute(obj);

        private bool _canAddNextEmptyMovie(object obj) => SelectedEvent is EventPanelRundownElementViewmodelBase ep && ep.CommandAddNextEmptyMovie.CanExecute(obj);

        private bool _canAddNextMovie(object obj) => SelectedEvent is EventPanelRundownElementViewmodelBase ep && ep.CommandAddNextMovie.CanExecute(obj);

        private void _toggleHold(object obj) => (SelectedEvent as EventPanelRundownElementViewmodelBase)?.CommandToggleHold.Execute(obj);

        private void _toggleEnabled(object obj) => (SelectedEvent as EventPanelRundownElementViewmodelBase)?.CommandToggleEnabled.Execute(obj);

        private void _forceNext(object obj) => Engine.ForceNext(IsForcedNext ? null : _selectedEvent.Event);

        private bool _canForceNextSelected(object obj) => Engine.EngineState == TEngineState.Running && (_canLoadSelected(obj) || IsForcedNext);

        private void _toggleLayer(object obj) => (SelectedEvent as EventPanelRundownElementViewmodelBase)?.CommandToggleLayer.Execute(obj);

        private void _addSubLive(object obj) => (SelectedEvent as EventPanelRundownViewmodel)?.CommandAddSubLive.Execute(obj);

        private void _addAnimation(object obj) => (SelectedEvent as EventPanelRundownElementViewmodelBase)?.CommandAddAnimation.Execute(obj);

        private bool _canAddAnimation(object obj) => SelectedEvent is EventPanelRundownElementViewmodelBase ep && ep.CommandAddAnimation.CanExecute(obj);

        private void _addSubRundown(object obj)
        {
            (SelectedEvent as EventPanelRundownViewmodel)?.CommandAddSubRundown.Execute(obj);
            (SelectedEvent as EventPanelContainerViewmodel)?.CommandAddSubRundown.Execute(obj);
        }

        private void _addSubMovie(object obj) => (SelectedEvent as EventPanelRundownViewmodel)?.CommandAddSubMovie.Execute(obj);

        private void _addNextLive(object obj) => (SelectedEvent as EventPanelRundownElementViewmodelBase)?.CommandAddNextLive.Execute(obj);

        private void _addNextRundown(object obj) => (SelectedEvent as EventPanelRundownElementViewmodelBase)?.CommandAddNextRundown.Execute(obj);

        private void _addNextEmptyMovie(object obj) => (SelectedEvent as EventPanelRundownElementViewmodelBase)?.CommandAddNextEmptyMovie.Execute(obj);

        private void _addNextMovie(object obj) => (SelectedEvent as EventPanelRundownElementViewmodelBase)?.CommandAddNextMovie.Execute(obj);

        private void _eventHide(object obj) => (SelectedEvent as EventPanelContainerViewmodel)?.CommandHide.Execute(obj);

        private void _pasteSelected(object obj) => LastAddedEvent = EventClipboard.Paste(_selectedEvent, (EventClipboard.PasteLocation)Enum.Parse(typeof(EventClipboard.PasteLocation), (string)obj, true));

        private void _copySelected(object obj) => EventClipboard.Copy(_multiSelectedEvents);

        private void _cutSelected(object obj) => EventClipboard.Cut(_multiSelectedEvents);

        private bool _canExportMedia(object obj)
        {
            if (!Engine.HaveRight(EngineRight.MediaExport))
                return false;

            bool exportAll = obj != null;
            return _multiSelectedEvents.Any(e => e is EventPanelMovieViewmodel m
                                                 && (m.IsEnabled || exportAll)
                                                 && m.Media?.FileExists() == true)
                   && Engine.MediaManager.IngestDirectories.Any(d => d.IsExport);
        }

        private void _exportMedia(object obj)
        {
            bool exportAll = obj != null;
            var selections = _multiSelectedEvents.Where(e => e is EventPanelMovieViewmodel m
                                                             && (m.IsEnabled || exportAll)
                                                             && m.Media?.FileExists() == true)
                .Select(e => new MediaExportDescription(
                    e.Event.Media,
                    e.Event.SubEvents.Where(sev => sev.EventType == TEventType.StillImage && sev.Media != null)
                        .Select(sev => sev.Media).ToList(),
                    e.Event.ScheduledTc,
                    e.Event.Duration,
                    e.Event.GetAudioVolume()));
            using (var vm = new ExportViewmodel(Engine, selections))
            {
                UiServices.ShowDialog<Views.ExportView>(vm);
            }
        }

        private void _startSelected(object obj)
        {
            var eventToStart = SelectedEvent.Event;
            if (Engine.EngineState != TEngineState.Running
                || Engine.Playing?.EventType == TEventType.Live
                || (string)obj == "Force"
                || MessageBox.Show(string.Format(resources._query_PlayWhileRunning), resources._caption_Confirmation, MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK
                )
                Engine.Start(eventToStart);
        }

        private bool _canStartSelected(object o)
        {
            IEvent ev = _selectedEvent?.Event;
            return ev != null
                   && ev.IsEnabled
                   && (ev.PlayState == TPlayState.Scheduled || ev.PlayState == TPlayState.Paused || ev.PlayState == TPlayState.Aborted)
                   && (ev.EventType == TEventType.Rundown || ev.EventType == TEventType.Live || ev.EventType == TEventType.Movie)
                   && Engine.HaveRight(EngineRight.Play);
        }

        private void _loadSelected(object obj)
        {
            var eventToLoad = SelectedEvent.Event;
            if (Engine.EngineState != TEngineState.Running
                || Engine.Playing?.EventType == TEventType.Live
                || MessageBox.Show(string.Format(resources._query_LoadWhileRunning), resources._caption_Confirmation, MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK
                )
                Engine.Load(eventToLoad);
        }

        private bool _canLoadSelected(object o)
        {
            IEvent ev = _selectedEvent?.Event;
            return ev != null
                && ev.IsEnabled
                && (ev.PlayState == TPlayState.Scheduled || ev.PlayState == TPlayState.Aborted)
                && (ev.EventType == TEventType.Rundown || ev.EventType == TEventType.Live || ev.EventType == TEventType.Movie)
                && Engine.HaveRight(EngineRight.Play);
        }

        private bool _canScheduleSelected(object o)
        {
            IEvent ev = _selectedEvent?.Event;
            return ev != null
                   && (ev.PlayState == TPlayState.Scheduled || ev.PlayState == TPlayState.Paused)
                   && ev.ScheduledTime >= _currentTime
                   && Engine.HaveRight(EngineRight.Play);
        }

        private bool _canRescheduleSelected(object o)
        {
            IEvent ev = _selectedEvent?.Event;
            return ev != null
                   && (ev.PlayState == TPlayState.Aborted || ev.PlayState == TPlayState.Played)
                   && Engine.HaveRight(EngineRight.Play);
        }

        private void _restartRundown(object o)
        {
            IEvent ev = _selectedEvent?.Event;
            if (ev != null)
                Engine.RestartRundown(ev);
        }

        private void _restartLayer(object obj)
        {
            Engine.Restart();
        }

        private void _addNewRootRundown(object o)
        {
            IEvent newEvent = Engine.CreateNewEvent(
                eventType: TEventType.Rundown,
                eventName: resources._title_NewRundown,
                startType: TStartType.Manual,
                scheduledTime: EventExtensions.DefaultScheduledTime);
            Engine.AddRootEvent(newEvent);
            newEvent.Save();
            LastAddedEvent = newEvent;
        }

        private void _newContainer(object o)
        {
            IEvent newEvent = Engine.CreateNewEvent(
                eventType: TEventType.Container,
                eventName: resources._title_NewContainer);
            Engine.AddRootEvent(newEvent);
            newEvent.Save();
            LastAddedEvent = newEvent;
        }

        private void _deleteSelected(object ob)
        {
            var evmList = _multiSelectedEvents.ToList();
            var containerList = evmList.Where(evm => evm is EventPanelContainerViewmodel).ToList();
            if (evmList.Count > 0
                && MessageBox.Show(string.Format(resources._query_DeleteSelected, evmList.Count, evmList.AsString(Environment.NewLine)), resources._caption_Confirmation, MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK
                && (containerList.Count == 0
                    || MessageBox.Show(string.Format(resources._query_DeleteSelectedContainers, containerList.Count, containerList.AsString(Environment.NewLine)), resources._caption_Confirmation, MessageBoxButton.OKCancel) == MessageBoxResult.OK))
            {
                var firstEvent = evmList.First().Event;
                EventClipboard.SaveUndo(evmList.Select(evm => evm.Event).ToList(), firstEvent.StartType == TStartType.After ? firstEvent.Prior : firstEvent.Parent);
                Task.Run(
                    () =>
                    {
                        try
                        {
                            foreach (var evm in evmList)
                            {
                                if (evm.Event != null
                                    && (evm.Event.PlayState == TPlayState.Scheduled || evm.Event.PlayState == TPlayState.Played || evm.Event.PlayState == TPlayState.Aborted))
                                    evm.Event.Delete();
                            }
                        }
                        catch (Exception e)
                        {
                            Application.Current?.Dispatcher.BeginInvoke((Action)delegate
                            {
                                MessageBox.Show(string.Format(resources._message_CommandFailed, e.Message), resources._caption_Error, MessageBoxButton.OK, MessageBoxImage.Hand);
                            });
                        }
                    }
                );
                _multiSelectedEvents.Clear();
            }
        }

        private void _debugShow(object o)
        {
            if (_debugWindow == null)
            {
                _debugWindow = new Views.EngineDebugView();
                _debugWindow.DataContext = this;
                _debugWindow.Closed += (w, e) =>
                {
                    var window = w as Views.EngineDebugView;
                    if (window != null)
                        window.DataContext = null;
                    _debugWindow = null;
                };
            }
            _debugWindow.Show();
        }

        #endregion // Commands

        #region MediaSearch
        public void AddMediaEvent(IEvent baseEvent, TStartType startType, TMediaType mediaType, VideoLayer layer, bool closeAfterAdd)
        {
            if (baseEvent == null)
                return;
            if (_mediaSearchViewModel == null)
            {
                var mediaSearchViewModel = new MediaSearchViewmodel(
                    Engine.HaveRight(EngineRight.Preview) ? Engine : null,
                    Engine, mediaType, layer, closeAfterAdd, baseEvent.Media?.FormatDescription())
                {
                    BaseEvent = baseEvent,
                    NewEventStartType = startType
                };
                mediaSearchViewModel.MediaChoosen += _mediaSearchViewModelMediaChoosen;
                if (closeAfterAdd)
                {
                    UiServices.ShowDialog<Views.MediaSearchView>(mediaSearchViewModel);
                    mediaSearchViewModel.MediaChoosen -= _mediaSearchViewModelMediaChoosen;
                    mediaSearchViewModel.Dispose();
                }
                else
                {
                    mediaSearchViewModel.Window = UiServices.ShowWindow<Views.MediaSearchView>(mediaSearchViewModel);
                    mediaSearchViewModel.Window.Closed += (sender, args) =>
                    {
                        mediaSearchViewModel.MediaChoosen -= _mediaSearchViewModelMediaChoosen;
                        mediaSearchViewModel.Dispose();
                        _mediaSearchViewModel = null;
                    };
                    _mediaSearchViewModel = mediaSearchViewModel;
                }
            }
            else
            {
                _mediaSearchViewModel.BaseEvent = baseEvent;
                _mediaSearchViewModel.Window.WindowState = WindowState.Normal;
            }
        }

        public void AddCommandScriptEvent(IEvent baseEvent)
        {
            var newEvent = Engine.CreateNewEvent(eventType: TEventType.CommandScript, duration: baseEvent.Duration, eventName: resources._title_NewCommandScript);
            baseEvent.InsertUnder(newEvent, false);
            LastAddedEvent = newEvent;
        }

        public void AddSimpleEvent(IEvent baseEvent, TEventType eventType, bool insertUnder)
        {
            IEvent newEvent = null;
            switch (eventType)
            {
                case TEventType.Live:
                    newEvent = Engine.CreateNewEvent(
                        eventType: TEventType.Live,
                        eventName: resources._title_NewLive,
                        videoLayer: VideoLayer.Program,
                        duration: new TimeSpan(0, 10, 0));
                    break;
                case TEventType.Movie:
                    newEvent = Engine.CreateNewEvent(
                        eventType: TEventType.Movie,
                        eventName: resources._title_EmptyMovie,
                        videoLayer: VideoLayer.Program);
                    break;
                case TEventType.Rundown:
                    newEvent = Engine.CreateNewEvent(
                        scheduledTime: EventExtensions.DefaultScheduledTime,
                        eventType: TEventType.Rundown,
                        eventName: resources._title_NewRundown);
                    break;
            }
            if (newEvent != null)
            {
                if (insertUnder)
                {
                    if (baseEvent.EventType == TEventType.Container)
                        newEvent.ScheduledTime = _currentTime;
                    baseEvent.InsertUnder(newEvent, false);
                }
                else
                    baseEvent.InsertAfter(newEvent);
                LastAddedEvent = newEvent;
            }
        }

        /// <summary>
        /// Used to determine if it should be selected when it's viewmodel is created
        /// </summary>
        public IEvent LastAddedEvent { get; private set; }
        #endregion // MediaSearch

        #region Search panel

        private bool _isSearchPanelVisible;
        private void _showSearchPanel(object obj)
        {
            IsSearchNotFound = false;
            IsSearchPanelVisible = true;
            IsSearchBoxFocused = true;
        }

        private void _hideSearchPanel(object obj)
        {
            IsSearchPanelVisible = false;
            SelectedEvent?.Focus();
        }

        private bool _isSearchBoxFocused;

        private bool _isSearchNotFound;
        public bool IsSearchNotFound { get { return _isSearchNotFound; } set { SetField(ref _isSearchNotFound, value); } }

        private string _searchText;
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (SetField(ref _searchText, value))
                    IsSearchNotFound = false;
            }
        }

        public bool _canSearch(object o)
        {
            return !string.IsNullOrWhiteSpace(_searchText);
        }

        private void _search(object o)
        {
            IEvent current = _selectedEvent?.Event;
            if (current != null && !string.IsNullOrWhiteSpace(SearchText))
            {
                string loweredSearchtext = SearchText.ToLower();
                Func<IEvent, bool> searchFunc = e => e.EventName.ToLower().Contains(loweredSearchtext);
                var visualRootTrack = current.GetVisualRootTrack();
                IEvent found = current.FindInside(searchFunc);
                if (found == null)
                    foreach (var et in visualRootTrack)
                    {
                        current = et.Next;
                        if (current != null)
                            found = current.FindNext(searchFunc);
                        if (found != null)
                            break;
                    }
                if (found != null)
                {
                    var rt = found.GetVisualRootTrack().Reverse();
                    var cl = RootEventViewModel;
                    foreach (var ev in rt)
                    {
                        cl = cl.Find(ev);
                        if (cl.Event == found)
                        {
                            SelectedEvent = cl;
                            cl.IsSelected = true;
                            cl.BringIntoView();
                        }
                        else
                            cl.IsExpanded = true;
                    }
                }
                else
                    IsSearchNotFound = true;
            }
        }
        #endregion


        public EventPanelViewmodelBase SelectedEvent
        {
            get => _selectedEvent;
            set
            {
                if (value == _selectedEvent)
                    return;
                if (SelectedEventEditViewmodel?.IsModified == true)
                {
                    if (SelectedEventEditViewmodel.IsValid)
                        switch (MessageBox.Show(string.Format(resources._query_SaveChangedData, SelectedEventEditViewmodel.EventName), resources._caption_Confirmation, MessageBoxButton.YesNoCancel))
                        {
                            case MessageBoxResult.Cancel:
                                NotifyPropertyChanged(nameof(SelectedEvent));
                                return;
                            case MessageBoxResult.Yes:
                                SelectedEventEditViewmodel.Save();
                                break;
                        }
                    else 
                    if (MessageBox.Show(string.Format(resources._query_AbadonChanges, SelectedEventEditViewmodel.EventName), resources._caption_Confirmation, MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                    {
                        NotifyPropertyChanged(nameof(SelectedEvent));
                        return;
                    }
                }
                SelectedEventEditViewmodel?.Dispose();
                var oldSelectedEvent = _selectedEvent?.Event;
                if (oldSelectedEvent != null)
                    oldSelectedEvent.PropertyChanged -= _onSelectedEventPropertyChanged;
                _selectedEvent = value;
                var newSelected = value?.Event;
                if (newSelected != null)
                {
                    newSelected.PropertyChanged += _onSelectedEventPropertyChanged;
                    SelectedEventEditViewmodel = new EventEditViewmodel(newSelected, this);
                }
                if (PreviewViewmodel != null)
                    PreviewViewmodel.SelectedEvent = newSelected;
                if (value is EventPanelRundownElementViewmodelBase re && _mediaSearchViewModel != null)
                {
                    _mediaSearchViewModel.BaseEvent = re.Event;
                    _mediaSearchViewModel.NewEventStartType = TStartType.After;
                }
                InvalidateRequerySuggested();
                _updatePluginCanExecute();
                IsSearchNotFound = false;
            }
        }

        public EventEditViewmodel SelectedEventEditViewmodel { get => _selectedEventEditViewmodel; set => SetField(ref _selectedEventEditViewmodel, value); }

        public bool Pst2Prv
        {
            get => Engine.Pst2Prv;
            set => Engine.Pst2Prv = value;
        }

        public DateTime CurrentTime
        {
            get => _currentTime;
            private set => SetField(ref _currentTime, value);
        }

        public TVideoFormat VideoFormat { get; }

        public TimeSpan TimeToAttention
        {
            get => _timeToAttention;
            set => SetField(ref _timeToAttention, value);
        }

        public bool FieldOrderInverted
        {
            get => Engine.FieldOrderInverted;
            set => Engine.FieldOrderInverted = value;
        }

        public string EngineName => Engine.EngineName;

        public bool IsPlayingMovie
        {
            get
            {
                var aEvent = Engine.Playing;
                return aEvent != null && aEvent.Layer == VideoLayer.Program && aEvent.EventType == TEventType.Movie;
            }
        }

        public double ProgramAudioVolume //decibels
        {
            get => 20 * Math.Log10(Engine.ProgramAudioVolume);
            set
            {
                var volume = Math.Pow(10, value / 20);
                if (Math.Abs(value - volume) > double.Epsilon)
                    Engine.ProgramAudioVolume = volume;
            }
        }

        public bool IsAnimationDirAvailable => Engine.MediaManager.AnimationDirectoryPRI != null || Engine.MediaManager.AnimationDirectorySEC != null;

        public bool NoAlarms => (ServerConnectedPRI || !ServerPRIExists)
                                && (ServerConnectedSEC || !ServerSECExists)
                                && (ServerConnectedPRV || !ServerPRVExists)
                                && DatabaseOK;

        public bool ServerPRIExists => Engine?.PlayoutChannelPRI != null;

        public bool ServerConnectedPRI => Engine?.PlayoutChannelPRI?.IsServerConnected == true;

        public bool ServerSECExists => Engine?.PlayoutChannelSEC != null;

        public bool ServerConnectedSEC => Engine?.PlayoutChannelSEC?.IsServerConnected == true;

        public bool ServerPRVExists => Engine?.PlayoutChannelPRV != null;

        public bool ServerConnectedPRV => Engine?.PlayoutChannelPRV?.IsServerConnected == true;

        public bool DatabaseOK
        {
            get
            {
                var state = Engine.DatabaseConnectionState;
                return (state & ConnectionStateRedundant.Open) > 0
                    && (state & (ConnectionStateRedundant.BrokenPrimary | ConnectionStateRedundant.BrokenSecondary | ConnectionStateRedundant.Desynchronized)) == 0;
            }
        }

        public string PlayingEventName
        {
            get
            {
                var e = Engine.Playing;
                return e == null ? string.Empty : e.EventName;
            }
        }

        public string NextToPlay
        {
            get
            {
                var e = Engine.GetNextToPlay();
                return e == null ? string.Empty : e.EventName;
            }
        }

        public IEvent NextWithRequestedStartTime => Engine.GetNextWithRequestedStartTime();

        public int SelectedCount => _multiSelectedEvents.Count;

        public TimeSpan SelectedTime
        {
            get { return TimeSpan.FromTicks(_multiSelectedEvents.Sum(e => e.Event.Duration.Ticks)); }
        }

        public bool IsForcedNext => Engine.ForcedNext != null;

        public int AudioLevelPRI
        {
            get => _audioLevelPri;
            private set => SetField(ref _audioLevelPri, value);
        }

        #region Plugin
        CompositionContainer _uiContainer;

#pragma warning disable CS0649
        [ImportMany]
        IUiPlugin[] _plugins = null;
        [Import(AllowDefault = true)]
        IVideoPreview _videoPreview;
        private EventEditViewmodel _selectedEventEditViewmodel;

#pragma warning restore

        public IList<IUiPlugin> Plugins => _plugins;


        public IVideoPreview VideoPreview => _videoPreview;

        public bool IsAnyPluginActive => _plugins != null && _plugins.Length > 0;

        private void _composePlugins()
        {
            try
            {
                var pluginPath = Path.Combine(Directory.GetCurrentDirectory(), "Plugins");
                if (!Directory.Exists(pluginPath))
                    return;
                DirectoryCatalog catalog = new DirectoryCatalog(pluginPath);
                _uiContainer = new CompositionContainer(catalog);
                _uiContainer.ComposeExportedValue<Func<PluginExecuteContext>>(_getPluginContext);
                _uiContainer.SatisfyImportsOnce(this);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        private void _updatePluginCanExecute()
        {
            if (_plugins != null)
                foreach (var p in _plugins)
                    p.NotifyExecuteChanged();
        }

        [Export]
        private PluginExecuteContext _getPluginContext()
        {
            return new PluginExecuteContext { Engine = Engine, Event = _selectedEvent == null ? null : _selectedEvent.Event };
        }

        #endregion // Plugin

        public bool CGControllerExists => Engine.CGElementsController != null;

        public bool CGControllerIsMaster => Engine.CGElementsController?.IsMaster == true;

        public TEngineState EngineState => Engine.EngineState;

        public IEnumerable<IEvent> VisibleEvents => _visibleEvents;

        public IEnumerable<IEvent> RunningEvents => _runningEvents;

        public IEnumerable<EventPanelViewmodelBase> MultiSelectedEvents => _multiSelectedEvents;

        public void ClearSelection()
        {
            foreach (var evm in _multiSelectedEvents.ToList())
                evm.IsMultiSelected = false;
            _multiSelectedEvents.Clear();
        }

        public void RemoveMultiSelected(EventPanelViewmodelBase evm)
        {
            if (_multiSelectedEvents.Contains(evm))
                _multiSelectedEvents.Remove(evm);
        }

        public void _searchMissingEvents(object o)
        {
            Engine.SearchMissingEvents();
        }

        private void _onSelectedEventPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var selected = _selectedEvent;
            if (selected != null && sender == selected.Event && e.PropertyName == nameof(IEvent.PlayState))
                InvalidateRequerySuggested();
        }

        private void _engineOperation(object sender, EngineOperationEventArgs a)
        {
            if (a.Operation == TEngineOperation.Play)
            {
                if (a.Event.Layer == VideoLayer.Program)
                {
                    if (_trackPlayingEvent)
                        Application.Current?.Dispatcher.BeginInvoke((Action)(() =>
                        {
                            var pe = Engine.Playing;
                            if (pe != null)
                                SetOnTopView(pe);
                        }), null);
                    NotifyPropertyChanged(nameof(NextToPlay));
                    NotifyPropertyChanged(nameof(NextWithRequestedStartTime));
                }
                NotifyPropertyChanged(nameof(VisibleEvents));
            }

            if (a.Operation == TEngineOperation.Load)
            {
                InvalidateRequerySuggested();
            }

            if (a.Operation == TEngineOperation.Stop || a.Operation == TEngineOperation.Clear)
            {

                NotifyPropertyChanged(nameof(NextToPlay));
                NotifyPropertyChanged(nameof(NextWithRequestedStartTime));
            }


            if (a.Event != null
                && _selectedEvent != null
                && a.Event == _selectedEvent.Event)
                InvalidateRequerySuggested();
        }

        private void SetOnTopView(IEvent pe)
        {
            GetEventViewModel(pe)?.SetOnTop();
        }

        private void _engineTick(object sender, EngineTickEventArgs e)
        {
            CurrentTime = e.CurrentTime.ToLocalTime();
            TimeToAttention = e.TimeToAttention;
        }

        private void OnServerChannelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IPlayoutServerChannel.IsServerConnected))
            {
                if (sender == Engine.PlayoutChannelPRI)
                    NotifyPropertyChanged(nameof(ServerConnectedPRI));
                if (sender == Engine.PlayoutChannelSEC)
                    NotifyPropertyChanged(nameof(ServerConnectedSEC));
                if (sender == Engine.PlayoutChannelPRV)
                    NotifyPropertyChanged(nameof(ServerConnectedPRV));
                NotifyPropertyChanged(nameof(NoAlarms));
            }
            if (sender == Engine.PlayoutChannelPRI && e.PropertyName == nameof(IPlayoutServerChannel.AudioLevel))
                AudioLevelPRI = ((IPlayoutServerChannel)sender).AudioLevel;
        }

        private void OnEngineRunningEventsOperation(object o, CollectionOperationEventArgs<IEvent> e)
        {
            Application.Current?.Dispatcher.BeginInvoke((Action)(() =>
            {
                if (e.Operation == CollectionOperation.Add)
                    _runningEvents.Add(e.Item);
                else
                    _runningEvents.Remove(e.Item);
            }));

        }

        private void _selectedEvents_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged(nameof(SelectedCount));
            NotifyPropertyChanged(nameof(SelectedTime));
            InvalidateRequerySuggested();
        }

        private void _enginePropertyChanged(object o, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IEngine.ProgramAudioVolume)
                || e.PropertyName == nameof(IEngine.EngineState)
                || e.PropertyName == nameof(IEngine.GetNextToPlay)
                || e.PropertyName == nameof(IEngine.FieldOrderInverted)
            )
                NotifyPropertyChanged(e.PropertyName);
            if (e.PropertyName == nameof(IEngine.Playing))
            {
                NotifyPropertyChanged(nameof(PlayingEventName));
                NotifyPropertyChanged(nameof(IsPlayingMovie));
                NotifyPropertyChanged(nameof(NextToPlay));
            }
            if (e.PropertyName == nameof(IEngine.ForcedNext))
                NotifyPropertyChanged(nameof(IsForcedNext));
            if (e.PropertyName == nameof(IEngine.EngineState))
            {
                InvalidateRequerySuggested();
                if (((IEngine)o).EngineState == TEngineState.Idle)
                {
                    NotifyPropertyChanged(nameof(ServerPRIExists));
                    NotifyPropertyChanged(nameof(ServerSECExists));
                    NotifyPropertyChanged(nameof(ServerPRVExists));
                }
            }
            if (e.PropertyName == nameof(IEngine.DatabaseConnectionState))
            {
                NotifyPropertyChanged(nameof(NoAlarms));
                NotifyPropertyChanged(nameof(DatabaseOK));
            }
        }

        private void _mediaSearchViewModelMediaChoosen(object o, MediaSearchEventArgs e)
        {
            if (e.Media == null || !(o is MediaSearchViewmodel mediaSearchVm))
                return;
            IEvent newEvent;
            switch (e.Media.MediaType)
            {
                case TMediaType.Movie:
                    var category = e.Media.MediaCategory;
                    var cgController = Engine.CGElementsController;
                    var defaultCrawl = cgController?.DefaultCrawl ?? 0;
                    var defaultLogo = cgController?.DefaultLogo ?? 0;
                    newEvent = Engine.CreateNewEvent(
                        eventName: e.MediaName,
                        videoLayer: VideoLayer.Program,
                        eventType: TEventType.Movie,
                        scheduledTC: e.TCIn,
                        duration: e.Duration,
                        isCGEnabled: Engine.EnableCGElementsForNewEvents,
                        crawl: ((Engine.CrawlEnableBehavior == TCrawlEnableBehavior.ShowsOnly && category == TMediaCategory.Show)
                                      || (Engine.CrawlEnableBehavior == TCrawlEnableBehavior.AllButCommercials && (category == TMediaCategory.Show || category == TMediaCategory.Fill || category == TMediaCategory.Insert || category == TMediaCategory.Uncategorized))
                            ? defaultCrawl : (byte)0),
                        logo: (category == TMediaCategory.Fill || category == TMediaCategory.Show || category == TMediaCategory.Promo || category == TMediaCategory.Insert ? defaultLogo : (byte)0),
                        parental: e.Media.Parental
                    );
                    break;
                case TMediaType.Still:
                    newEvent = Engine.CreateNewEvent(
                        eventName: e.MediaName,
                        eventType: TEventType.StillImage,
                        videoLayer: mediaSearchVm.Layer,
                        duration: mediaSearchVm.BaseEvent.Duration);
                    break;
                case TMediaType.Animation:
                    if (!(e.Media is ITemplated templatedMedia))
                        return;
                    newEvent = Engine.CreateNewEvent(
                        eventName: e.MediaName,
                        eventType: TEventType.Animation,
                        videoLayer: VideoLayer.Animation,
                        scheduledDelay: templatedMedia.ScheduledDelay,
                        fields: templatedMedia.Fields);
                    break;
                default:
                    throw new ApplicationException("Invalid MediaType choosen");

            }
            newEvent.Media = e.Media;
            if (mediaSearchVm.NewEventStartType == TStartType.After)
                mediaSearchVm.BaseEvent.InsertAfter(newEvent);
            if (mediaSearchVm.NewEventStartType == TStartType.WithParent)
                mediaSearchVm.BaseEvent.InsertUnder(newEvent, (e.Media as ITemplated)?.StartType == TStartType.WithParentFromEnd);
            mediaSearchVm.NewEventStartType = TStartType.After;
            mediaSearchVm.BaseEvent = newEvent;
            LastAddedEvent = newEvent;
        }

        private void _cGElementsController_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ICGElementsController.IsMaster))
                NotifyPropertyChanged(nameof(CGControllerIsMaster));
        }

        private void _engineViewmodel_ClipboardChanged() => InvalidateRequerySuggested();

        private EventPanelViewmodelBase GetEventViewModel(IEvent aEvent)
        {
            IEnumerable<IEvent> rt = aEvent.GetVisualRootTrack().Reverse();
            EventPanelViewmodelBase evm = RootEventViewModel;
            foreach (IEvent ev in rt)
            {
                if (evm != null)
                {
                    evm = evm.Childrens.FirstOrDefault(e => e.Event == ev);
                    if (evm != null && evm.Event == aEvent)
                        return evm;
                }
            }
            return null;
        }
    }
}
