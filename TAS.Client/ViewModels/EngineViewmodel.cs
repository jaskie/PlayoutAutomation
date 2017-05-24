using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using TAS.Client.Common;
using TAS.Server.Common;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition;
using TAS.Client.Common.Plugin;
using TAS.Server.Common.Interfaces;
using resources = TAS.Client.Common.Properties.Resources;

namespace TAS.Client.ViewModels
{
    public class EngineViewmodel : ViewmodelBase
    {
        private readonly IEngine _engine;
        private readonly PreviewViewmodel _previewViewmodel;
        private readonly EventEditViewmodel _eventEditViewmodel;
        private readonly VideoFormatDescription _videoFormatDescription;
        private readonly ICGElementsController _cGElementsController;
        private readonly EngineCGElementsControllerViewmodel _cGElementsControllerViewmodel;
        private readonly bool _allowPlayControl;

        public IEngine Engine { get { return _engine; } }
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
        #region Single selected commands
        public ICommand CommandEventHide { get; private set; }
        public ICommand CommandAddNextMovie { get; private set; }
        public ICommand CommandAddNextEmptyMovie { get; private set; }
        public ICommand CommandAddNextRundown { get; private set; }
        public ICommand CommandAddNextLive { get; private set; }
        public ICommand CommandAddSubMovie { get; private set; }
        public ICommand CommandAddSubRundown { get; private set; }
        public ICommand CommandAddSubLive { get; private set; }
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

        public EngineViewmodel(IEngine engine, IPreview preview, bool allowPlayControl)
        {
            Debug.WriteLine($"Creating EngineViewmodel for {engine}");
            _engine = engine;
            _videoFormat = engine.VideoFormat;
            _videoFormatDescription = engine.FormatDescription;
            _allowPlayControl = allowPlayControl;

            // Creating root EventViewmodel
            _rootEventViewModel = new EventPanelRootViewmodel(this);
            _engine.EngineTick += _engineTick;
            _engine.EngineOperation += _engineOperation;
            _engine.PropertyChanged += _enginePropertyChanged;
            _engine.VisibleEventsOperation += _onEngineVisibleEventsOperation;
            _engine.RunningEventsOperation += OnEngineRunningEventsOperation;
            _composePlugins();


            // Creating PreviewViewmodel
            if (preview != null && allowPlayControl)
                _previewViewmodel = new PreviewViewmodel(preview) { IsSegmentsVisible = true };
            
            // Creating EventEditViewmodel
            _eventEditViewmodel = new EventEditViewmodel(this, _previewViewmodel);

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
            _cGElementsController = engine.CGElementsController;
            if (_cGElementsController != null)
            {
                _cGElementsController.PropertyChanged += _cGElementsController_PropertyChanged;
                _cGElementsControllerViewmodel = new EngineCGElementsControllerViewmodel(engine.CGElementsController);
            }
        }

        private void _cGElementsController_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ICGElementsController.IsMaster))
                NotifyPropertyChanged(nameof(CGControllerIsMaster));
        }

        protected override void OnDispose()
        {
            _engine.EngineTick -= _engineTick;
            _engine.EngineOperation -= _engineOperation;
            _engine.PropertyChanged -= _enginePropertyChanged;
            _engine.VisibleEventsOperation -= _onEngineVisibleEventsOperation;
            _engine.RunningEventsOperation -= OnEngineRunningEventsOperation;

            _multiSelectedEvents.CollectionChanged -= _selectedEvents_CollectionChanged;
            EventClipboard.ClipboardChanged -= _engineViewmodel_ClipboardChanged;
            if (_engine.PlayoutChannelPRI != null)
                _engine.PlayoutChannelPRI.PropertyChanged -= OnServerChannelPropertyChanged;
            if (_engine.PlayoutChannelSEC != null)
                _engine.PlayoutChannelSEC.PropertyChanged -= OnServerChannelPropertyChanged;
            if (_engine.PlayoutChannelPRV != null)
                _engine.PlayoutChannelPRV.PropertyChanged -= OnServerChannelPropertyChanged;
            if (_videoPreview != null)
                _videoPreview.Dispose();
        }

        void _engineViewmodel_ClipboardChanged()
        {
            InvalidateRequerySuggested();
        }

        public PreviewViewmodel PreviewViewmodel { get { return _previewViewmodel; } }
        public EngineCGElementsControllerViewmodel CGElementsControllerViewmodel { get { return _cGElementsControllerViewmodel; } }

        #region Commands

        private void _createCommands()
        {
            CommandClearAll = new UICommand() { ExecuteDelegate = o => _engine.Clear(), CanExecuteDelegate = _canClear };
            CommandClearLayer = new UICommand() { ExecuteDelegate = layer => _engine.Clear((VideoLayer)int.Parse((string)layer)), CanExecuteDelegate = _canClear };
            CommandClearMixer = new UICommand() { ExecuteDelegate = o => _engine.ClearMixer(), CanExecuteDelegate = _canClear };
            CommandRestart = new UICommand() { ExecuteDelegate = ev => _engine.Restart(), CanExecuteDelegate = _canClear };
            CommandStartSelected = new UICommand() { ExecuteDelegate = _startSelected, CanExecuteDelegate = _canStartSelected };
            CommandLoadSelected = new UICommand() { ExecuteDelegate = _loadSelected, CanExecuteDelegate = _canLoadSelected };
            CommandScheduleSelected = new UICommand() { ExecuteDelegate = o => _engine.Schedule(_selected.Event), CanExecuteDelegate = _canScheduleSelected };
            CommandRescheduleSelected = new UICommand() { ExecuteDelegate = o => _engine.ReSchedule(_selected.Event), CanExecuteDelegate = _canRescheduleSelected };
            CommandForceNextSelected = new UICommand() { ExecuteDelegate = _forceNext, CanExecuteDelegate = _canForceNextSelected };
            CommandTrackingToggle = new UICommand() { ExecuteDelegate = o => TrackPlayingEvent = !TrackPlayingEvent };
            CommandDebugToggle = new UICommand() { ExecuteDelegate = _debugShow };
            CommandRestartRundown = new UICommand() { ExecuteDelegate = _restartRundown, CanExecuteDelegate = _canClear };
            CommandRestartLayer = new UICommand { ExecuteDelegate = _restartLayer, CanExecuteDelegate = o => IsPlayingMovie && _allowPlayControl };
            CommandNewRootRundown = new UICommand() { ExecuteDelegate = _addNewRootRundown };
            CommandNewContainer = new UICommand() { ExecuteDelegate = _newContainer };
            CommandSearchMissingEvents = new UICommand() { ExecuteDelegate = _searchMissingEvents };
            CommandStartLoaded = new UICommand() { ExecuteDelegate = o => _engine.StartLoaded(), CanExecuteDelegate = o => _engine.EngineState == TEngineState.Hold };
            CommandDeleteSelected = new UICommand() { ExecuteDelegate = _deleteSelected, CanExecuteDelegate = o => _multiSelectedEvents.Count > 0 };
            CommandCopySelected = new UICommand() { ExecuteDelegate = _copySelected, CanExecuteDelegate = o => _multiSelectedEvents.Count > 0 };
            CommandCutSelected = new UICommand() { ExecuteDelegate = _cutSelected, CanExecuteDelegate = o => _multiSelectedEvents.Count > 0  };
            CommandPasteSelected = new UICommand() { ExecuteDelegate = _pasteSelected, CanExecuteDelegate = o => EventClipboard.CanPaste(_selected, (EventClipboard.TPasteLocation)Enum.Parse(typeof(EventClipboard.TPasteLocation), o.ToString(), true)) };
            CommandExportMedia = new UICommand() { ExecuteDelegate = _exportMedia, CanExecuteDelegate = _canExportMedia };
            CommandUndelete = new UICommand() { ExecuteDelegate = _undelete, CanExecuteDelegate = _canUndelete };

            CommandEventHide = new UICommand { ExecuteDelegate = _eventHide };
            CommandMoveUp = EventEditViewmodel.CommandMoveUp;
            CommandMoveDown = EventEditViewmodel.CommandMoveDown;
            CommandAddNextMovie = new UICommand { ExecuteDelegate = _addNextMovie, CanExecuteDelegate = _canAddNextMovie  };
            CommandAddNextEmptyMovie = new UICommand { ExecuteDelegate = _addNextEmptyMovie, CanExecuteDelegate = _canAddNextEmptyMovie };
            CommandAddNextRundown = new UICommand { ExecuteDelegate = _addNextRundown, CanExecuteDelegate = _canAddNextRundown };
            CommandAddNextLive = new UICommand { ExecuteDelegate = _addNextLive, CanExecuteDelegate = _canAddNextLive };
            CommandAddSubMovie = new UICommand { ExecuteDelegate = _addSubMovie, CanExecuteDelegate = _canAddSubMovie };
            CommandAddSubRundown = new UICommand { ExecuteDelegate = _addSubRundown, CanExecuteDelegate = _canAddSubRundown };
            CommandAddSubLive = new UICommand { ExecuteDelegate = _addSubLive, CanExecuteDelegate = _canAddSubLive };
            CommandToggleLayer = new UICommand { ExecuteDelegate = _toggleLayer };
            CommandToggleEnabled = new UICommand { ExecuteDelegate = _toggleEnabled };
            CommandToggleHold = new UICommand { ExecuteDelegate = _toggleHold };

            CommandSearchDo = new UICommand { ExecuteDelegate = _search, CanExecuteDelegate = _canSearch };
            CommandSearchShowPanel = new UICommand { ExecuteDelegate = _showSearchPanel };
            CommandSearchHidePanel = new UICommand { ExecuteDelegate = _hideSearchPanel };

            CommandSaveEdit = new UICommand { ExecuteDelegate = _eventEditViewmodel.CommandSaveEdit.Execute };
            CommandUndoEdit = new UICommand { ExecuteDelegate = _eventEditViewmodel.CommandUndoEdit.Execute };

            CommandSaveRundown = new UICommand { ExecuteDelegate = _saveRundown, CanExecuteDelegate = o => Selected != null && Selected.Event.EventType == TEventType.Rundown };
            CommandLoadRundown = new UICommand { ExecuteDelegate = _loadRundown, CanExecuteDelegate = o => o.Equals("Under") ? _canAddSubRundown(o) : _canAddNextRundown(o) };

        }

        private bool _canUndelete(object obj)
        {
            return EventClipboard.CanUndo();
        }

        private void _undelete(object obj)
        {
            if (MessageBox.Show(string.Format(resources._query_Undelete), resources._caption_Confirmation, MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                EventClipboard.Undo();
        }

        private bool _canClear(object obj)
        {
            return _allowPlayControl;
        }

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
                using (var reader = System.IO.File.OpenText(dlg.FileName))
                using (var jreader = new Newtonsoft.Json.JsonTextReader(reader))
                {
                    var proxy = (new Newtonsoft.Json.JsonSerializer() { DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Populate })
                        .Deserialize<EventProxy>(jreader);
                    if (proxy != null)
                    {
                        var mediaFiles = (_engine.MediaManager.MediaDirectoryPRI ?? _engine.MediaManager.MediaDirectorySEC)?.GetFiles();
                        var animationFiles = (_engine.MediaManager.AnimationDirectoryPRI ?? _engine.MediaManager.AnimationDirectorySEC)?.GetFiles();
                        var newEvent = obj.Equals("Under") ? proxy.InsertUnder(Selected.Event, false, mediaFiles, animationFiles) : proxy.InsertAfter(Selected.Event, mediaFiles, animationFiles);
                        LastAddedEvent = newEvent;
                    }

                }
            }
        }

        private void _saveRundown(object obj)
        {
            EventProxy proxy = EventProxy.FromEvent(Selected.Event);
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog()
            {
                FileName = proxy.EventName,
                DefaultExt = FileUtils.RundownFileExtension,
                Filter = string.Format("{0}|*{1}|{2}|*.*", resources._rundowns, FileUtils.RundownFileExtension, resources._allFiles)
            };
            if (dlg.ShowDialog() == true)
            {
                using (var writer = System.IO.File.CreateText(dlg.FileName))
                    new Newtonsoft.Json.JsonSerializer() { Formatting = Newtonsoft.Json.Formatting.Indented, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore, DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore, TypeNameHandling=Newtonsoft.Json.TypeNameHandling.Auto }
                    .Serialize(writer, proxy);
            }
        }

        private bool _canAddSubLive(object obj)
        {
            var ep = Selected as EventPanelRundownViewmodel;
            return ep != null && ep.CommandAddSubLive.CanExecute(obj);
        }

        private bool _canAddSubRundown(object obj)
        {
            var ep = Selected as EventPanelRundownViewmodel;
            if (ep != null)
                return ep.CommandAddSubRundown.CanExecute(obj);
            var ec = Selected as EventPanelContainerViewmodel;
            return ec != null && ec.CommandAddSubRundown.CanExecute(obj);
        }

        private bool _canAddSubMovie(object obj)
        {
            var ep = Selected as EventPanelRundownViewmodel;
            return ep != null && ep.CommandAddSubMovie.CanExecute(obj);
        }

        private bool _canAddNextLive(object obj)
        {
            var ep = Selected as EventPanelRundownElementViewmodelBase;
            return ep != null && ep.CommandAddNextLive.CanExecute(obj);
        }

        private bool _canAddNextRundown(object obj)
        {
            var ep = Selected as EventPanelRundownElementViewmodelBase;
            return ep != null && ep.CommandAddNextRundown.CanExecute(obj);
        }

        private bool _canAddNextEmptyMovie(object obj)
        {
            var ep = Selected as EventPanelRundownElementViewmodelBase;
            return ep != null && ep.CommandAddNextEmptyMovie.CanExecute(obj);
        }

        private bool _canAddNextMovie(object obj)
        {
            var ep = Selected as EventPanelRundownElementViewmodelBase;
            return ep != null && ep.CommandAddNextMovie.CanExecute(obj);
        }

        private void _toggleHold(object obj)
        {
            var ep = Selected as EventPanelRundownElementViewmodelBase;
            if (ep != null)
                ep.CommandToggleHold.Execute(obj);
        }

        private void _toggleEnabled(object obj)
        {
            var ep = Selected as EventPanelRundownElementViewmodelBase;
            if (ep != null)
                ep.CommandToggleEnabled.Execute(obj);
        }

        private void _forceNext(object obj)
        {
            if (IsForcedNext)
                _engine.ForcedNext = null;
            else
                _engine.ForcedNext = _selected.Event;
        }

        private bool _canForceNextSelected(object obj)
        {
            return _engine.EngineState == TEngineState.Running && (_canLoadSelected(obj) || IsForcedNext);
        }

        private void _toggleLayer(object obj)
        {
            var ep = Selected as EventPanelRundownElementViewmodelBase;
            if (ep != null)
                ep.CommandToggleLayer.Execute(obj);
        }

        private void _addSubLive(object obj)
        {
            var ep = Selected as EventPanelRundownViewmodel;
            if (ep != null)
                ep.CommandAddSubLive.Execute(obj);
        }

        private void _addSubRundown(object obj)
        {
            var ep = Selected as EventPanelRundownViewmodel;
            if (ep != null)
                ep.CommandAddSubRundown.Execute(obj);
            var ec = Selected as EventPanelContainerViewmodel;
            if (ec != null)
                ec.CommandAddSubRundown.Execute(obj);
        }

        private void _addSubMovie(object obj)
        {
            var ep = Selected as EventPanelRundownViewmodel;
            if (ep != null)
                ep.CommandAddSubMovie.Execute(obj);
        }

        private void _addNextLive(object obj)
        {
            var ep = Selected as EventPanelRundownElementViewmodelBase;
            if (ep != null)
                ep.CommandAddNextLive.Execute(obj);
        }

        private void _addNextRundown(object obj)
        {
            var ep = Selected as EventPanelRundownElementViewmodelBase;
            if (ep != null)
                ep.CommandAddNextRundown.Execute(obj);
        }

        private void _addNextEmptyMovie(object obj)
        {
            var ep = Selected as EventPanelRundownElementViewmodelBase;
            if (ep != null)
                ep.CommandAddNextEmptyMovie.Execute(obj);
        }

        private void _addNextMovie(object obj)
        {
            var ep = Selected as EventPanelRundownElementViewmodelBase;
            if (ep != null)
                ep.CommandAddNextMovie.Execute(obj);
        }

        private void _eventHide(object obj)
        {
            var ep = Selected as EventPanelContainerViewmodel;
            if (ep != null)
                ep.CommandHide.Execute(obj);
        }
    
        private void _pasteSelected(object obj)
        {
            LastAddedEvent = EventClipboard.Paste(_selected, (EventClipboard.TPasteLocation)Enum.Parse(typeof(EventClipboard.TPasteLocation), (string)obj, true));
        }

        private void _copySelected(object obj)
        {
            EventClipboard.Copy(_multiSelectedEvents);
        }

        private void _cutSelected(object obj)
        {
            EventClipboard.Cut(_multiSelectedEvents);
        }
        
        private bool _canExportMedia(object obj)
        {
            bool exportAll = obj != null;
            return _multiSelectedEvents.Any(e =>
                    {
                        var m = e as EventPanelMovieViewmodel;
                        return m != null
                            && (m.IsEnabled || exportAll)
                            && m.Media?.FileExists() == true;
                    })
                    && _engine.MediaManager.IngestDirectories.Any(d => d.IsExport);
        }

        private void _exportMedia(object obj)
        {
            bool exportAll = obj != null;
            var selections = _multiSelectedEvents.Where(e =>
            {
                var m = e as EventPanelMovieViewmodel;
                return m != null
                    && (m.IsEnabled || exportAll)
                    && m.Media?.FileExists() == true;
            }).Select(e => new ExportMedia(
                e.Event.Media, 
                e.Event.SubEvents.Where(sev => sev.EventType == TEventType.StillImage && sev.Media != null).Select(sev => sev.Media).ToList(),
                e.Event.ScheduledTc, 
                e.Event.Duration, 
                e.Event.GetAudioVolume()));
            using (ExportViewmodel evm = new ExportViewmodel(_engine.MediaManager, selections)) { }
        }

        private void _startSelected(object obj)
        {
            var eventToStart = Selected.Event;
            if (_engine.EngineState != TEngineState.Running
                || _engine.Playing?.EventType == TEventType.Live 
                || (string)obj == "Force"
                || MessageBox.Show(string.Format(resources._query_PlayWhileRunning), resources._caption_Confirmation, MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK
                )
                _engine.Start(eventToStart);
        }

        private bool _canStartSelected(object o)
        {
            IEvent ev = _selected?.Event;
            return _allowPlayControl
                && ev != null
                && ev.IsEnabled
                && (ev.PlayState == TPlayState.Scheduled || ev.PlayState == TPlayState.Paused || ev.PlayState == TPlayState.Aborted)
                && (ev.EventType == TEventType.Rundown || ev.EventType == TEventType.Live || ev.EventType == TEventType.Movie);
        }

        private void _loadSelected(object obj)
        {
            var eventToLoad = Selected.Event;
            if (_engine.EngineState != TEngineState.Running
                || _engine.Playing?.EventType == TEventType.Live
                || MessageBox.Show(string.Format(resources._query_LoadWhileRunning), resources._caption_Confirmation, MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK
                )
                _engine.Load(eventToLoad);
        }

        private bool _canLoadSelected(object o)
        {
            IEvent ev = _selected?.Event;
            return
                _allowPlayControl
                && ev != null
                && ev.IsEnabled
                && (ev.PlayState == TPlayState.Scheduled || ev.PlayState == TPlayState.Aborted)
                && (ev.EventType == TEventType.Rundown || ev.EventType == TEventType.Live || ev.EventType == TEventType.Movie);
        }

        private bool _canScheduleSelected(object o)
        {
            IEvent ev = _selected?.Event;
            return _allowPlayControl && ev != null && (ev.PlayState == TPlayState.Scheduled || ev.PlayState == TPlayState.Paused) && ev.ScheduledTime >= _currentTime;
        }

        private bool _canRescheduleSelected(object o)
        {
            IEvent ev = _selected?.Event;
            return ev != null && (ev.PlayState == TPlayState.Aborted || ev.PlayState == TPlayState.Played);
        }

        private bool _canCut(object o)
        {
            IEvent ev = _selected?.Event;
            return ev != null
                && (ev.EventType == TEventType.Rundown || ev.EventType == TEventType.Movie || ev.EventType == TEventType.Live)
                && ev.PlayState == TPlayState.Scheduled;
        }

        private bool _canCopySingle(object o)
        {
            IEvent ev = _selected?.Event;
            return ev != null
                && (ev.EventType == TEventType.Rundown || ev.EventType == TEventType.Movie || ev.EventType == TEventType.Live);
        }

        private void _restartRundown(object o)
        {
            if (!_allowPlayControl)
                return;
            IEvent ev = _selected?.Event;
            if (ev != null)
                _engine.RestartRundown(ev);
        }

        private void _restartLayer(object obj)
        {
            if (!_allowPlayControl)
                return;
            _engine.Restart();
        }

        private void _addNewRootRundown(object o)
        {
            IEvent newEvent = _engine.AddNewEvent(
                eventType: TEventType.Rundown,
                eventName: resources._title_NewRundown,
                startType: TStartType.Manual,
                scheduledTime: _currentTime);
            _engine.AddRootEvent(newEvent);
            newEvent.Save();
            LastAddedEvent = newEvent;
        }

        private void _newContainer(object o)
        {
            IEvent newEvent = _engine.AddNewEvent(
                eventType: TEventType.Container,
                eventName: resources._title_NewContainer);
            _engine.AddRootEvent(newEvent);
            newEvent.Save();
            LastAddedEvent = newEvent;
        }

        private void _deleteSelected(object ob)
        {
            var evmList = _multiSelectedEvents.ToList();
            var containerList = evmList.Where(evm => evm is EventPanelContainerViewmodel);
            if (evmList.Count() > 0
                && MessageBox.Show(string.Format(resources._query_DeleteSelected, evmList.Count(), evmList.AsString(Environment.NewLine, 20)), resources._caption_Confirmation, MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK
                && (containerList.Count() == 0
                    || MessageBox.Show(string.Format(resources._query_DeleteSelectedContainers, containerList.Count(), containerList.AsString(Environment.NewLine, 20)), resources._caption_Confirmation, MessageBoxButton.OKCancel) == MessageBoxResult.OK))
            {
                var firstEvent = evmList.First().Event;
                EventClipboard.SaveUndo(evmList.Select(evm => evm.Event), firstEvent.StartType == TStartType.After ? firstEvent.Prior : firstEvent.Parent);
                ThreadPool.QueueUserWorkItem(
                    o =>
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
                            Application.Current.Dispatcher.BeginInvoke((Action)delegate ()
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
        private MediaSearchViewmodel _mediaSearchViewModel;
        public void AddMediaEvent(IEvent baseEvent, TStartType startType, TMediaType mediaType, VideoLayer layer, bool closeAfterAdd)
        {
            if (baseEvent != null && _mediaSearchViewModel == null)
            {
                _mediaSearchViewModel = new MediaSearchViewmodel(_engine, _engine.MediaManager, mediaType, layer, closeAfterAdd, baseEvent.Media?.FormatDescription());
                _mediaSearchViewModel.BaseEvent = baseEvent;
                _mediaSearchViewModel.NewEventStartType = startType;
                _mediaSearchViewModel.SearchWindowClosed += _mediaSearchViewModelSearchWindowClosed;
                _mediaSearchViewModel.MediaChoosen += _mediaSearchViewModelMediaChoosen;
            }
        }

        private void _mediaSearchViewModelSearchWindowClosed(object o, EventArgs e)
        {
            MediaSearchViewmodel mvs = (MediaSearchViewmodel)o;
            mvs.SearchWindowClosed -= _mediaSearchViewModelSearchWindowClosed;
            mvs.MediaChoosen -= _mediaSearchViewModelMediaChoosen;
            _mediaSearchViewModel.Dispose();
            _mediaSearchViewModel = null;
        }

        private void _mediaSearchViewModelMediaChoosen(object o, MediaSearchEventArgs e)
        {
            MediaSearchViewmodel mediaSearchVm = o as MediaSearchViewmodel;
            if (e.Media != null && mediaSearchVm != null)
            {
                IEvent newEvent;
                switch (e.Media.MediaType)
                {
                    case TMediaType.Movie:
                        TMediaCategory category = e.Media.MediaCategory;
                        var cgController = Engine.CGElementsController;
                        var defaultCrawl = cgController == null ? 0 : cgController.DefaultCrawl;
                        newEvent = _engine.AddNewEvent(
                            eventName: e.MediaName,
                            videoLayer: VideoLayer.Program,
                            eventType: TEventType.Movie,
                            scheduledTC: e.TCIn,
                            duration: e.Duration,
                            isCGEnabled: _engine.EnableCGElementsForNewEvents,
                            crawl: (byte)((
                                Engine.CrawlEnableBehavior == TCrawlEnableBehavior.ShowsOnly && category == TMediaCategory.Show)
                                || (Engine.CrawlEnableBehavior == TCrawlEnableBehavior.AllButCommercials && (category == TMediaCategory.Show || category == TMediaCategory.Promo || category == TMediaCategory.Fill || category == TMediaCategory.Insert || category == TMediaCategory.Uncategorized))
                                    ? defaultCrawl : 0),
                            logo: (byte)(category == TMediaCategory.Fill || category == TMediaCategory.Show || category == TMediaCategory.Promo || category == TMediaCategory.Insert || category == TMediaCategory.Jingle ? 1 : 0),
                            parental: e.Media.Parental
                            );
                        break;
                    case TMediaType.Still:
                        newEvent = _engine.AddNewEvent(
                            eventName: e.MediaName,
                            eventType: TEventType.StillImage,
                            videoLayer: mediaSearchVm.Layer,
                            duration: mediaSearchVm.BaseEvent.Duration);
                        break;
                    case TMediaType.Animation:
                        newEvent = _engine.AddNewEvent(
                            eventName: e.MediaName,
                            eventType: TEventType.Animation,
                            videoLayer: VideoLayer.Animation);
                        if (newEvent is ITemplated && e.Media is ITemplated)
                            ((ITemplated)newEvent).Fields = ((ITemplated)e.Media).Fields;
                        break;
                    default:
                        throw new ApplicationException("Invalid MediaType choosen");

                }
                newEvent.Media = e.Media;
                if (mediaSearchVm.NewEventStartType == TStartType.After)
                    mediaSearchVm.BaseEvent.InsertAfter(newEvent);
                if (mediaSearchVm.NewEventStartType == TStartType.WithParent)
                    mediaSearchVm.BaseEvent.InsertUnder(newEvent, false);
                mediaSearchVm.NewEventStartType = TStartType.After;
                mediaSearchVm.BaseEvent = newEvent;
                LastAddedEvent = newEvent;
            }
        }
        
        public void AddCommandScriptEvent(IEvent baseEvent)
        {
            var newEvent = Engine.AddNewEvent(eventType: TEventType.CommandScript, duration:baseEvent.Duration, eventName:resources._title_NewCommandScript);
            baseEvent.InsertUnder(newEvent, false);
            LastAddedEvent = newEvent;
        }

        public void AddSimpleEvent(IEvent baseEvent, TEventType eventType, bool insertUnder)
        {
            IEvent newEvent = null;
            switch (eventType)
            {
                case TEventType.Live:
                    newEvent = Engine.AddNewEvent(
                        eventType: TEventType.Live,
                        eventName: resources._title_NewLive,
                        videoLayer: VideoLayer.Program,
                        duration: new TimeSpan(0, 10, 0));
                    break;
                case TEventType.Movie:
                    newEvent = Engine.AddNewEvent(
                        eventType: TEventType.Movie,
                        eventName: resources._title_EmptyMovie,
                        videoLayer: VideoLayer.Program);
                    break;
                case TEventType.Rundown:
                    newEvent = Engine.AddNewEvent(
                        scheduledTime: DateTime.Today.AddDays(1) + new TimeSpan(DateTime.Now.Hour, 0, 0),
                        eventType: TEventType.Rundown,
                        eventName: resources._title_NewRundown);
                    break;
            }
            if (newEvent != null)
            {
                if (insertUnder == true)
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
        public bool IsSearchPanelVisible { get { return _isSearchPanelVisible; }  set { SetField(ref _isSearchPanelVisible, value); } }
        private void _showSearchPanel(object obj)
        {
            IsSearchNotFound = false;
            IsSearchPanelVisible = true;
            IsSearchBoxFocused = true;
        }

        private void _hideSearchPanel(object obj)
        {
            IsSearchPanelVisible = false;
            Selected?.Focus();
        }

        private bool _isSearchBoxFocused;
        public bool IsSearchBoxFocused { get { return _isSearchBoxFocused; } set { SetField(ref _isSearchBoxFocused, value); } }

        private bool _isSearchNotFound;
        public bool IsSearchNotFound { get { return _isSearchNotFound; }  set { SetField(ref _isSearchNotFound, value); } }

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
            IEvent current = _selected?.Event;
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
                            Selected = cl;
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


        public bool IsInterlacedFormat { get { return _videoFormatDescription.Interlaced; } }

        readonly EventPanelViewmodelBase _rootEventViewModel;
        public EventPanelViewmodelBase RootEventViewModel { get { return _rootEventViewModel; } }

        private EventPanelViewmodelBase _selected;
        public EventPanelViewmodelBase Selected
        {
            get { return _selected; }
            set
            {
                if (value != _selected)
                {
                    IEvent oldSelectedEvent = _selected == null ? null : _selected.Event;
                    if (oldSelectedEvent != null)
                    {
                        oldSelectedEvent.PropertyChanged -= _onSelectedEventPropertyChanged;
                    }
                    _selected = value;
                    IEvent newSelected = value == null ? null : value.Event;
                    if (newSelected != null)
                    {
                        newSelected.PropertyChanged += _onSelectedEventPropertyChanged;
                        oldSelectedEvent = value.Event;
                    }
                    if (_previewViewmodel != null)
                        _previewViewmodel.Event = newSelected;
                    _eventEditViewmodel.Event = newSelected;
                    var re = value as EventPanelRundownElementViewmodelBase;
                    if (re != null && _mediaSearchViewModel != null)
                    {
                        _mediaSearchViewModel.BaseEvent = re.Event;
                        _mediaSearchViewModel.NewEventStartType = TStartType.After;
                    }
                    InvalidateRequerySuggested();
                    _updatePluginCanExecute();
                    IsSearchNotFound = false;
                }
            }
        }

        public EventEditViewmodel EventEditViewmodel { get { return _eventEditViewmodel; } }

        private EventPanelViewmodelBase _GetEventViewModel(IEvent aEvent)
        {
            IEnumerable<IEvent> rt = aEvent.GetVisualRootTrack().Reverse();
            EventPanelViewmodelBase evm = _rootEventViewModel;
            foreach (IEvent ev in rt)
            {
                if (evm != null)
                {
                    evm = evm.Childrens.FirstOrDefault(e => e.Event == ev);
                    if (evm!= null && evm.Event == aEvent)
                        return evm;
                }
            }
            return null;
        }

        public bool Pst2Prv
        {
            get { return _engine.Pst2Prv; }
            set { _engine.Pst2Prv = value; }
        }

        private DateTime _currentTime;
        public DateTime CurrentTime
        {
            get { return _currentTime; }
            private set { SetField(ref _currentTime, value); }
        }

        private TVideoFormat _videoFormat;
        public TVideoFormat VideoFormat { get { return _videoFormat; } }

        private TimeSpan _timeToAttention;
        public TimeSpan TimeToAttention
        {
            get { return _timeToAttention; }
            set { SetField(ref _timeToAttention, value); }
        }

        public string EngineName { get { return _engine.EngineName; } }

        public bool AllowPlayControl { get { return _allowPlayControl; } }

        public bool IsPlayingMovie
        {
            get
            {
                var aEvent = _engine.Playing;
                return aEvent != null && aEvent.Layer == VideoLayer.Program && aEvent.EventType == TEventType.Movie;
            }
        }         

        public bool IsAnimationDirAvailable
        {
            get { return _engine.MediaManager.AnimationDirectoryPRI != null || _engine.MediaManager.AnimationDirectorySEC != null;  }
        }

        public bool IsPreviewPanelVisible
        {
            get { return PreviewViewmodel != null || VideoPreview != null; }
        }

        public bool NoAlarms
        {
            get
            {
                return (ServerConnectedPRI || !ServerPRIExists)
                       && (ServerConnectedSEC || !ServerSECExists)
                       && (ServerConnectedPRV || !ServerPRVExists)
                       && DatabaseOK;
            }
        }

        public bool ServerPRIExists
        {
            get { return _engine?.PlayoutChannelPRI != null; }
        }

        public bool ServerConnectedPRI
        {
            get { return _engine?.PlayoutChannelPRI?.IsServerConnected == true; }
        }
        public bool ServerSECExists
        {
            get { return _engine?.PlayoutChannelSEC != null; }
        }
        public bool ServerConnectedSEC
        {
            get { return _engine?.PlayoutChannelSEC?.IsServerConnected == true; }
        }
        public bool ServerPRVExists
        {
            get { return _engine?.PlayoutChannelPRV != null; }
        }
        public bool ServerConnectedPRV
        {
            get { return _engine?.PlayoutChannelPRV?.IsServerConnected == true; }
        }

        public bool DatabaseOK
        {
            get {
                var state = _engine.DatabaseConnectionState;
                return (state & ConnectionStateRedundant.Open) > 0 
                    && (state & (ConnectionStateRedundant.BrokenPrimary | ConnectionStateRedundant.BrokenSecondary | ConnectionStateRedundant.Desynchronized)) == 0; } 
        }

        public string PlayingEventName
        {
            get
            {
                var e = _engine.Playing;
                return e == null ? string.Empty : e.EventName;
            }
        }

        public string NextToPlay
        {
            get
            {
                var e = _engine.NextToPlay;
                return e == null ? string.Empty : e.EventName;
            }
        }

        public IEvent NextWithRequestedStartTime
        {
            get { return _engine.NextWithRequestedStartTime; }
        }

        public int SelectedCount
        {
            get { return _multiSelectedEvents.Count; }
        }

        public TimeSpan SelectedTime
        {
            get { return TimeSpan.FromTicks(_multiSelectedEvents.Sum(e => e.Event.Duration.Ticks)); }
        }

        public bool IsForcedNext
        {
            get { return _engine.ForcedNext != null; }
        }

        private int _audioVolumePGM = -100;
        public int AudioLevelPRI 
        {
            get { return _audioVolumePGM; }
            private set { SetField(ref _audioVolumePGM, value); }
        }

        #region Plugin
        CompositionContainer _uiContainer;

#pragma warning disable CS0649 
        [ImportMany]
        IUiPlugin[] _plugins = null;
        [Import(AllowDefault = true)]
        IVideoPreview _videoPreview = null;
#pragma warning restore

        public IList<Common.Plugin.IUiPlugin> Plugins { get { return _plugins; } }


        public IVideoPreview VideoPreview { get { return _videoPreview; } }

        public bool IsAnyPluginActive { get { return _plugins != null && _plugins.Length > 0; } }

        private void _composePlugins()
        {
            try
            {
                var pluginPath = Path.Combine(Directory.GetCurrentDirectory(), "Plugins");
                if (Directory.Exists(pluginPath))
                {
                    DirectoryCatalog catalog = new DirectoryCatalog(pluginPath);
                    _uiContainer = new CompositionContainer(catalog);
                    _uiContainer.ComposeExportedValue<Func<PluginExecuteContext>>(_getPluginContext);
                    _uiContainer.SatisfyImportsOnce(this);
                }
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
            return new PluginExecuteContext { Engine = _engine, Event = _selected == null ? null : _selected.Event };
        }

        #endregion // Plugin
        public bool CGControllerExists
        {
            get { return _engine.CGElementsController != null; }
        }

        public bool CGControllerIsMaster
        {
            get { return _engine.CGElementsController?.IsMaster == true; }
        }

        public TEngineState EngineState { get { return _engine.EngineState; } }

        private readonly ObservableCollection<IEvent> _visibleEvents = new ObservableCollection<IEvent>();
        public IEnumerable<IEvent> VisibleEvents { get { return _visibleEvents; } }
        private readonly ObservableCollection<IEvent> _runningEvents = new ObservableCollection<IEvent>();
        public IEnumerable<IEvent> RunningEvents { get { return _runningEvents; } }

        private readonly ObservableCollection<EventPanelViewmodelBase> _multiSelectedEvents;
        public IEnumerable<EventPanelViewmodelBase> MultiSelectedEvents { get { return _multiSelectedEvents; } }

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


        private Views.EngineDebugView _debugWindow;
        public void _searchMissingEvents(object o)
        {
            _engine.SearchMissingEvents();
        }

        private void _onSelectedEventPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var selected = _selected;
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
                        Application.Current.Dispatcher.BeginInvoke((Action)delegate()
                        {
                            var pe = _engine.Playing;
                            if (pe != null)
                                SetOnTopView(pe);
                        }, null);
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
                && _selected != null
                && a.Event == _selected.Event)
                InvalidateRequerySuggested();
        }

        private void SetOnTopView(IEvent pe)
        {
            var evm = _GetEventViewModel(pe);
            if (evm != null)
                evm.SetOnTop();
        }

        public void _engineTick(object sender, EngineTickEventArgs e)
        {
            CurrentTime = e.CurrentTime.ToLocalTime();
            TimeToAttention = e.TimeToAttention;
        }

        public void OnServerChannelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IPlayoutServerChannel.IsServerConnected))
            {
                NotifyPropertyChanged(nameof(ServerConnectedPRI));
                NotifyPropertyChanged(nameof(NoAlarms));
            }
            if (sender == _engine.PlayoutChannelPRI && e.PropertyName == nameof(IPlayoutServerChannel.AudioLevel))
                AudioLevelPRI = ((IPlayoutServerChannel)sender).AudioLevel;
        }

        public decimal ProgramAudioVolume //decibels
        {
            get { return (decimal)(20 * Math.Log10((double)_engine.ProgramAudioVolume)); }
            set
            {
                decimal volume = (decimal)Math.Pow(10, (double)value / 20);
                if (value != volume)
                    _engine.ProgramAudioVolume = volume;
            }
        }

        public bool FieldOrderInverted
        {
            get { return _engine.FieldOrderInverted; }
            set { _engine.FieldOrderInverted = value; }
        }

        private void _onEngineVisibleEventsOperation(object o, CollectionOperationEventArgs<IEvent> e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate()
            {
                if (e.Operation == TCollectionOperation.Insert)
                    _visibleEvents.Add(e.Item);
                else
                    _visibleEvents.Remove(e.Item);
            });
        }

        private void OnEngineRunningEventsOperation(object o, CollectionOperationEventArgs<IEvent> e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate()
            {
                if (e.Operation == TCollectionOperation.Insert)
                    _runningEvents.Add(e.Item);
                else
                    _runningEvents.Remove(e.Item);
            });

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

        private bool _trackPlayingEvent = true;
        public bool TrackPlayingEvent
        {
            get { return _trackPlayingEvent; }
            set
            {
                if (SetField(ref _trackPlayingEvent, value))
                    if (value)
                    {
                        IEvent cp = _engine.Playing;
                        if (cp != null)
                            SetOnTopView(cp);
                    }
            }
        }

        public override string ToString()
        {
            return resources._rundown;
        }

    }
}
