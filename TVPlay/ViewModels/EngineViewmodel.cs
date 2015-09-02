using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using TAS.Server;
using TAS.Common;
using System.Windows;
using System.Runtime.Remoting.Messaging;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Diagnostics;

namespace TAS.Client.ViewModels
{
    public class EngineViewmodel : ViewmodelBase
    {
        private readonly Engine _engine;
        private readonly EventEditViewmodel _eventEditViewmodel;
        private readonly EventClipboard _clipboard;
        private readonly EngineView _engineView;
        private Event _selectedEvent;
        public Engine Engine { get { return _engine; } }

        public ICommand CommandClearAll { get; private set; }
        public ICommand CommandClearLayer { get; private set; }
        public ICommand CommandRestartLayer { get; private set; }
        public ICommand CommandStartSelected { get; private set; }
        public ICommand CommandStartLoaded { get; private set; }
        public ICommand CommandLoadSelected { get; private set; }
        public ICommand CommandScheduleSelected { get; private set; }
        public ICommand CommandRescheduleSelected { get; private set; }
        public ICommand CommandTrackingToggle { get; private set; }
        public ICommand CommandRestartRundown { get; private set; }
        public ICommand CommandNewRootRundown { get; private set; }
        public ICommand CommandNewContainer { get; private set; }
        public ICommand CommandCutSingle { get; private set; }
        public ICommand CommandCutMultiple { get; private set; }
        public ICommand CommandCopySingle { get; private set; }
        public ICommand CommandPaste { get; private set; }
        public ICommand CommandDebugToggle { get; private set; }
        public ICommand CommandSearchMissingEvents { get; private set; }
        public ICommand CommandResume { get; private set; }
        public ICommand CommandPause { get; private set; }
        public ICommand CommandDeleteSelected { get; private set; }
        public ICommand CommandExport { get; private set; }
        public ICommand CommandEngineSettings { get; private set; }

        public ICommand CommandAddNewMovie { get { return _eventEditViewmodel.CommandAddNextMovie; } }
        public ICommand CommandAddNewRundown { get { return _eventEditViewmodel.CommandAddNextRundown; } }
        public ICommand CommandAddNewLive { get { return _eventEditViewmodel.CommandAddNextLive; } }
        public ICommand CommandAddSubMovie { get { return _eventEditViewmodel.CommandAddSubMovie; } }
        public ICommand CommandAddSubRundown { get { return _eventEditViewmodel.CommandAddSubRundown; } }
        public ICommand CommandAddSubLive { get { return _eventEditViewmodel.CommandAddSubLive; } }
        public ICommand CommandToggleEnabled { get { return _eventEditViewmodel.CommandToggleEnabled; } }
        public ICommand CommandToggleHold { get { return _eventEditViewmodel.CommandToggleHold; } }
        public ICommand CommandSaveEdit { get { return _eventEditViewmodel.CommandSaveEdit; } }

        public EngineViewmodel(Server.Engine engine)
        {
            _engine = engine;
            
            _engine.EngineTick += this.OnEngineTick;
            _engine.EngineOperation += this._engineOperation;
            _engine.ServerPropertyChanged += OnServerPropertyChanged;
            _engine.PropertyChanged += this.OnEnginePropertyChanged;
            _engine.VisibleEventsOperation += OnEnginePlayingEventsDictionaryOperation;
            _engine.LoadedNextEventsOperation += OnEngineLoadedEventsDictionaryOperation;
            _engine.RunningEventsOperation += OnEngineRunningEventsOperation;

            Debug.WriteLine(this, "Creating root EventViewmodel");
            _rootEventViewModel = new EventViewmodel(_engine, this);

            Debug.WriteLine(this, "Creating EventEditViewmodel");
            _eventEditViewmodel = new EventEditViewmodel(this);
            
            Debug.WriteLine(this, "Creating EventClipboard");
            _clipboard = new EventClipboard(this);
            
            Debug.WriteLine(this, "Creating PlayingEventViewmodel");
            _playingEventViewmodel = new PlayingEventViewmodel(_engine);
            _createCommands();

            Debug.WriteLine(this, "Creating EngineView");
            _engineView = new EngineView();
            _engineView.DataContext = this;

            _selectedEvents = new ObservableCollection<EventViewmodel>();
            _selectedEvents.CollectionChanged += _selectedEvents_CollectionChanged;
        }

        protected override void OnDispose()
        {
            _engine.EngineTick -= this.OnEngineTick;
            _engine.EngineOperation -= this._engineOperation;
            _engine.ServerPropertyChanged -= this.OnServerPropertyChanged;
            _engine.PropertyChanged -= this.OnEnginePropertyChanged;
            _selectedEvents.CollectionChanged -= _selectedEvents_CollectionChanged;
        }

        public EngineView View { get { return _engineView; } }

        #region Commands

        private void _createCommands()
        {
            CommandClearAll = new SimpleCommand() { ExecuteDelegate = o => _engine.Clear() };
            CommandClearLayer = new SimpleCommand() { ExecuteDelegate = layer => _engine.Clear((VideoLayer)int.Parse((string)layer)) };
            CommandRestartLayer = new SimpleCommand() { ExecuteDelegate = layer => _engine.RestartLayer((VideoLayer)int.Parse((string)layer)) };
            CommandStartSelected = new SimpleCommand() { ExecuteDelegate = o => _engine.Start(_selected.Event), CanExecuteDelegate = _canStartSelected };
            CommandLoadSelected = new SimpleCommand()
            {
                ExecuteDelegate = o =>
                    {
                        Event e = _selectedEvent;
                        _engine.ReScheduleAsync(e);
                        _engine.Load(e);
                    },
                CanExecuteDelegate = _canLoadSelected
            };

            CommandScheduleSelected = new SimpleCommand()
            {
                ExecuteDelegate = o =>
                    {
                        Event e = _selectedEvent;
                        _engine.ReScheduleAsync(e);
                        _engine.Schedule(e);
                    },
                CanExecuteDelegate = _canScheduleSelected
            };
            CommandRescheduleSelected = new SimpleCommand() { ExecuteDelegate = o => _engine.ReScheduleAsync(_selectedEvent), CanExecuteDelegate = _canRescheduleSelected };
            CommandTrackingToggle = new SimpleCommand() { ExecuteDelegate = o => TrackPlayingEvent = !TrackPlayingEvent };
            CommandDebugToggle = new SimpleCommand() { ExecuteDelegate = _debugToggle };
            CommandRestartRundown = new SimpleCommand() { ExecuteDelegate = _restartRundown };
            CommandNewRootRundown = new SimpleCommand() { ExecuteDelegate = _newRootRundown };
            CommandNewContainer = new SimpleCommand() { ExecuteDelegate = _newContainer };
            CommandCutSingle = new SimpleCommand() { ExecuteDelegate = o => _cut(false), CanExecuteDelegate = _canCut };
            CommandCutMultiple = new SimpleCommand() { ExecuteDelegate = o => _cut(true), CanExecuteDelegate = _canCut };
            CommandCopySingle = new SimpleCommand() { ExecuteDelegate = o => _copy(false), CanExecuteDelegate =_canCopySingle };
            CommandPaste = new SimpleCommand() { ExecuteDelegate = pos => _paste(pos as string) };
            CommandSearchMissingEvents = new SimpleCommand() { ExecuteDelegate = _searchMissingEvents };
            CommandResume = new SimpleCommand() { ExecuteDelegate = o => _engine.Resume() };
            CommandStartLoaded = new SimpleCommand() { ExecuteDelegate = o => _engine.Resume(), CanExecuteDelegate = o => _engine.EngineState == TEngineState.Hold};
            CommandPause = new SimpleCommand() { ExecuteDelegate = o => _engine.Pause() };
            CommandDeleteSelected = new SimpleCommand() { ExecuteDelegate = _deleteSelected, CanExecuteDelegate = o => _selectedEvents.Any() };
            CommandExport = new SimpleCommand() { ExecuteDelegate = _export, CanExecuteDelegate = _canExport };
            CommandEngineSettings = new SimpleCommand() { ExecuteDelegate = o => new Client.Setup.EngineViewmodel(this.Engine, App.EngineController), CanExecuteDelegate = o => _engine.EngineState == TEngineState.Idle };
        }

        private bool _canExport(object obj)
        {
            return _selectedEvents.Any(e => e.Media != null && e.Media.MediaType == TMediaType.Movie) && _engine.MediaManager.IngestDirectories.Any(d => d.IsXDCAM);
        }

        private void _export(object obj)
        {
            var selections = _selectedEvents.Where(e => e.Media != null && e.Media.MediaType == TMediaType.Movie).Select(e => new MediaExport(e.Media, e.ScheduledTC, e.Duration));
            using (ExportViewmodel evm = new ExportViewmodel(_engine.MediaManager, selections)) { }
        }

       
        private bool _canStartSelected(object o)
        {
            Event ev = _selectedEvent;
            return ev != null
                && (ev.PlayState == TPlayState.Scheduled || ev.PlayState == TPlayState.Paused || ev.PlayState == TPlayState.Aborted)
                && (ev.EventType == TEventType.Rundown || ev.EventType == TEventType.Live || ev.EventType == TEventType.Movie || ev.EventType == TEventType.AnimationFlash);
        }
        private bool _canLoadSelected(object o)
        {
            Event ev = _selectedEvent;
            return ev != null
                && (ev.PlayState == TPlayState.Scheduled || ev.PlayState == TPlayState.Aborted)
                && (ev.EventType == TEventType.Rundown || ev.EventType == TEventType.Live || ev.EventType == TEventType.Movie || ev.EventType == TEventType.AnimationFlash);
        }
        private bool _canScheduleSelected(object o)
        {
            Event ev = _selectedEvent;
            return ev != null && (ev.PlayState == TPlayState.Scheduled || ev.PlayState == TPlayState.Paused) && ev.ScheduledTime >= _engine.CurrentTime;
        }
        private bool _canRescheduleSelected(object o)
        {
            Event ev = _selectedEvent;
            return ev != null && (ev.PlayState == TPlayState.Aborted || ev.PlayState == TPlayState.Played);
        }
        private bool _canCut(object o)
        {
            Event ev = _selectedEvent;
            return ev != null
                && (ev.EventType == TEventType.Rundown || ev.EventType == TEventType.Movie || ev.EventType == TEventType.Live)
                && ev.PlayState == TPlayState.Scheduled;
        }
        private bool _canCopySingle(object o)
        {
            Event ev = _selectedEvent;
            return ev != null
                && (ev.EventType == TEventType.Rundown || ev.EventType == TEventType.Movie || ev.EventType == TEventType.Live);
        }

        private void _restartRundown(object o)
        {
            var selectedEvent = _selectedEvent;
            if (selectedEvent != null)
                _engine.RestartRundown(selectedEvent);
        }

        private void _newRootRundown(object o)
        {
            Event newEvent = new Event(_engine);
            newEvent.EventType = TEventType.Rundown;
            newEvent.EventName = "Playlista";
            newEvent.Duration = TimeSpan.Zero;
            newEvent.StartType = TStartType.Manual;
            newEvent.ScheduledTime = _engine.CurrentTime;
            _engine.RootEvents.Add(newEvent);
            newEvent.Save();
        }
        
        private void _newContainer(object o)
        {
            Event newEvent = new Event(_engine);
            newEvent.EventType = TEventType.Container;
            newEvent.EventName = "Kontener";
            newEvent.StartType = TStartType.None;
            _engine.RootEvents.Add(newEvent);
            newEvent.Save();
        }

        private void _cut(bool multipleElements)
        {
            try
            {
                if (multipleElements)
                    _clipboard.CutMultiple(Selected);
                else
                    _clipboard.CutSingle(Selected);
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format(Properties.Resources._message_CutFailed, e.Message), Properties.Resources._caption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void _copy(bool multipleElements)
        {
            try
            {
                if (multipleElements)
                    _clipboard.CopyMultiple(Selected);
                else
                    _clipboard.CopySingle(Selected);
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format(Properties.Resources._message_CopyFailed, e.Message), Properties.Resources._caption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void _paste(string position)
        {
            try
            {
                switch (position)
                {
                    case "before":
                        _clipboard.Paste(Selected, EventClipboard.TPasteLocation.Before);
                        break;
                    case "after":
                        _clipboard.Paste(Selected, EventClipboard.TPasteLocation.After);
                        break;
                    case "under":
                        _clipboard.Paste(Selected, EventClipboard.TPasteLocation.Under);
                        break;
                    default:
                        throw new InvalidOperationException(string.Format("Invalid paste string: {0}", position));
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format(Properties.Resources._message_PasteFailed, e.Message), Properties.Resources._caption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void _deleteSelected(object ob)
        {
            try
            {
                var evmList = _selectedEvents.ToList();
                var containerList = evmList.Where(evm => evm.IsRootContainer);
                if (evmList.Count() > 0
                    && MessageBox.Show(string.Format(Properties.Resources._query_DeleteSelected, evmList.Count(), string.Join(Environment.NewLine, evmList)), Properties.Resources._caption_Confirmation, MessageBoxButton.YesNo) == MessageBoxResult.Yes
                    && (containerList.Count() == 0
                        || MessageBox.Show(string.Format(Properties.Resources._query_DeleteSelectedContainers, containerList.Count(), string.Join(Environment.NewLine, containerList)), Properties.Resources._caption_Confirmation, MessageBoxButton.YesNo) == MessageBoxResult.Yes))
                {
                    UiServices.SetBusyState();
                    ThreadPool.QueueUserWorkItem(
                        o =>
                        {
                            foreach (var evm in evmList)
                            {
                                if (evm.Event != null
                                    && (evm.Event.PlayState == TPlayState.Scheduled || evm.Event.PlayState == TPlayState.Played || evm.Event.PlayState == TPlayState.Aborted))
                                {
                                    evm.Event.Delete();
                                    evm.IsMultiSelected = false;
                                }
                            }
                        }
                    );
                    _selectedEvents.Clear();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format(Properties.Resources._message_DeleteError, e.Message), Properties.Resources._caption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void _debugToggle(object o)
        {
            if (_debugWindow == null)
            {
                _debugWindow = new EngineStateView();
                _debugWindow.DataContext = this;
                _debugWindow.Show();
            }
        }


        #endregion // Commands


        private EventViewmodel _rootEventViewModel;
        public EventViewmodel RootEventViewModel { get { return _rootEventViewModel; } }

        public bool CanPasteBefore { get { return _clipboard.CanPaste(Selected, EventClipboard.TPasteLocation.Before); } }
        public bool CanPasteAfter { get { return _clipboard.CanPaste(Selected, EventClipboard.TPasteLocation.After); } }
        public bool CanPasteUnder { get { return _clipboard.CanPaste(Selected, EventClipboard.TPasteLocation.Under); } }

        private EventViewmodel _selected;
        public EventViewmodel Selected
        {
            get { return _selected; }
            set
            {
                if (value != _selected)
                {
                    var selectedEvent = _selectedEvent;
                    if (selectedEvent != null)
                        selectedEvent.PropertyChanged -= _onSelectedEventPropertyChanged;
                    _selected = value;
                    if (value != null)
                    {
                        if (value.Event != null)
                        {
                            value.Event.PropertyChanged += _onSelectedEventPropertyChanged;
                            selectedEvent = value.Event;
                        }
                    }
                    else
                        selectedEvent = null;
                    _selectedEvent = selectedEvent;
                    PreviewViewmodel.Event = selectedEvent;
                    _eventEditViewmodel.Event = selectedEvent;
                    _onSelectedChanged();
                }
            }
        }

        public PreviewViewmodel _previewViewmodel;
        public PreviewViewmodel PreviewViewmodel
        {
            get { return _previewViewmodel; } 
            set
            {
                var pvm = _previewViewmodel;
                if (pvm != value)
                {
                    if (pvm != null)
                        pvm.PropertyChanged -= _eventEditViewmodel._previewPropertyChanged;
                    _previewViewmodel = value;
                    if (value != null)
                        value.PropertyChanged += _eventEditViewmodel._previewPropertyChanged;
                }
            }
        }
        public EventEditViewmodel EventEditViewmodel { get { return _eventEditViewmodel; } }

        private EventViewmodel _GetEventViewModel(Event aEvent)
        {
            IEnumerable<Event> rt = aEvent.VisualRootTrack;
            EventViewmodel evm = _rootEventViewModel;
            foreach (Event ev in rt)
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


        public DateTime CurrentTime
        {
            get { return _engine.CurrentTime; }
        }

        public TimeSpan TimeToPause { get { return _engine.TimeToPause; } }

        private readonly PlayingEventViewmodel _playingEventViewmodel;
        public PlayingEventViewmodel PlayingEvent { get { return _playingEventViewmodel; } }
        
        public bool ServerConnectedPGM
        {
            get
            {
                var channel = _engine.PlayoutChannelPGM;
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

        public int SelectedCount
        {
            get { return _selectedEvents.Count; }
        }

        public TimeSpan SelectedTime
        {
            get { return TimeSpan.FromTicks(_selectedEvents.Sum(e => e.Duration.Ticks)); }
        }

        #region GPI
        public bool GPIExists
        {
            get { return _engine.GPI != null; }
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

        private ObservableCollection<Event> _visibleEvents = new ObservableCollection<Event>();
        public IEnumerable<Event> VisibleEvents { get { return _visibleEvents; } }
        private ObservableCollection<Event> _loadedNextEvents = new ObservableCollection<Event>();
        public IEnumerable<Event> LoadedNextEvents { get { return _loadedNextEvents; } }
        private ObservableCollection<Event> _runningEvents = new ObservableCollection<Event>();
        public IEnumerable<Event> RunningEvents { get { return _runningEvents; } }

        private ObservableCollection<EventViewmodel> _selectedEvents;
        public ObservableCollection<EventViewmodel> SelectedEvents { get { return _selectedEvents; } }

        private EngineStateView _debugWindow;
        public void _searchMissingEvents(object o)
        {
            _engine.SearchMissingEvents();
        }

        private void _onSelectedEventPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender == _selectedEvent && e.PropertyName == "PlayState")
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
                }
                NotifyPropertyChanged("VisibleEvents");
            }

            NotifyPropertyChanged("CommandStartLoaded");

            if (a.Event != null
                && _selected != null
                && a.Event == _selected.Event)
                Application.Current.Dispatcher.BeginInvoke((Action)_onSelectedChanged, null);
        }

        private void SetOnTopView(Event pe)
        {
            var evm = _GetEventViewModel(pe);
            if (evm != null)
            {
                evm.SetOnTop();
            }
        }

        
        private void _onSelectedChanged()
        {
            NotifyPropertyChanged("CommandStartSelected");
            NotifyPropertyChanged("CommandLoadSelected");
            NotifyPropertyChanged("CommandScheduleSelected");
            NotifyPropertyChanged("CommandRescheduleSelected");
            NotifyPropertyChanged("CommandCutSingle");
            NotifyPropertyChanged("CommandCutMultiple");
            NotifyPropertyChanged("CommandCopySingle");
            NotifyPropertyChanged("CanPasteBefore");
            NotifyPropertyChanged("CanPasteAfter");
            NotifyPropertyChanged("CanPasteUnder");
        }

        public void OnEngineTick(object sender, EventArgs a)
        {
            NotifyPropertyChanged("CurrentTime");
            NotifyPropertyChanged("TimeToPause");
        }

        public void OnServerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var ch = _engine.PlayoutChannelPGM;
            if (ch!= null && sender == ch.OwnerServer)
                NotifyPropertyChanged("ServerConnectedPGM");
            ch = _engine.PlayoutChannelPRV;
            if (ch != null && sender == ch.OwnerServer)
                NotifyPropertyChanged("ServerConnectedPRV");
        }

        public decimal AudioVolume //decibels
        {
            get { return (decimal)(20 * Math.Log10((double)_engine.AudioVolume)); }
            set
            {
                decimal volumeDB = (decimal)Math.Pow(10, (double)value / 20);
                if (value != volumeDB)
                {
                    _engine.AudioVolume = volumeDB;
                    NotifyPropertyChanged("AudioVolume");
                }
            }
        }

        private void OnEnginePlayingEventsDictionaryOperation(object o, DictionaryOperationEventArgs<VideoLayer, Event> e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate()
            {
                if (e.Operation == TDictionaryOperation.Add)
                    _visibleEvents.Add(e.Value);
                else
                    _visibleEvents.Remove(e.Value);
            });
        }

        private void OnEngineLoadedEventsDictionaryOperation(object o, DictionaryOperationEventArgs<VideoLayer, Event> e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate()
            {
                if (e.Operation == TDictionaryOperation.Add)
                    _loadedNextEvents.Add(e.Value);
                else
                    _loadedNextEvents.Remove(e.Value);
            });
        }

        private void OnEngineRunningEventsOperation(object o, CollectionOperationEventArgs<Event> e)
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
            if (sender is ObservableCollection<EventViewmodel>)
            {
                NotifyPropertyChanged("CommandDeleteSelected");
                NotifyPropertyChanged("CommandExport");
                NotifyPropertyChanged("SelectedCount");
                NotifyPropertyChanged("SelectedTime");
            }
        }

        private void OnEnginePropertyChanged(object o, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "AudioVolume"
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
                NotifyPropertyChanged("CommandEngineSettings");
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
                        Event cp = _engine.PlayingEvent();
                        if (cp != null)
                            SetOnTopView(cp);
                    }
                }
            }
        }
    

    }
}
