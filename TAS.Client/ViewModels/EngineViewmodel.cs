using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Windows;
using System.Runtime.Remoting.Messaging;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Diagnostics;
using TAS.Common;
using TAS.Client.Common;
using System.Configuration;
using TAS.Server.Interfaces;
using TAS.Server.Common;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition;
using TAS.Client.Common.Plugin;
using resources = TAS.Client.Common.Properties.Resources;
using System.IO;

namespace TAS.Client.ViewModels
{
    public class EngineViewmodel : ViewmodelBase
    {
        private readonly IEngine _engine;
        private readonly EngineView _engineView;
        private readonly PreviewViewmodel _previewViewmodel;
        private readonly Views.PreviewView _previewView;
        private readonly EventEditViewmodel _eventEditViewmodel;
        private readonly EventEditView _eventEditView;

        public IEngine Engine { get { return _engine; } }
        public ICommand CommandClearAll { get; private set; }
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
        public ICommand CommandSaveRundown { get; private set; }
        public ICommand CommandLoadRundown { get; private set; }

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

        public EngineViewmodel(IEngine engine, IPreview preview)
        {
            _engine = engine;
            _frameRate = engine.FrameRate;

            Debug.WriteLine(this, "Creating root EventViewmodel");
            _rootEventViewModel = new EventPanelRootViewmodel(this);
            _engine.EngineTick += this._engineTick;
            _engine.EngineOperation += this._engineOperation;
            _engine.PropertyChanged += this._enginePropertyChanged;
            _engine.VisibleEventsOperation += _onEngineVisibleEventsOperation;
            _engine.RunningEventsOperation += OnEngineRunningEventsOperation;
            _engine.EventSaved += _engine_EventSaved;
            _engine.DatabaseConnectionStateChanged += _engine_DatabaseConnectionStateChanged;
            _composePlugins();

            _fixedTimeEvents = new ObservableCollection<EventPanelAutoStartEventViewmodel>(engine.FixedTimeEvents.Select(e => new EventPanelAutoStartEventViewmodel(e)));
            engine.FixedTimeEventOperation += _engine_FixedTimeEventOperation;


            Debug.WriteLine(this, "Creating EngineView");
            _engineView = new EngineView(this._frameRate);
            _engineView.DataContext = this;

            if (preview != null)
            {
                _previewViewmodel = new PreviewViewmodel(preview) { IsSegmentsVisible = true };
                _previewView = new Views.PreviewView(_previewViewmodel.FrameRate) { DataContext = _previewViewmodel };
            }
            Debug.WriteLine(this, "Creating EventEditViewmodel");
            _eventEditViewmodel = new EventEditViewmodel(this, _previewViewmodel);
            _eventEditView = new EventEditView(_frameRate) { DataContext = _eventEditViewmodel };

            _createCommands();

            _selectedEvents = new ObservableCollection<EventPanelViewmodelBase>();
            _selectedEvents.CollectionChanged += _selectedEvents_CollectionChanged;
            EventClipboard.ClipboardChanged += _engineViewmodel_ClipboardChanged;
            if (engine.PlayoutChannelPRI != null)
                engine.PlayoutChannelPRI.OwnerServer.PropertyChanged += OnPRIServerPropertyChanged;
            if (engine.PlayoutChannelSEC != null)
                engine.PlayoutChannelSEC.OwnerServer.PropertyChanged += OnSECServerPropertyChanged;
            if (engine.PlayoutChannelPRV != null)
                engine.PlayoutChannelPRV.OwnerServer.PropertyChanged += OnPRVServerPropertyChanged;
        }

        private void _engine_FixedTimeEventOperation(object sender, CollectionOperationEventArgs<IEvent> e)
        {
            if (e.Operation == TCollectionOperation.Insert)
                _fixedTimeEvents.Add(new EventPanelAutoStartEventViewmodel(e.Item));
            if (e.Operation == TCollectionOperation.Remove)
                _fixedTimeEvents.Remove(_fixedTimeEvents.FirstOrDefault(evm => evm.Event == e.Item));
        }

        private void _engine_DatabaseConnectionStateChanged(object sender, RedundantConnectionStateEventArgs e)
        {
            NotifyPropertyChanged("NoAlarms");
            NotifyPropertyChanged("DatabaseOK");
        }

        private void _engine_EventSaved(object sender, IEventEventArgs e)
        {
            if (RootEventViewModel.Childrens.Any(evm => evm.Event == e.Event))
                NotifyPropertyChanged("IsAnyContainerHidden");
        }

        protected override void OnDispose()
        {
            _engine.EngineTick -= _engineTick;
            _engine.EngineOperation -= _engineOperation;
            _engine.PropertyChanged -= _enginePropertyChanged;
            _engine.VisibleEventsOperation -= _onEngineVisibleEventsOperation;
            _engine.RunningEventsOperation -= OnEngineRunningEventsOperation;
            _engine.FixedTimeEventOperation -= _engine_FixedTimeEventOperation;


            _selectedEvents.CollectionChanged -= _selectedEvents_CollectionChanged;
            _engine.EventSaved -= _engine_EventSaved;
            _engine.DatabaseConnectionStateChanged -= _engine_DatabaseConnectionStateChanged;
            EventClipboard.ClipboardChanged -= _engineViewmodel_ClipboardChanged;
            if (_engine.PlayoutChannelPRI != null)
                _engine.PlayoutChannelPRI.OwnerServer.PropertyChanged -= OnPRIServerPropertyChanged;
            if (_engine.PlayoutChannelSEC != null)
                _engine.PlayoutChannelSEC.OwnerServer.PropertyChanged -= OnSECServerPropertyChanged;
            if (_engine.PlayoutChannelPRV != null)
                _engine.PlayoutChannelPRV.OwnerServer.PropertyChanged -= OnPRVServerPropertyChanged;
        }

        void _engineViewmodel_ClipboardChanged()
        {
            InvalidateRequerySuggested();
        }

        public EngineView View { get { return _engineView; } }
        public Views.PreviewView PreviewView { get { return _previewView; } }
        public EventEditView EventEditView { get { return _eventEditView; } }


        #region Commands

        private void _createCommands()
        {
            CommandClearAll = new UICommand() { ExecuteDelegate = o => _engine.Clear() };
            CommandClearLayer = new UICommand() { ExecuteDelegate = layer => _engine.Clear((VideoLayer)int.Parse((string)layer)) };
            CommandRestart = new UICommand() { ExecuteDelegate = ev => _engine.Restart() };
            CommandStartSelected = new UICommand() { ExecuteDelegate = o => _engine.Start(_selected.Event), CanExecuteDelegate = _canStartSelected };
            CommandLoadSelected = new UICommand() { ExecuteDelegate = o => _engine.Load(_selected.Event), CanExecuteDelegate = _canLoadSelected };
            CommandScheduleSelected = new UICommand() { ExecuteDelegate = o => _engine.Schedule(_selected.Event), CanExecuteDelegate = _canScheduleSelected };
            CommandRescheduleSelected = new UICommand() { ExecuteDelegate = o => _engine.ReScheduleAsync(_selected.Event), CanExecuteDelegate = _canRescheduleSelected };
            CommandForceNextSelected = new UICommand() { ExecuteDelegate = _forceNext, CanExecuteDelegate = _canForceNextSelected };
            CommandTrackingToggle = new UICommand() { ExecuteDelegate = o => TrackPlayingEvent = !TrackPlayingEvent };
            CommandDebugToggle = new UICommand() { ExecuteDelegate = _debugShow };
            CommandRestartRundown = new UICommand() { ExecuteDelegate = _restartRundown };
            CommandNewRootRundown = new UICommand() { ExecuteDelegate = _newRootRundown };
            CommandNewContainer = new UICommand() { ExecuteDelegate = _newContainer };
            CommandSearchMissingEvents = new UICommand() { ExecuteDelegate = _searchMissingEvents };
            CommandStartLoaded = new UICommand() { ExecuteDelegate = o => _engine.StartLoaded(), CanExecuteDelegate = o => _engine.EngineState == TEngineState.Hold };
            CommandDeleteSelected = new UICommand() { ExecuteDelegate = _deleteSelected, CanExecuteDelegate = o => _selectedEvents.Any() };
            CommandCopySelected = new UICommand() { ExecuteDelegate = _copySelected, CanExecuteDelegate = o => _selectedEvents.Any() };
            CommandCutSelected = new UICommand() { ExecuteDelegate = _cutSelected, CanExecuteDelegate = o => _selectedEvents.Any() };
            CommandPasteSelected = new UICommand() { ExecuteDelegate = _pasteSelected, CanExecuteDelegate = o => EventClipboard.CanPaste(_selected, (EventClipboard.TPasteLocation)Enum.Parse(typeof(EventClipboard.TPasteLocation), o.ToString(), true)) };
            CommandExportMedia = new UICommand() { ExecuteDelegate = _exportMedia, CanExecuteDelegate = _canExportMedia };


            CommandEventHide = new UICommand { ExecuteDelegate = _eventHide };
            CommandMoveUp = new UICommand { ExecuteDelegate = _moveUp };
            CommandMoveDown = new UICommand { ExecuteDelegate = _moveDown };
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

            CommandSaveEdit = new UICommand { ExecuteDelegate = _eventEditViewmodel.CommandSaveEdit.Execute };
            CommandUndoEdit = new UICommand { ExecuteDelegate = _eventEditViewmodel.CommandUndoEdit.Execute };

            CommandSaveRundown = new UICommand { ExecuteDelegate = _saveRundown, CanExecuteDelegate = o => Selected != null && Selected.Event.EventType == TEventType.Rundown };
            CommandLoadRundown = new UICommand { ExecuteDelegate = _loadRundown, CanExecuteDelegate = o => o.Equals("Under") ? _canAddSubRundown(o) : _canAddNextRundown(o) };
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
                using (var reader = System.IO.File.OpenText(dlg.FileName))
                using (var jreader = new Newtonsoft.Json.JsonTextReader(reader))
                {
                    var proxy = (new Newtonsoft.Json.JsonSerializer()).Deserialize<EventProxy>(jreader);
                    if (obj.Equals("Under"))
                        proxy.InsertUnder(Selected.Event);
                    else
                        proxy.InsertAfter(Selected.Event);
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
                    new Newtonsoft.Json.JsonSerializer() { Formatting = Newtonsoft.Json.Formatting.Indented }
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

        private void _moveDown(object obj)
        {
            var ep = Selected as EventPanelRundownElementViewmodelBase;
            if (ep != null)
                ep.CommandMoveDown.Execute(obj);
        }

        private void _moveUp(object obj)
        {
            var ep = Selected as EventPanelRundownElementViewmodelBase;
            if (ep != null)
                ep.CommandMoveUp.Execute(obj);
        }

        private void _eventHide(object obj)
        {
            var ep = Selected as EventPanelContainerViewmodel;
            if (ep != null)
                ep.CommandHide.Execute(obj);
        }
    
        private void _pasteSelected(object obj)
        {
            EventClipboard.Paste(_selected, (EventClipboard.TPasteLocation)Enum.Parse(typeof(EventClipboard.TPasteLocation), (string)obj, true));
        }

        private void _copySelected(object obj)
        {
            EventClipboard.Copy(_selectedEvents);
        }

        private void _cutSelected(object obj)
        {
            EventClipboard.Cut(_selectedEvents);
        }


        private bool _canExportMedia(object obj)
        {
            return _selectedEvents.Any(e =>
                    {
                        if (e is EventPanelMovieViewmodel)
                        {
                            IMedia media = ((EventPanelMovieViewmodel)e).Media;
                            return media != null && media.FileExists();
                        }
                        return false;
                    })
                    && _engine.MediaManager.IngestDirectories.Any(d => d.IsExport);
        }

        private void _exportMedia(object obj)
        {
            var selections = _selectedEvents.Where(e => e.Event != null && e.Event.Media != null && e.Event.Media.MediaType == TMediaType.Movie).Select(e => new ExportMedia(
                e.Event.Media, 
                e.Event.SubEvents.Where(sev => sev.EventType == TEventType.StillImage && sev.Media != null).Select(sev => sev.Media).ToList(),
                e.Event.ScheduledTc, 
                e.Event.Duration, 
                e.Event.GetAudioVolume()));
            using (ExportViewmodel evm = new ExportViewmodel(_engine.MediaManager, selections)) { }
        }

       
        private bool _canStartSelected(object o)
        {
            IEvent ev = _selected == null ? null : _selected.Event;
            return ev != null
                && (ev.PlayState == TPlayState.Scheduled || ev.PlayState == TPlayState.Paused || ev.PlayState == TPlayState.Aborted)
                && (ev.EventType == TEventType.Rundown || ev.EventType == TEventType.Live || ev.EventType == TEventType.Movie);
        }
        private bool _canLoadSelected(object o)
        {
            IEvent ev = _selected == null ? null : _selected.Event;
            return ev != null
                && (ev.PlayState == TPlayState.Scheduled || ev.PlayState == TPlayState.Aborted)
                && (ev.EventType == TEventType.Rundown || ev.EventType == TEventType.Live || ev.EventType == TEventType.Movie);
        }
        private bool _canScheduleSelected(object o)
        {
            IEvent ev = _selected == null ? null : _selected.Event;
            return ev != null && (ev.PlayState == TPlayState.Scheduled || ev.PlayState == TPlayState.Paused) && ev.ScheduledTime >= _currentTime;
        }
        private bool _canRescheduleSelected(object o)
        {
            IEvent ev = _selected == null ? null : _selected.Event;
            return ev != null && (ev.PlayState == TPlayState.Aborted || ev.PlayState == TPlayState.Played);
        }
        private bool _canCut(object o)
        {
            IEvent ev = _selected == null ? null : _selected.Event;
            return ev != null
                && (ev.EventType == TEventType.Rundown || ev.EventType == TEventType.Movie || ev.EventType == TEventType.Live)
                && ev.PlayState == TPlayState.Scheduled;
        }
        private bool _canCopySingle(object o)
        {
            IEvent ev = _selected == null ? null : _selected.Event;
            return ev != null
                && (ev.EventType == TEventType.Rundown || ev.EventType == TEventType.Movie || ev.EventType == TEventType.Live);
        }

        private void _restartRundown(object o)
        {
            IEvent ev = _selected == null ? null : _selected.Event;
            if (ev != null)
                _engine.RestartRundown(ev);
        }

        private void _newRootRundown(object o)
        {
            IEvent newEvent = _engine.AddNewEvent(
                eventType: TEventType.Rundown,
                eventName: resources._title_NewRundown,
                startType: TStartType.Manual,
                scheduledTime: _currentTime);
            _engine.RootEvents.Add(newEvent);
            newEvent.Save();
        }
        
        private void _newContainer(object o)
        {
            IEvent newEvent = _engine.AddNewEvent(
                eventType: TEventType.Container,
                eventName: resources._title_NewContainer);
            _engine.RootEvents.Add(newEvent);
            newEvent.Save();
        }

        private void _deleteSelected(object ob)
        {
            var evmList = _selectedEvents.ToList();
            var containerList = evmList.Where(evm => evm is EventPanelContainerViewmodel);
            if (evmList.Count() > 0
                && MessageBox.Show(string.Format(resources._query_DeleteSelected, evmList.Count(), evmList.AsString(Environment.NewLine, 20)), resources._caption_Confirmation, MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK
                && (containerList.Count() == 0
                    || MessageBox.Show(string.Format(resources._query_DeleteSelectedContainers, containerList.Count(), containerList.AsString(Environment.NewLine, 20)), resources._caption_Confirmation, MessageBoxButton.OKCancel) == MessageBoxResult.OK))
            {
                ThreadPool.QueueUserWorkItem(
                    o =>
                    {
                        foreach (var evm in evmList)
                        {
                            if (evm.Event != null
                                && (evm.Event.PlayState == TPlayState.Scheduled || evm.Event.PlayState == TPlayState.Played || evm.Event.PlayState == TPlayState.Aborted))
                                evm.Event.Delete();
                        }
                    }
                );
                _selectedEvents.Clear();
            }
        }
    

        private void _debugShow(object o)
        {
            if (_debugWindow == null)
            {
                _debugWindow = new Views.EngineStateView();
                _debugWindow.DataContext = this;
                _debugWindow.Closed += (w, e) =>
                {
                    var window = w as Views.EngineStateView;
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
                _mediaSearchViewModel = new MediaSearchViewmodel(_engine, _engine.MediaManager, mediaType, closeAfterAdd, _engine.FormatDescription);
                _mediaSearchViewModel.BaseEvent = baseEvent;
                _mediaSearchViewModel.NewEventStartType = startType;
                _mediaSearchViewModel.SearchWindowClosed += (o, e) =>
                {
                    MediaSearchViewmodel mvs = (MediaSearchViewmodel)o;
                    _mediaSearchViewModel.Dispose();
                    _mediaSearchViewModel = null;
                };
                _mediaSearchViewModel.MediaChoosen += (o, e) =>
                {
                    if (e.Media != null)
                    {
                        IEvent newEvent;
                        switch (e.Media.MediaType)
                        {
                            case TMediaType.Movie:
                                TMediaCategory category = e.Media.MediaCategory;
                                newEvent = _engine.AddNewEvent(
                                    eventName: e.MediaName,
                                    videoLayer: VideoLayer.Program,
                                    eventType: TEventType.Movie,
                                    scheduledTC: e.TCIn,
                                    duration: e.Duration,
                                    gpi: new EventGPI
                                    {
                                        CanTrigger = _engine.EnableGPIForNewEvents,
                                        Crawl = category == TMediaCategory.Show ? TCrawl.Normal : TCrawl.NoCrawl,
                                        Logo = category == TMediaCategory.Fill || category == TMediaCategory.Show || category == TMediaCategory.Promo || category == TMediaCategory.Insert || category == TMediaCategory.Jingle ? TLogo.Normal : TLogo.NoLogo,
                                        Parental = e.Media.Parental
                                    }
                                    );
                                break;
                            case TMediaType.Still:
                                newEvent = _engine.AddNewEvent(
                                    eventName: e.MediaName,
                                    eventType: TEventType.StillImage,
                                    videoLayer: layer,
                                    duration: baseEvent.Duration);
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
                        if (_mediaSearchViewModel.NewEventStartType == TStartType.After)
                            _mediaSearchViewModel.BaseEvent.InsertAfter(newEvent);
                        if (_mediaSearchViewModel.NewEventStartType == TStartType.With)
                            _mediaSearchViewModel.BaseEvent.InsertUnder(newEvent);
                        _mediaSearchViewModel.NewEventStartType = TStartType.After;
                        _mediaSearchViewModel.BaseEvent = newEvent;
                        LastAddedEvent = newEvent;
                    }
                };
            }
        }
        /// <summary>
        /// Used to determine if it should be selected when it's viewmodel is created
        /// </summary>
        public IEvent LastAddedEvent { get; private set; }
        #endregion // MediaSearch

        readonly EventPanelViewmodelBase _rootEventViewModel;
        public EventPanelViewmodelBase RootEventViewModel { get { return _rootEventViewModel; } }

        readonly ObservableCollection<EventPanelAutoStartEventViewmodel> _fixedTimeEvents;
        public ObservableCollection<EventPanelAutoStartEventViewmodel> FixedTimeEvents { get { return _fixedTimeEvents; } }

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
                    _previewViewmodel.Event = newSelected;
                    _eventEditViewmodel.Event = newSelected;
                    var re = value as EventPanelRundownElementViewmodelBase;
                    if (re != null && _mediaSearchViewModel != null && re.CommandAddNextMovie.CanExecute(null))
                    {
                        _mediaSearchViewModel.BaseEvent = re.Event;
                        _mediaSearchViewModel.NewEventStartType = TStartType.After;
                    }
                    InvalidateRequerySuggested();
                    _updatePluginCanExecute();
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
            private set { SetField(ref _currentTime, value, "CurrentTime"); }
        }

        private RationalNumber _frameRate;
        public RationalNumber FrameRate { get { return _frameRate; } }

        private TimeSpan _timeToAttention;
        public TimeSpan TimeToAttention
        {
            get { return _timeToAttention; }
            set { SetField(ref _timeToAttention, value, "TimeToAttention"); }
        }

        public bool IsAnyContainerHidden
        {
            get { return _rootEventViewModel.Childrens.Any(evm => evm is EventPanelContainerViewmodel && !((EventPanelContainerViewmodel)evm).IsVisible); }
        }

        public bool NoAlarms
        {
            get
            {
                return ServerConnectedPRI
                       && ServerConnectedSEC
                       && ServerConnectedPRV
                       && DatabaseOK;
            }
        }

        public bool ServerConnectedPRI
        {
            get
            {
                var channel = _engine.PlayoutChannelPRI;
                if (channel != null)
                {
                    var server = channel.OwnerServer;
                    if (server != null)
                        return server.IsConnected;
                }
                return false;
            }
        }

        public bool ServerConnectedSEC
        {
            get
            {
                var channel = _engine.PlayoutChannelSEC;
                if (channel != null)
                {
                    var server = channel.OwnerServer;
                    if (server != null)
                        return server.IsConnected;
                }
                return false;
            }
        }

        public bool ServerConnectedPRV
        {
            get
            {
                var channel = _engine.PlayoutChannelPRV;
                if (channel != null)
                {
                    var server = channel.OwnerServer;
                    if (server != null)
                        return server.IsConnected;
                }
                return false;
            }
        }

        public bool DatabaseOK
        {
            get { return _engine.DatabaseConnectionState == ConnectionStateRedundant.Open; } 
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
            get { return _selectedEvents.Count; }
        }

        public TimeSpan SelectedTime
        {
            get { return TimeSpan.FromTicks(_selectedEvents.Sum(e => e.Event.Duration.Ticks)); }
        }

        public bool IsForcedNext
        {
            get { return _engine.ForcedNext != null; }
        }

        #region Plugin
        CompositionContainer _uiContainer;

        [ImportMany]
        IUiPlugin[] _plugins = null;

        public IList<Common.Plugin.IUiPlugin> Plugins { get { return _plugins; } }

        public bool IsAnyPluginActive { get { return _plugins != null && _plugins.Length > 0; } }

        private void _composePlugins()
        {
            try
            {
                var pluginPath = Path.GetFullPath(".\\Plugins");
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
        #region GPI
        public bool GPIExists
        {
            get { return _engine.Gpi != null; }
        }

        public bool GPIConnected
        {
            get {return _engine.GPIConnected;}
        }

        public bool GPIEnabled { get { return _engine.GPIEnabled; } set { _engine.GPIEnabled = value; } }
        
        public bool GPIAspectNarrow
        {
            get { return _engine.GPIAspectNarrow; }
            set { _engine.GPIAspectNarrow = value; }
        }

        readonly Array _gPICrawls = Enum.GetValues(typeof(TCrawl));
        public Array GPICrawls { get { return _gPICrawls; } }
        
        public TCrawl GPICrawl
        {
            get { return _engine.GPICrawl; }
            set { _engine.GPICrawl = value; }
        }

        readonly Array _gPILogos = Enum.GetValues(typeof(TLogo));
        public Array GPILogos { get { return _gPILogos; } }
        
        public TLogo GPILogo
        {
            get { return _engine.GPILogo; }
            set { _engine.GPILogo = value; }
        }

        readonly Array _gPIParentals = Enum.GetValues(typeof(TParental)); 
        public Array GPIParentals { get { return _gPIParentals; } }
        
        public TParental GPIParental
        {
            get { return _engine.GPIParental; }
            set { _engine.GPIParental = value; }
        }

        public bool GPIIsMaster
        {
            get { return _engine.GPIIsMaster; }
        }
        #endregion // GPI


        public TEngineState EngineState { get { return _engine.EngineState; } }

        private readonly ObservableCollection<IEvent> _visibleEvents = new ObservableCollection<IEvent>();
        public IEnumerable<IEvent> VisibleEvents { get { return _visibleEvents; } }
        private readonly ObservableCollection<IEvent> _runningEvents = new ObservableCollection<IEvent>();
        public IEnumerable<IEvent> RunningEvents { get { return _runningEvents; } }

        private readonly ObservableCollection<EventPanelViewmodelBase> _selectedEvents;
        public IEnumerable<EventPanelViewmodelBase> SelectedEvents { get { return _selectedEvents; } }

        public void ClearSelection()
        {
            foreach (var evm in _selectedEvents)
                evm.IsMultiSelected = false;
            _selectedEvents.Clear();
        }

        public void RemoveSelected(EventPanelViewmodelBase evm)
        {
            if (_selectedEvents.Contains(evm))
                _selectedEvents.Remove(evm);
        }


        private Views.EngineStateView _debugWindow;
        public void _searchMissingEvents(object o)
        {
            _engine.SearchMissingEvents();
        }

        private void _onSelectedEventPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var selected = _selected;
            if (selected != null && sender == selected.Event && e.PropertyName == "PlayState")
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
                    NotifyPropertyChanged("PlayingEventName");
                    NotifyPropertyChanged("NextToPlay");
                    NotifyPropertyChanged("NextWithRequestedStartTime");
                }
                NotifyPropertyChanged("VisibleEvents");
            }

            if (a.Operation == TEngineOperation.Load)
                InvalidateRequerySuggested();

            if (a.Operation == TEngineOperation.Stop || a.Operation == TEngineOperation.Clear)
            {
                NotifyPropertyChanged("PlayingEventName");
                NotifyPropertyChanged("NextToPlay");
                NotifyPropertyChanged("NextWithRequestedStartTime");
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

        public void OnPRIServerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsConnected")
            {
                NotifyPropertyChanged("ServerConnectedPRI");
                NotifyPropertyChanged("NoAlarms");
            }
        }

        public void OnSECServerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsConnected")
            {
                NotifyPropertyChanged("ServerConnectedSEC");
                NotifyPropertyChanged("NoAlarms");
            }
        }

        public void OnPRVServerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsConnected")
            {
                NotifyPropertyChanged("ServerConnectedPRV");
                NotifyPropertyChanged("NoAlarms");
            }
        }

        public decimal ProgramAudioVolume //decibels
        {
            get { return (decimal)(20 * Math.Log10((double)_engine.ProgramAudioVolume)); }
            set
            {
                decimal volumeDB = (decimal)Math.Pow(10, (double)value / 20);
                if (value != volumeDB)
                {
                    _engine.ProgramAudioVolume = volumeDB;
                    NotifyPropertyChanged("ProgramAudioVolume");
                }
            }
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
            NotifyPropertyChanged("SelectedCount");
            NotifyPropertyChanged("SelectedTime");
            InvalidateRequerySuggested();
        }

        private void _enginePropertyChanged(object o, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ProgramAudioVolume"
                || e.PropertyName == "EngineState"
                || e.PropertyName == "GPIConnected"
                || e.PropertyName == "GPIAspectNarrow"
                || e.PropertyName == "GPICrawl"
                || e.PropertyName == "GPILogo"
                || e.PropertyName == "GPIParental"
                || e.PropertyName == "GPIIsMaster"
                || e.PropertyName == "GPIEnabled"
                || e.PropertyName == "NextToPlay"
            )
                NotifyPropertyChanged(e.PropertyName);
            if (e.PropertyName == "GPIIsMaster")
                NotifyPropertyChanged("GPIEnabled");
            if (e.PropertyName == "ForcedNext")
                NotifyPropertyChanged("IsForcedNext");
            if (e.PropertyName == "EngineState")
                InvalidateRequerySuggested();
        }

        private bool _trackPlayingEvent = true;
        public bool TrackPlayingEvent
        {
            get { return _trackPlayingEvent; }
            set
            {
                if (value != _trackPlayingEvent)
                {
                    _trackPlayingEvent = value;
                    NotifyPropertyChanged("TrackPlayingEvent");
                    if (_trackPlayingEvent)
                    {
                        IEvent cp = _engine.Playing;
                        if (cp != null)
                            SetOnTopView(cp);
                    }
                }
            }
        }
    

    }
}
