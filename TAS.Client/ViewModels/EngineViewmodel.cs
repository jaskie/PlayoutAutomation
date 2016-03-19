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
using resources = TAS.Client.Common.Properties.Resources;

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
        public ICommand CommandExport { get; private set; }

        public ICommand CommandEventHide { get; private set; }

        public ICommand CommandAddNewMovie { get { return _eventEditViewmodel.CommandAddNextMovie; } }
        public ICommand CommandAddNewRundown { get { return _eventEditViewmodel.CommandAddNextRundown; } }
        public ICommand CommandAddNewLive { get { return _eventEditViewmodel.CommandAddNextLive; } }
        public ICommand CommandAddSubMovie { get { return _eventEditViewmodel.CommandAddSubMovie; } }
        public ICommand CommandAddSubRundown { get { return _eventEditViewmodel.CommandAddSubRundown; } }
        public ICommand CommandAddSubLive { get { return _eventEditViewmodel.CommandAddSubLive; } }
        public ICommand CommandToggleEnabled { get { return _eventEditViewmodel.CommandToggleEnabled; } }
        public ICommand CommandToggleHold { get { return _eventEditViewmodel.CommandToggleHold; } }
        public ICommand CommandSaveEdit { get { return _eventEditViewmodel.CommandSaveEdit; } }
        public ICommand CommandAddNextMovie { get { return _eventEditViewmodel.CommandAddNextMovie; } }
        public ICommand CommandAddGraphics { get { return _eventEditViewmodel.CommandAddGraphics; } }
        public EngineViewmodel(IEngine engine, IPreview preview)
        {
            _engine = engine;
            _frameRate = engine.FrameRate;
            
            _engine.EngineTick += this._engineTick;
            _engine.EngineOperation += this._engineOperation;
            _engine.PropertyChanged += this._enginePropertyChanged;
            _engine.VisibleEventsOperation += OnEnginePlayingEventsDictionaryOperation;
            _engine.LoadedNextEventsOperation += OnEngineLoadedEventsDictionaryOperation;
            _engine.RunningEventsOperation += OnEngineRunningEventsOperation;
            _engine.EventSaved += _engine_EventSaved;

            Debug.WriteLine(this, "Creating root EventViewmodel");
            _rootEventViewModel = new EventPanelRootViewmodel(this);

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

            _selectedEvents = new ObservableCollection<EventPanelViewmodelBase>();
            _selectedEvents.CollectionChanged += _selectedEvents_CollectionChanged;
            EventClipboard.ClipboardChanged += _engineViewmodel_ClipboardChanged;
            if (engine.PlayoutChannelPRI != null)
                engine.PlayoutChannelPRI.OwnerServer.PropertyChanged += OnPRIServerPropertyChanged;
            if (engine.PlayoutChannelSEC != null)
                engine.PlayoutChannelSEC.OwnerServer.PropertyChanged += OnSECServerPropertyChanged;
            if (engine.PlayoutChannelPRV != null)
                engine.PlayoutChannelPRV.OwnerServer.PropertyChanged += OnPRVServerPropertyChanged;
            _createCommands();
        }

        private void _engine_EventSaved(object sender, IEventEventArgs e)
        {
            if (RootEventViewModel.Childrens.Any(evm => evm.Event == e.Event))
                NotifyPropertyChanged("IsAnyContainerHidden");
        }

        protected override void OnDispose()
        {
            _engine.EngineTick -= this._engineTick;
            _engine.EngineOperation -= this._engineOperation;
            _engine.PropertyChanged -= this._enginePropertyChanged;
            _selectedEvents.CollectionChanged -= _selectedEvents_CollectionChanged;
            _engine.EventSaved -= _engine_EventSaved;
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
            NotifyPropertyChanged("CommandPasteSelected");
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
            CommandExport = new UICommand() { ExecuteDelegate = _export, CanExecuteDelegate = _canExport };
            CommandEventHide = new UICommand() { ExecuteDelegate = _eventHide };
        }

        private void _eventHide(object obj)
        {
            var ep = Selected as EventPanelContainerViewmodel;
            if (ep != null)
                ep.IsVisible = false;
        }
    
        private void _pasteSelected(object obj)
        {
            UiServices.SetBusyState();
            EventClipboard.Paste(_selected, (EventClipboard.TPasteLocation)Enum.Parse(typeof(EventClipboard.TPasteLocation), (string)obj, true));
        }

        private void _copySelected(object obj)
        {
            UiServices.SetBusyState();
            EventClipboard.Copy(_selectedEvents);
        }

        private void _cutSelected(object obj)
        {
            UiServices.SetBusyState();
            EventClipboard.Cut(_selectedEvents);
        }


        private bool _canExport(object obj)
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

        private void _export(object obj)
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
            IEvent newEvent = _engine.CreateEvent();
            newEvent.EventType = TEventType.Rundown;
            newEvent.EventName = resources._title_NewRundown;
            newEvent.Duration = TimeSpan.Zero;
            newEvent.StartType = TStartType.Manual;
            newEvent.ScheduledTime = _currentTime;
            _engine.RootEvents.Add(newEvent);
            newEvent.Save();
        }
        
        private void _newContainer(object o)
        {
            IEvent newEvent = _engine.CreateEvent();
            newEvent.EventType = TEventType.Container;
            newEvent.EventName = resources._title_NewContainer;
            newEvent.StartType = TStartType.None;
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
                UiServices.SetBusyState();
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


        private EventPanelViewmodelBase _rootEventViewModel;
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
                        oldSelectedEvent.Deleted -= _selectedEvent_Deleted;
                    }
                    _selected = value;
                    IEvent newSelected = value == null ? null : value.Event;
                    if (newSelected != null)
                    {
                        newSelected.PropertyChanged += _onSelectedEventPropertyChanged;
                        newSelected.Deleted += _selectedEvent_Deleted;
                        oldSelectedEvent = value.Event;
                    }
                    _previewViewmodel.Event = newSelected;
                    _eventEditViewmodel.Event = newSelected;
                    _onSelectedChanged();
                }
            }
        }

        private void _selectedEvent_Deleted(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() => Selected = null));
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

        public string PlayingEventName
        {
            get
            {
                var e = _engine.PlayingEvent();
                return e == null ? string.Empty : e.EventName;
            }
        }

        public int SelectedCount
        {
            get { return _selectedEvents.Count; }
        }

        public TimeSpan SelectedTime
        {
            get { return TimeSpan.FromTicks(_selectedEvents.Sum(e => e.Event.Duration.Ticks)); }
        }

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

        private ObservableCollection<IEvent> _visibleEvents = new ObservableCollection<IEvent>();
        public IEnumerable<IEvent> VisibleEvents { get { return _visibleEvents; } }
        private ObservableCollection<IEvent> _loadedNextEvents = new ObservableCollection<IEvent>();
        public IEnumerable<IEvent> LoadedNextEvents { get { return _loadedNextEvents; } }
        private ObservableCollection<IEvent> _runningEvents = new ObservableCollection<IEvent>();
        public IEnumerable<IEvent> RunningEvents { get { return _runningEvents; } }

        private ObservableCollection<EventPanelViewmodelBase> _selectedEvents;
        public ObservableCollection<EventPanelViewmodelBase> SelectedEvents { get { return _selectedEvents; } }

        private Views.EngineStateView _debugWindow;
        public void _searchMissingEvents(object o)
        {
            _engine.SearchMissingEvents();
        }

        private void _onSelectedEventPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var selected = _selected; 
            if (selected != null && sender == selected.Event && e.PropertyName == "PlayState")
                Application.Current.Dispatcher.BeginInvoke((Action)_onSelectedChanged, null);
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
                            var pe = _engine.PlayingEvent();
                            if (pe != null)
                                SetOnTopView(pe);
                        }, null);
                    NotifyPropertyChanged("PlayingEventName");
                }
                NotifyPropertyChanged("VisibleEvents");
            }

            if (a.Operation == TEngineOperation.Load)
                NotifyPropertyChanged("CommandStartLoaded");

            if (a.Operation == TEngineOperation.Stop || a.Operation == TEngineOperation.Clear)
                NotifyPropertyChanged("PlayingEventName");

            if (a.Event != null
                && _selected != null
                && a.Event == _selected.Event)
                Application.Current.Dispatcher.BeginInvoke((Action)_onSelectedChanged, null);
        }

        private void SetOnTopView(IEvent pe)
        {
            var evm = _GetEventViewModel(pe);
            if (evm != null)
                evm.SetOnTop();
        }

        
        private void _onSelectedChanged()
        {
            NotifyPropertyChanged("CommandStartSelected");
            NotifyPropertyChanged("CommandLoadSelected");
            NotifyPropertyChanged("CommandScheduleSelected");
            NotifyPropertyChanged("CommandRescheduleSelected");
        }

        public void _engineTick(object sender, EngineTickEventArgs e)
        {
            CurrentTime = e.CurrentTime.ToLocalTime();
            TimeToAttention = e.TimeToAttention;
        }

        public void OnPRIServerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsConnected")
                NotifyPropertyChanged("ServerConnectedPRI");
        }

        public void OnSECServerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsConnected")
                NotifyPropertyChanged("ServerConnectedSEC");
        }

        public void OnPRVServerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsConnected")
                NotifyPropertyChanged("ServerConnectedPRV");
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

        private void OnEnginePlayingEventsDictionaryOperation(object o, DictionaryOperationEventArgs<VideoLayer, IEvent> e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate()
            {
                if (e.Operation == TDictionaryOperation.Add)
                    _visibleEvents.Add(e.Value);
                else
                    _visibleEvents.Remove(e.Value);
            });
        }

        private void OnEngineLoadedEventsDictionaryOperation(object o, DictionaryOperationEventArgs<VideoLayer, IEvent> e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate()
            {
                if (e.Operation == TDictionaryOperation.Add)
                    _loadedNextEvents.Add(e.Value);
                else
                    _loadedNextEvents.Remove(e.Value);
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
            if (sender is ObservableCollection<EventPanelViewmodelBase>)
            {
                NotifyPropertyChanged("CommandDeleteSelected");
                NotifyPropertyChanged("CommandExport");
                NotifyPropertyChanged("SelectedCount");
                NotifyPropertyChanged("SelectedTime");
            }
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
            )
                NotifyPropertyChanged(e.PropertyName);
            if (e.PropertyName == "GPIIsMaster")
                NotifyPropertyChanged("GPIEnabled");
            if (e.PropertyName == "EngineState")
            {
                NotifyPropertyChanged("CommandStartSelected");
                NotifyPropertyChanged("CommandLoadSelected");
                NotifyPropertyChanged("CommandScheduleSelected");
                NotifyPropertyChanged("CommandRescheduleSelected");
                NotifyPropertyChanged("CommandStartLoaded");
                InvalidateRequerySuggested();
            }
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
                        IEvent cp = _engine.PlayingEvent();
                        if (cp != null)
                            SetOnTopView(cp);
                    }
                }
            }
        }
    

    }
}
