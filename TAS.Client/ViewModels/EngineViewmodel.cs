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
using System.Threading.Tasks;
using TAS.Client.Common.Plugin;
using TAS.Common.Interfaces;
using resources = TAS.Client.Common.Properties.Resources;

namespace TAS.Client.ViewModels
{
    public class EngineViewmodel : ViewModelBase, IUiEngine, IUiPreviewProvider
    {
        private EventPanelViewmodelBase _selectedEventPanel;
        private DateTime _currentTime;
        private TimeSpan _timeToAttention;
        private Views.EngineDebugView _debugWindow;
        private int _audioLevelPri = -100;
        private bool _trackPlayingEvent = true;
        private bool _isPropertiesPanelVisible = true;
        private readonly IUiPlugin[] _plugins;
        private EventEditViewmodel _selectedEventEditViewmodel;
        private IEvent _selectedEvent;
        private PreviewViewmodel _preview;

        private MediaSearchViewmodel _mediaSearchViewModel;
        private readonly ObservableCollection<IEvent> _visibleEvents = new ObservableCollection<IEvent>();
        private readonly ObservableCollection<IEvent> _runningEvents = new ObservableCollection<IEvent>();
        private readonly ObservableCollection<EventPanelViewmodelBase> _multiSelectedEvents = new ObservableCollection<EventPanelViewmodelBase>();
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        internal Views.EngineView View;

        public EngineViewmodel(IEngine engine, IPreview preview)
        {
            Debug.WriteLine($"Creating EngineViewmodel for {engine}");
            Engine = engine;
            VideoFormat = engine.VideoFormat;
            IsInterlacedFormat = engine.FormatDescription.Interlaced;
            Router = engine.Router;

            if (Router != null)
                EngineRouterViewModel = new EngineRouterViewModel(Router);

            RootEventViewModel = new EventPanelRootViewmodel(this);
            Engine.EngineTick += _engineTick;
            Engine.EngineOperation += EngineOperation;
            Engine.PropertyChanged += Engine_PropertyChanged;
            Engine.VisibleEventAdded += Engine_VisibleEventAdded;
            Engine.VisibleEventRemoved += Engine_VisibleEventRemoved;
            Engine.RunningEventsOperation += OnEngineRunningEventsOperation;
            _plugins = UiPluginManager.ComposeUiParts(this);
            VideoPreview = UiPluginManager.ComposePart<IVideoPreview>(this);
            

            if (preview != null && engine.HaveRight(EngineRight.Preview))
                _preview = new PreviewViewmodel(preview, engine.HaveRight(EngineRight.MediaEdit), false) { IsSegmentsVisible = true };

            _multiSelectedEvents.CollectionChanged += SelectedEvents_CollectionChanged;
            EventClipboard.ClipboardChanged += EngineViewmodel_ClipboardChanged;

            if (engine.PlayoutChannelPRI != null)
                engine.PlayoutChannelPRI.PropertyChanged += OnServerChannelPropertyChanged;
            if (engine.PlayoutChannelSEC != null)
                engine.PlayoutChannelSEC.PropertyChanged += OnServerChannelPropertyChanged;
            if (engine.Preview != null && engine.Preview.Channel != engine.PlayoutChannelSEC && engine.Preview.Channel != null)
                engine.Preview.Channel.PropertyChanged += OnServerChannelPropertyChanged;
            if (engine.CGElementsController != null)
            {
                engine.CGElementsController.PropertyChanged += CGElementsController_PropertyChanged;
                CGElementsControllerViewmodel = new EngineCGElementsControllerViewmodel(engine.CGElementsController);
            }


            CommandClearAll = new UiCommand(CommandName(nameof(Clear)), Clear, CanClear);
            CommandClearLayer = new UiCommand(CommandName(nameof(ClearLayer)), ClearLayer, CanClear);
            CommandClearMixer = new UiCommand(CommandName(nameof(Engine.ClearMixer)), _ => Engine.ClearMixer(), CanClear);
            CommandRestart = new UiCommand(CommandName(nameof(Engine.Restart)), _ => Engine.Restart(), CanClear);
            CommandStartSelected = new UiCommand(CommandName(nameof(StartSelected)), StartSelected, CanStartSelected);
            CommandLoadSelected = new UiCommand(CommandName(nameof(LoadSelected)), LoadSelected, CanLoadSelected);
            CommandScheduleSelected = new UiCommand(CommandName(nameof(Engine.Schedule)), _ => Engine.Schedule(_selectedEventPanel.Event), CanScheduleSelected);
            CommandRescheduleSelected = new UiCommand(CommandName(nameof(Engine.ReSchedule)), _ => Engine.ReSchedule(_selectedEventPanel.Event), CanRescheduleSelected);
            CommandForceNextSelected = new UiCommand(CommandName(nameof(ForceNext)), ForceNext, CanForceNextSelected);
            CommandTrackingToggle = new UiCommand(CommandName(nameof(TrackPlayingEvent)), _ => TrackPlayingEvent = !TrackPlayingEvent);
            CommandShowRunningItem = new UiCommand(CommandName(nameof(ShowRunningItem)), ShowRunningItem, _ => engine.Playing != null);
            CommandDebugToggle = new UiCommand(CommandName(nameof(DebugToggle)), DebugToggle);
            CommandRestartRundown = new UiCommand(CommandName(nameof(RestartRundown)), RestartRundown, CanClear);
            CommandRestartLayer = new UiCommand(CommandName(nameof(RestartLayer)), RestartLayer, _ => IsPlayingMovie && Engine.HaveRight(EngineRight.Play));
            CommandNewRootRundown = new UiCommand(CommandName(nameof(AddNewRootRundown)), AddNewRootRundown);
            CommandNewContainer = new UiCommand(CommandName(nameof(NewContainer)), NewContainer);
            CommandCheckDatabase = new UiCommand(CommandName(nameof(CheckDatabase)), CheckDatabase, _ => CurrentUser.IsAdmin);
            CommandStartLoaded = new UiCommand(CommandName(nameof(Engine.StartLoaded)), _ => Engine.StartLoaded(), _ => Engine.EngineState == TEngineState.Hold && Engine.HaveRight(EngineRight.Play));
            CommandDeleteSelected = new UiCommand(CommandName(nameof(DeleteSelected)), DeleteSelected, CanDeleteSelected);
            CommandCopySelected = new UiCommand(CommandName(nameof(CopySelected)), CopySelected, _ => _multiSelectedEvents.Count > 0);
            CommandCutSelected = new UiCommand(CommandName(nameof(CutSelected)), CutSelected, CanDeleteSelected);
            CommandPasteSelected = new UiCommand(CommandName(nameof(PasteSelected)), PasteSelected, CanPasteSelected);
            CommandExportMedia = new UiCommand(CommandName(nameof(ExportMedia)), ExportMedia, CanExportMedia);
            CommandUndelete = new UiCommand(CommandName(nameof(Undelete)), Undelete, CanUndelete);

            CommandEventHide = new UiCommand(CommandName(nameof(EventHide)), EventHide);
            CommandMoveUp = new UiCommand(CommandName(nameof(MoveUp)), MoveUp);
            CommandMoveDown = new UiCommand(CommandName(nameof(MoveDown)), MoveDown);
            CommandAddNextMovie = new UiCommand(CommandName(nameof(AddNextMovie)), AddNextMovie, CanAddNextMovie);
            CommandAddNextEmptyMovie = new UiCommand(CommandName(nameof(AddNextEmptyMovie)), AddNextEmptyMovie, CanAddNextEmptyMovie);
            CommandAddNextRundown = new UiCommand(CommandName(nameof(AddNextRundown)), AddNextRundown, CanAddNextRundown);
            CommandAddNextLive = new UiCommand(CommandName(nameof(AddNextLive)), AddNextLive, CanAddNextLive);
            CommandAddSubMovie = new UiCommand(CommandName(nameof(AddSubMovie)), AddSubMovie, CanAddSubMovie);
            CommandAddSubRundown = new UiCommand(CommandName(nameof(AddSubRundown)), AddSubRundown, CanAddSubRundown);
            CommandAddSubLive = new UiCommand(CommandName(nameof(AddSubLive)), AddSubLive, CanAddSubLive);
            CommandAddAnimation = new UiCommand(CommandName(nameof(AddAnimation)), AddAnimation, CanAddAnimation);
            CommandToggleLayer = new UiCommand(CommandName(nameof(ToggleLayer)), ToggleLayer);
            CommandToggleEnabled = new UiCommand(CommandName(nameof(ToggleEnabled)), ToggleEnabled);
            CommandToggleHold = new UiCommand(CommandName(nameof(ToggleHold)), ToggleHold);
            CommandTogglePropertiesPanel = new UiCommand(CommandName(nameof(CommandTogglePropertiesPanel)), _ => IsPropertiesPanelVisible = !IsPropertiesPanelVisible);
            CommandFocusEventName = new UiCommand(CommandName(nameof(FocusEventName)), FocusEventName, _ => IsPropertiesPanelVisible && SelectedEventEditViewmodel != null);
            CommandFocusRundown = new UiCommand(CommandName(nameof(FocusRundown)), FocusRundown);
            CommandToggleCg = new UiCommand(CommandName(nameof(ToggleCg)), ToggleCg, CanToggleCg);

            CommandSearchDo = new UiCommand(CommandName(nameof(Search)), Search, CanSearch);
            CommandSearchShowPanel = new UiCommand(CommandName(nameof(ShowSearchPanel)), ShowSearchPanel);
            CommandSearchHidePanel = new UiCommand(CommandName(nameof(HideSearchPanel)), HideSearchPanel);
            CommandSetTimeCorrection = new UiCommand(CommandName(nameof(SetTimeCorrection)), SetTimeCorrection);

            CommandSaveEdit = new UiCommand(CommandName(nameof(SaveEdit)), SaveEdit);
            CommandUndoEdit = new UiCommand(CommandName(nameof(UndoEdit)), UndoEdit);

            CommandSaveRundown = new UiCommand(CommandName(nameof(SaveRundown)), SaveRundown, _ => SelectedEventPanel != null && SelectedEventPanel.Event.EventType == TEventType.Rundown);
            CommandLoadRundown = new UiCommand(CommandName(nameof(LoadRundown)), LoadRundown, o => o.Equals("Under") ? CanAddSubRundown(o) : CanAddNextRundown(o));
            CommandUserManager = new UiCommand(CommandName(nameof(UserManager)), UserManager, CanUserManager);

            CommandEngineRights = new UiCommand(CommandName(nameof(EngineRights)), EngineRights, CanEngineRights);
        }

        public ICommand CommandClearAll { get; }
        public ICommand CommandClearMixer { get; }
        public ICommand CommandClearLayer { get; }
        public ICommand CommandRestart { get; }
        public ICommand CommandStartSelected { get; }
        public ICommand CommandForceNextSelected { get; }
        public ICommand CommandStartLoaded { get; }
        public ICommand CommandLoadSelected { get; }
        public ICommand CommandScheduleSelected { get; }
        public ICommand CommandRescheduleSelected { get; }
        public ICommand CommandTrackingToggle { get; }
        public ICommand CommandShowRunningItem { get; }
        public ICommand CommandRestartRundown { get; }
        public ICommand CommandNewRootRundown { get; }
        public ICommand CommandNewContainer { get; }
        public ICommand CommandDebugToggle { get; }
        public ICommand CommandCheckDatabase { get; }
        public ICommand CommandDeleteSelected { get; }
        public ICommand CommandCopySelected { get; }
        public ICommand CommandPasteSelected { get; }
        public ICommand CommandCutSelected { get; }
        public ICommand CommandExportMedia { get; }
        public ICommand CommandUndelete { get; }
        public ICommand CommandSaveRundown { get; }
        public ICommand CommandLoadRundown { get; }
        public ICommand CommandRestartLayer { get; }
        public ICommand CommandSearchDo { get; }
        public ICommand CommandSearchShowPanel { get; }
        public ICommand CommandSearchHidePanel { get; }
        public ICommand CommandSetTimeCorrection { get; }


        #region Single selected commands
        public ICommand CommandEventHide { get; }
        public ICommand CommandAddNextMovie { get; }
        public ICommand CommandAddNextEmptyMovie { get; }
        public ICommand CommandAddNextRundown { get; }
        public ICommand CommandAddNextLive { get; }
        public ICommand CommandAddSubMovie { get; }
        public ICommand CommandAddSubRundown { get; }
        public ICommand CommandAddSubLive { get; }
        public ICommand CommandAddAnimation { get; }
        public ICommand CommandToggleEnabled { get; }
        public ICommand CommandToggleHold { get; }
        public ICommand CommandToggleLayer { get; }
        public ICommand CommandMoveUp { get; }
        public ICommand CommandMoveDown { get; }
        public ICommand CommandToggleCg { get; }
        #endregion // Single selected commands
        #region Editor commands
        public ICommand CommandSaveEdit { get; }
        public ICommand CommandUndoEdit { get; }

        #endregion // Editor commands
        public ICommand CommandUserManager { get; }
        public ICommand CommandEngineRights { get; }
        public ICommand CommandTogglePropertiesPanel { get; }
        public ICommand CommandFocusEventName { get; }
        public ICommand CommandFocusRundown { get; }

        #region PreviewCommands

        public ICommand CommandPreviewCue => _preview?.CommandCue;
        public ICommand CommandPreviewTogglePlay => _preview?.CommandTogglePlay;
        public ICommand CommandPreviewUnload => _preview?.CommandUnload;
        public ICommand CommandPreviewFastForward => _preview?.CommandFastForward;
        public ICommand CommandPreviewBackward => _preview?.CommandBackward;
        public ICommand CommandPreviewFastForwardOneFrame => _preview?.CommandFastForwardOneFrame;
        public ICommand CommandPreviewBackwardOneFrame => _preview?.CommandBackwardOneFrame;

        public ICommand CommandPreviewTrimSource => _preview?.CommandTrimSource;
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

        public EventPanelRootViewmodel RootEventViewModel { get; }

        public IUiPreview Preview => _preview;

        public EngineRouterViewModel EngineRouterViewModel { get; }

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
                var cp = Engine.Playing;
                if (cp != null)
                    SetOnTopView(cp);
            }
        }

        public bool IsInterlacedFormat { get; }

        public void Focus()
        {
            View?.RundownTreeView.Focus();
        }

        protected override void OnDispose()
        {
            Engine.EngineTick -= _engineTick;
            Engine.EngineOperation -= EngineOperation;
            Engine.PropertyChanged -= Engine_PropertyChanged;
            Engine.VisibleEventAdded -= Engine_VisibleEventAdded;
            Engine.VisibleEventRemoved -= Engine_VisibleEventRemoved;
            Engine.RunningEventsOperation -= OnEngineRunningEventsOperation;

            _multiSelectedEvents.CollectionChanged -= SelectedEvents_CollectionChanged;
            EventClipboard.ClipboardChanged -= EngineViewmodel_ClipboardChanged;
            if (Engine.PlayoutChannelPRI != null)
                Engine.PlayoutChannelPRI.PropertyChanged -= OnServerChannelPropertyChanged;
            if (Engine.PlayoutChannelSEC != null)
                Engine.PlayoutChannelSEC.PropertyChanged -= OnServerChannelPropertyChanged;
            if (Engine.Preview != null && Engine.Preview.Channel != Engine.PlayoutChannelSEC && Engine.Preview.Channel != null)
                Engine.Preview.Channel.PropertyChanged -= OnServerChannelPropertyChanged;
            VideoPreview?.Dispose();
            if (Engine.CGElementsController != null)
                Engine.CGElementsController.PropertyChanged -= CGElementsController_PropertyChanged;
            EngineRouterViewModel?.Dispose();
        }


        #region Command methods

        private void UndoEdit(object _)
        {
            if (SelectedEventEditViewmodel == null)
                return;
            if (SelectedEventEditViewmodel.IsModified)
                SelectedEventEditViewmodel.UndoEdit();
        }

        private void SaveEdit(object _)
        {
            if (SelectedEventEditViewmodel == null) 
                return;
            if (SelectedEventEditViewmodel.CanSave)
                SelectedEventEditViewmodel.Save();
        }

        private void MoveUp(object _)
        {
            var e = SelectedEventPanel?.Event;
            if (e == null || !e.CanMoveUp())
                return;
            SelectedEventPanel?.Event.MoveUp();
        }

        private void MoveDown(object _)
        {
            var e = SelectedEventPanel?.Event;
            if (e == null || !e.CanMoveDown())
                return;
            SelectedEventPanel?.Event.MoveDown();
        }

        private void SetTimeCorrection(object obj)
        {
            if (!(obj is string strValue && int.TryParse(strValue, out var intValue)))
                return;
            Engine.TimeCorrection += intValue;
        }

        private bool CanDeleteSelected(object _) => _multiSelectedEvents.Count > 0 && _multiSelectedEvents.All(e => e?.Event != null && e.Event.HaveRight(EventRight.Delete) && e.Event.AllowDelete());

        private void EngineRights(object _)
        {
            using (var vm = new EngineRightsEditViewmodel(Engine, Engine.AuthenticationService))
                UiServices.ShowDialog<Views.EngineRightsEditView>(vm);
        }

        private bool CanEngineRights(object _) => CurrentUser.IsAdmin;

        private void UserManager(object _)
        {
            var vm = new UserManagerViewmodel(Engine.AuthenticationService);
            UiServices.ShowWindow<Views.UserManagerView>(vm).Closed += (s, e) =>
                vm.Dispose();
        }

        private bool CanUserManager(object _) => CurrentUser.IsAdmin;

        private bool CanUndelete(object _) => EventClipboard.CanUndo();

        private void Undelete(object _)
        {
            if (MessageBox.Show(resources._query_Undelete, resources._caption_Confirmation, MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                EventClipboard.Undo();
        }

        private void Clear(object _) => Engine.Clear();

        private void ClearLayer(object layer) => Engine.Clear((VideoLayer)int.Parse((string)layer));

        private void ClearMixer(object _) => Engine.ClearMixer();

        private bool CanClear(object _) => Engine.HaveRight(EngineRight.Play);

        private void FocusRundown(object _)
        {
            Focus();
        }

        private void FocusEventName(object _)
        {
            SelectedEventEditViewmodel?.SetFocusOnEventName();
        }

        private void Engine_VisibleEventRemoved(object _, EventEventArgs e)
        {
            OnUiThread(() => _visibleEvents.Remove(e.Event));
        }

        private void Engine_VisibleEventAdded(object _, EventEventArgs e)
        {
            OnUiThread(() => _visibleEvents.Add(e.Event));
        }

        private void LoadRundown(object obj)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog()
            {
                DefaultExt = FileUtils.RundownFileExtension,
                Filter = $"{resources._rundowns}|*{FileUtils.RundownFileExtension}|{resources._allFiles}|*.*"
            };
            if (dlg.ShowDialog() != true)
                return;
            using (var reader = File.OpenText(dlg.FileName))
            using (var jreader = new Newtonsoft.Json.JsonTextReader(reader))
            {
                var proxy = new Newtonsoft.Json.JsonSerializer
                {
                    DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Populate
                }
                    .Deserialize<EventProxy>(jreader);
                if (proxy != null)
                {
                    var mediaFiles =
                        (Engine.MediaManager.MediaDirectoryPRI ?? Engine.MediaManager.MediaDirectorySEC)
                        ?.GetAllFiles();
                    var animationFiles =
                        (Engine.MediaManager.AnimationDirectoryPRI ?? Engine.MediaManager.AnimationDirectorySEC)
                        ?.GetAllFiles();
                    var newEvent = obj.Equals("Under")
                        ? proxy.InsertUnder(SelectedEventPanel.Event, false, mediaFiles, animationFiles)
                        : proxy.InsertAfter(SelectedEventPanel.Event, mediaFiles, animationFiles);
                    LastAddedEvent = newEvent;
                }

            }
        }

        private void SaveRundown(object _)
        {
            EventProxy proxy = EventProxy.FromEvent(SelectedEventPanel.Event);
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

        private bool CanAddSubLive(object obj) => SelectedEventPanel is EventPanelRundownViewmodel ep && ep.CommandAddSubLive.CanExecute(obj);

        private bool CanAddSubRundown(object obj)
        {
            if (SelectedEventPanel is EventPanelRundownViewmodel ep)
                return ep.CommandAddSubRundown.CanExecute(obj);
            return SelectedEventPanel is EventPanelContainerViewmodel ec && ec.CommandAddSubRundown.CanExecute(obj);
        }

        private bool CanAddSubMovie(object obj) => SelectedEventPanel is EventPanelRundownViewmodel ep && ep.CommandAddSubMovie.CanExecute(obj);

        private bool CanAddNextLive(object obj) => SelectedEventPanel is EventPanelRundownElementViewmodelBase ep && ep.CommandAddNextLive.CanExecute(obj);

        private bool CanAddNextRundown(object obj) => SelectedEventPanel is EventPanelRundownElementViewmodelBase ep && ep.CommandAddNextRundown.CanExecute(obj);

        private bool CanAddNextEmptyMovie(object obj) => SelectedEventPanel is EventPanelRundownElementViewmodelBase ep && ep.CommandAddNextEmptyMovie.CanExecute(obj);

        private bool CanAddNextMovie(object obj) => SelectedEventPanel is EventPanelRundownElementViewmodelBase ep && ep.CommandAddNextMovie.CanExecute(obj);

        private void ToggleHold(object obj) => (SelectedEventPanel as EventPanelRundownElementViewmodelBase)?.CommandToggleHold.Execute(obj);

        private void ToggleEnabled(object obj) => (SelectedEventPanel as EventPanelRundownElementViewmodelBase)?.CommandToggleEnabled.Execute(obj);

        private void ForceNext(object obj) => Engine.ForceNext(IsForcedNext ? null : _selectedEventPanel.Event);

        private bool CanForceNextSelected(object obj) => Engine.EngineState == TEngineState.Running && (CanLoadSelected(obj) || IsForcedNext);

        private void ToggleLayer(object obj) => (SelectedEventPanel as EventPanelRundownElementViewmodelBase)?.CommandToggleLayer.Execute(obj);

        private void AddSubLive(object obj) => (SelectedEventPanel as EventPanelRundownViewmodel)?.CommandAddSubLive.Execute(obj);

        private void AddAnimation(object obj) => (SelectedEventPanel as EventPanelRundownElementViewmodelBase)?.CommandAddAnimation.Execute(obj);

        private bool CanAddAnimation(object obj) => SelectedEventPanel is EventPanelRundownElementViewmodelBase ep && ep.CommandAddAnimation.CanExecute(obj);

        private void ToggleCg(object _)
        {
            if (SelectedEvent == null)
                return;
            SelectedEvent.IsCGEnabled = !SelectedEvent.IsCGEnabled;
            SelectedEvent.Save();
        }

        private bool CanToggleCg(object _)
        {
            return Engine.CGElementsController?.IsConnected == true
            && SelectedEvent != null
            && SelectedEvent.PlayState == TPlayState.Scheduled
            && (SelectedEvent.EventType == TEventType.Movie || SelectedEvent.EventType == TEventType.Live)
            && SelectedEvent.HaveRight(EventRight.Modify);
        }
        
        private void AddSubRundown(object obj)
        {
            (SelectedEventPanel as EventPanelRundownViewmodel)?.CommandAddSubRundown.Execute(obj);
            (SelectedEventPanel as EventPanelContainerViewmodel)?.CommandAddSubRundown.Execute(obj);
        }

        private void AddSubMovie(object obj) => (SelectedEventPanel as EventPanelRundownViewmodel)?.CommandAddSubMovie.Execute(obj);

        private void AddNextLive(object obj) => (SelectedEventPanel as EventPanelRundownElementViewmodelBase)?.CommandAddNextLive.Execute(obj);

        private void AddNextRundown(object obj) => (SelectedEventPanel as EventPanelRundownElementViewmodelBase)?.CommandAddNextRundown.Execute(obj);

        private void AddNextEmptyMovie(object obj) => (SelectedEventPanel as EventPanelRundownElementViewmodelBase)?.CommandAddNextEmptyMovie.Execute(obj);

        private void AddNextMovie(object obj) => (SelectedEventPanel as EventPanelRundownElementViewmodelBase)?.CommandAddNextMovie.Execute(obj);

        private void EventHide(object obj) => (SelectedEventPanel as EventPanelContainerViewmodel)?.CommandHide.Execute(obj);

        private void PasteSelected(object obj)
        {
            if (!Enum.TryParse<EventClipboard.PasteLocation>((string)obj, out EventClipboard.PasteLocation location))
                return;
            var added = EventClipboard.Paste(_selectedEventPanel, location);
            var vm = RootEventViewModel.Find(added, true);
            if (vm != null)
                vm.IsSelected = true;
        }

        private bool CanPasteSelected(object obj)
        {
            if (!Enum.TryParse<EventClipboard.PasteLocation>((string)obj, out EventClipboard.PasteLocation location))
                return false;
            return EventClipboard.CanPaste(_selectedEventPanel, location);
        }

        private void CopySelected(object obj) => EventClipboard.Copy(_multiSelectedEvents.ToList());

        private void CutSelected(object obj) => EventClipboard.Cut(_multiSelectedEvents.ToList());

        private bool CanExportMedia(object obj)
        {
            if (!Engine.HaveRight(EngineRight.MediaExport))
                return false;

            bool exportAll = obj != null;
            return _multiSelectedEvents.Any(e => e is EventPanelMovieViewmodel m
                                                 && (m.IsEnabled || exportAll))
                   && Engine.MediaManager.IngestDirectories.Any(d => d.IsExport);
        }

        private void ExportMedia(object obj)
        {
            bool exportAll = obj != null;
            var selections = _multiSelectedEvents.Where(e => e is EventPanelMovieViewmodel m
                                                             && (m.IsEnabled || exportAll)
                                                             && m.Media?.FileExists() == true)
                .OrderBy(e => e.Event.ScheduledTime)
                .Select(e => new MediaExportDescription(
                    e.Event.Media,
                    e.Event.GetSubEvents().Where(sev => sev.EventType == TEventType.StillImage && sev.Media != null)
                        .Select(sev => sev.Media).ToList(),
                    e.Event.ScheduledTc,
                    e.Event.Duration,
                    e.Event.GetAudioVolume()));
            using (var vm = new ExportViewmodel(Engine, selections))
            {
                UiServices.ShowDialog<Views.ExportView>(vm);
            }
        }

        private void StartSelected(object obj)
        {
            var eventToStart = SelectedEventPanel.Event;
            if (Engine.EngineState != TEngineState.Running
                || Engine.Playing?.EventType == TEventType.Live
                || (string)obj == "Force"
                || MessageBox.Show(string.Format(resources._query_PlayWhileRunning), resources._caption_Confirmation, MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK
                )
                Engine.Start(eventToStart);
        }

        private bool CanStartSelected(object o)
        {
            IEvent ev = _selectedEventPanel?.Event;
            return ev != null
                   && ev.IsEnabled
                   && (ev.PlayState == TPlayState.Scheduled || ev.PlayState == TPlayState.Paused || ev.PlayState == TPlayState.Aborted)
                   && (ev.EventType == TEventType.Rundown || ev.EventType == TEventType.Live || ev.EventType == TEventType.Movie)
                   && Engine.HaveRight(EngineRight.Play);
        }

        private void LoadSelected(object obj)
        {
            var eventToLoad = SelectedEventPanel.Event;
            if (Engine.EngineState != TEngineState.Running
                || Engine.Playing?.EventType == TEventType.Live
                || MessageBox.Show(string.Format(resources._query_LoadWhileRunning), resources._caption_Confirmation, MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK
                )
                Engine.Load(eventToLoad);
        }

        private bool CanLoadSelected(object o)
        {
            IEvent ev = _selectedEventPanel?.Event;
            return ev != null
                && ev.IsEnabled
                && (ev.PlayState == TPlayState.Scheduled || ev.PlayState == TPlayState.Aborted)
                && (ev.EventType == TEventType.Rundown || ev.EventType == TEventType.Live || ev.EventType == TEventType.Movie)
                && Engine.HaveRight(EngineRight.Play);
        }

        private bool CanScheduleSelected(object o)
        {
            IEvent ev = _selectedEventPanel?.Event;
            return ev != null
                   && (ev.PlayState == TPlayState.Scheduled || ev.PlayState == TPlayState.Paused)
                   && ev.ScheduledTime >= _currentTime
                   && Engine.HaveRight(EngineRight.Play);
        }

        private bool CanRescheduleSelected(object o)
        {
            IEvent ev = _selectedEventPanel?.Event;
            return ev != null
                   && (ev.PlayState == TPlayState.Aborted || ev.PlayState == TPlayState.Played)
                   && Engine.HaveRight(EngineRight.Play);
        }

        private void RestartRundown(object o)
        {
            IEvent ev = _selectedEventPanel?.Event;
            if (ev != null)
                Engine.RestartRundown(ev);
        }

        private void RestartLayer(object obj)
        {
            Engine.Restart();
        }

        private void AddNewRootRundown(object o)
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

        private void NewContainer(object o)
        {
            IEvent newEvent = Engine.CreateNewEvent(
                eventType: TEventType.Container,
                eventName: resources._title_NewContainer);
            Engine.AddRootEvent(newEvent);
            newEvent.Save();
            LastAddedEvent = newEvent;
        }

        private void DeleteSelected(object ob)
        {
            var evmList = _multiSelectedEvents.Where(vm => vm?.Event != null).ToList();
            if (evmList.Count == 0
                || MessageBox.Show(string.Format(resources._query_DeleteSelected, evmList.Count, evmList.AsString(Environment.NewLine)), resources._caption_Confirmation, MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                return;
            _multiSelectedEvents.Clear();
            UiServices.SetBusyState();
            var firstEvent = evmList.First().Event;
            EventClipboard.SaveUndo(evmList.Select(evm => evm.Event).ToList(), firstEvent.StartType == TStartType.After ? firstEvent.GetPrior() : firstEvent.GetParent());
            Task.Run(
                () =>
                {
                    try
                    {
                        foreach (var evm in evmList)
                        {
                            if (evm.Event is null
                                || !(evm.Event.PlayState == TPlayState.Scheduled || evm.Event.PlayState == TPlayState.Played || evm.Event.PlayState == TPlayState.Aborted))
                                continue;
                            evm.Event.Delete();
                            Logger.LogEventDeletion(evm.Event);
                        }
                    }
                    catch (Exception e)
                    {
                        OnUiThread(() =>
                        {
                            MessageBox.Show(string.Format(resources._message_CommandFailed, e.Message), resources._caption_Error, MessageBoxButton.OK, MessageBoxImage.Hand);
                        });
                    }
                }
            );
        }

        private void DebugToggle(object o)
        {
            if (_debugWindow != null)
                return;
            _debugWindow = UiServices.ShowWindow<Views.EngineDebugView>(this);
            _debugWindow.Closed += (w, e) =>
            {
                if (w is Views.EngineDebugView window)
                    window.DataContext = null;
                _debugWindow = null;
            };
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
                    Engine.HaveRight(EngineRight.Preview) ? Engine.Preview : null,
                    Engine, mediaType, layer, closeAfterAdd, baseEvent.Media?.FormatDescription())
                {
                    BaseEvent = baseEvent,
                    NewEventStartType = startType
                };
                mediaSearchViewModel.MediaChoosen += MediaSearchViewModelMediaChoosen;
                if (closeAfterAdd)
                {
                    UiServices.ShowDialog<Views.MediaSearchView>(mediaSearchViewModel);
                    mediaSearchViewModel.MediaChoosen -= MediaSearchViewModelMediaChoosen;
                    mediaSearchViewModel.Dispose();
                }
                else
                {
                    mediaSearchViewModel.Window = UiServices.ShowWindow<Views.MediaSearchView>(mediaSearchViewModel);
                    mediaSearchViewModel.Window.Closed += (sender, args) =>
                    {
                        mediaSearchViewModel.MediaChoosen -= MediaSearchViewModelMediaChoosen;
                        mediaSearchViewModel.Dispose();
                        _mediaSearchViewModel = null;
                        _selectedEventPanel?.Focus();
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
                        isCGEnabled: Engine.EnableCGElementsForNewEvents,
                        isHold: Engine.StudioMode,
                        duration: new TimeSpan(0, 10, 0));
                    break;
                case TEventType.Movie:
                    newEvent = Engine.CreateNewEvent(
                        eventType: TEventType.Movie,
                        eventName: resources._title_EmptyMovie,
                        isCGEnabled: Engine.EnableCGElementsForNewEvents,
                        isHold: Engine.StudioMode,
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
                LastAddedEvent = newEvent;
                if (insertUnder)
                {
                    if (baseEvent.EventType == TEventType.Container)
                        newEvent.ScheduledTime = _currentTime;
                    baseEvent.InsertUnder(newEvent, false);
                }
                else
                    baseEvent.InsertAfter(newEvent);
            }
        }

        /// <summary>
        /// Used to determine if it should be selected when it's viewmodel is created
        /// </summary>
        public IEvent LastAddedEvent { get; private set; }

        #endregion // MediaSearch

        #region Search panel

        private bool _isSearchPanelVisible;
        private void ShowSearchPanel(object obj)
        {
            IsSearchNotFound = false;
            IsSearchPanelVisible = true;
            IsSearchBoxFocused = true;
        }

        private void HideSearchPanel(object obj)
        {
            IsSearchPanelVisible = false;
            SelectedEventPanel?.Focus();
        }

        private bool _isSearchBoxFocused;

        private bool _isSearchNotFound;
        public bool IsSearchNotFound { get => _isSearchNotFound; set => SetField(ref _isSearchNotFound, value); }

        private string _searchText;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (!SetField(ref _searchText, value))
                    return;
                IsSearchNotFound = false;
            }
        }

        public bool CanSearch(object o)
        {
            return !string.IsNullOrWhiteSpace(_searchText);
        }

        private void Search(object o)
        {
            var current = _selectedEventPanel?.Event;
            if (current == null || string.IsNullOrWhiteSpace(SearchText))
                return;
            string loweredSearchtext = SearchText.ToLower();
            bool SearchFunc(IEvent e) => e.EventName.ToLower().Contains(loweredSearchtext);
            var visualRootTrack = current.GetVisualRootTrack();
            var found = current.FindInside(SearchFunc);
            if (found == null)
                foreach (var et in visualRootTrack)
                {
                    current = et.GetNext();
                    if (current != null)
                        found = current.FindNext(SearchFunc);
                    if (found != null)
                        break;
                }
            if (found != null)
            {
                var rootTrack = found.GetVisualRootTrack().ToArray();
                EventPanelViewmodelBase cl = RootEventViewModel;
                for (var i = rootTrack.Length - 1; i >= 0; i--)
                {
                    cl = cl.Find(rootTrack[i], false);
                    if (cl == null)
                        return;
                    if (cl.Event == found)
                        cl.IsSelected = true;
                    else
                        cl.IsExpanded = true;
                }
            }
            else
                IsSearchNotFound = true;
        }
        #endregion

        public EventPanelViewmodelBase SelectedEventPanel
        {
            get => _selectedEventPanel;
            set
            {
                if (value == _selectedEventPanel)
                    return;
                if (SelectedEventEditViewmodel?.IsModified == true)
                {
                    if (SelectedEventEditViewmodel.IsValid)
                        switch (MessageBox.Show(string.Format(resources._query_SaveChangedData, SelectedEventEditViewmodel.EventName), resources._caption_Confirmation, MessageBoxButton.YesNoCancel))
                        {
                            case MessageBoxResult.Cancel:
                                NotifyPropertyChanged(nameof(SelectedEventPanel));
                                return;
                            case MessageBoxResult.Yes:
                                SelectedEventEditViewmodel.Save();
                                break;
                        }
                    else
                    if (MessageBox.Show(string.Format(resources._query_AbadonChanges, SelectedEventEditViewmodel.EventName), resources._caption_Confirmation, MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                    {
                        NotifyPropertyChanged(nameof(SelectedEventPanel));
                        return;
                    }
                }
                _selectedEventPanel = value;
                SelectedEvent = value?.Event;
                if (value is EventPanelRundownElementViewmodelBase re && _mediaSearchViewModel != null)
                {
                    _mediaSearchViewModel.BaseEvent = re.Event;
                    _mediaSearchViewModel.NewEventStartType = TStartType.After;
                }
                InvalidateRequerySuggested();
                IsSearchNotFound = false;
            }
        }

        public IEvent SelectedEvent
        {
            get => _selectedEvent;
            private set
            {
                var oldSelectedEvent = _selectedEvent;
                if (!SetField(ref _selectedEvent, value))
                    return;
                if (oldSelectedEvent != null)
                    oldSelectedEvent.PropertyChanged -= OnSelectedEvent_PropertyChanged;
                if (value != null)
                {
                    value.PropertyChanged += OnSelectedEvent_PropertyChanged;
                    SelectedEventEditViewmodel = new EventEditViewmodel(value, this);
                }
                else
                    SelectedEventEditViewmodel = null;
                if (_preview != null)
                    _preview.SelectedEvent = value;
            }
        }

        public EventEditViewmodel SelectedEventEditViewmodel
        {
            get => _selectedEventEditViewmodel; set
            {
                var oldSelectedEventEditViewModel = _selectedEventEditViewmodel;
                if (!SetField(ref _selectedEventEditViewmodel, value))
                    return;
                oldSelectedEventEditViewModel?.Dispose();
            }
        }

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

        public bool ServerPRIExists => Engine.PlayoutChannelPRI != null;

        public bool ServerConnectedPRI => Engine.PlayoutChannelPRI?.IsServerConnected == true;

        public bool ServerSECExists => Engine.PlayoutChannelSEC != null;

        public bool ServerConnectedSEC => Engine.PlayoutChannelSEC?.IsServerConnected == true;

        public bool ServerPRVExists => Engine.Preview != null;

        public bool ServerConnectedPRV => Engine.Preview?.Channel.IsServerConnected == true;

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
                var e = Engine.NextToPlay;
                return e == null ? string.Empty : e.EventName;
            }
        }

        public IEvent NextWithRequestedStartTime => Engine.GetNextWithRequestedStartTime();

        public int SelectedCount => _multiSelectedEvents.Count;

        public TimeSpan SelectedTime => TimeSpan.FromTicks(_multiSelectedEvents.Sum(e => e.Event.Duration.Ticks));

        public bool IsForcedNext => Engine.ForcedNext != null;

        public int AudioLevelPRI
        {
            get => _audioLevelPri;
            private set => SetField(ref _audioLevelPri, value);
        }

        #region Plugin

        public IList<IUiPlugin> Plugins => _plugins;


        public IVideoPreview VideoPreview { get; }

        public IRouter Router { get; }

        public bool IsAnyPluginVisible => _plugins != null && _plugins.Any(p => p.Menu != null);

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
                if (evm != null)
                    evm.IsMultiSelected = false;
            _multiSelectedEvents.Clear();
        }

        public void RemoveEventPanel(EventPanelViewmodelBase evm)
        {
            if (_multiSelectedEvents.Contains(evm))
                _multiSelectedEvents.Remove(evm);
            if (SelectedEventPanel == evm)
                SelectedEvent = null;
        }

        public void CheckDatabase(object o)
        {
            bool recoverFoundEvents = false;
            switch (MessageBox.Show(resources._query_RevoverFoundEvents, resources._caption_Confirmation, MessageBoxButton.YesNoCancel))
            {
                case MessageBoxResult.Yes:
                    recoverFoundEvents = true;
                    break;
                case MessageBoxResult.No:
                    break;
                default:
                    return;
            }
            UiServices.SetBusyState();
            var recovered = Engine.CheckDatabase(recoverFoundEvents);
            MessageBox.Show(string.Format(resources._message_FoundEvents, recovered), resources._caption_Information);
        }

        private void OnSelectedEvent_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var selected = _selectedEventPanel;
            if (selected != null && sender == selected.Event && e.PropertyName == nameof(IEvent.PlayState))
                InvalidateRequerySuggested();
        }

        private void EngineOperation(object sender, EngineOperationEventArgs a)
        {
            if (a.Operation == TEngineOperation.Play)
            {
                if (a.Event.Layer == VideoLayer.Program)
                {
                    if (_trackPlayingEvent)
                        OnUiThread(() =>
                        {
                            var pe = Engine.Playing;
                            if (pe != null)
                                SetOnTopView(pe);
                        });
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
                && _selectedEventPanel != null
                && a.Event == _selectedEventPanel.Event)
                InvalidateRequerySuggested();
        }

        private void SetOnTopView(IEvent pe)
        {
            var rootTrack = pe.GetVisualRootTrack().ToArray().Reverse();
            EventPanelViewmodelBase vm = RootEventViewModel;
            foreach (var ev in rootTrack)
            {
                vm = vm.Find(ev, false);
                if (vm is null)
                    return;
                if (ev.EventType == TEventType.Rundown || ev.EventType == TEventType.Container)
                    vm.IsExpanded = true;
                if (vm.Event == pe)
                    Application.Current?.Dispatcher.BeginInvoke(
                        (Action)vm.SetOnTop,
                        System.Windows.Threading.DispatcherPriority.Input // after expanded tree is rendered
                        );
            }
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
                if (sender == Engine.Preview?.Channel)
                    NotifyPropertyChanged(nameof(ServerConnectedPRV));
                NotifyPropertyChanged(nameof(NoAlarms));
            }
            if (sender == Engine.PlayoutChannelPRI && e.PropertyName == nameof(IPlayoutServerChannel.AudioLevel))
                AudioLevelPRI = ((IPlayoutServerChannel)sender).AudioLevel;
        }

        private void OnEngineRunningEventsOperation(object o, CollectionOperationEventArgs<IEvent> e)
        {
            OnUiThread(() =>
            {
                if (e.Operation == CollectionOperation.Add)
                    _runningEvents.Add(e.Item);
                else
                    _runningEvents.Remove(e.Item);
            });
        }

        private void SelectedEvents_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged(nameof(SelectedCount));
            NotifyPropertyChanged(nameof(SelectedTime));
            InvalidateRequerySuggested();
        }

        private void Engine_PropertyChanged(object o, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IEngine.ProgramAudioVolume)
                || e.PropertyName == nameof(IEngine.EngineState)
                || e.PropertyName == nameof(IEngine.NextToPlay)
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

        private void MediaSearchViewModelMediaChoosen(object o, MediaSearchEventArgs e)
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
                        isHold: Engine.StudioMode,
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
            LastAddedEvent = newEvent;
            switch (mediaSearchVm.NewEventStartType)
            {
                case TStartType.After:
                    mediaSearchVm.BaseEvent.InsertAfter(newEvent);
                    break;
                case TStartType.WithParent:
                    mediaSearchVm.BaseEvent.InsertUnder(newEvent, (e.Media as ITemplated)?.StartType == TStartType.WithParentFromEnd);
                    break;
            }
            mediaSearchVm.NewEventStartType = TStartType.After;
            mediaSearchVm.BaseEvent = newEvent;
        }

        private void CGElementsController_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ICGElementsController.IsMaster))
                NotifyPropertyChanged(nameof(CGControllerIsMaster));
        }

        private void ShowRunningItem(object obj)
        {
            var playing = Engine.Playing;
            if (playing == null)
                return;
            var rootTrack = playing.GetVisualRootTrack().ToArray();
            EventPanelViewmodelBase vm = RootEventViewModel;
            for (var i = rootTrack.Length - 1; i >= 0; i--)
            {
                vm = vm.Find(rootTrack[i], false);
                if (vm?.IsVisible != true)
                    return;
                if (vm.Event == playing)
                    vm.SetOnTop();
                else
                    vm.IsExpanded = true;
            }
        }

        private void EngineViewmodel_ClipboardChanged() => InvalidateRequerySuggested();

    }
}
