using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.Remoting.Messaging;
using System.Xml;
using System.Xml.Serialization;
using TAS.Common;
using TAS.Server.Interfaces;
using TAS.Server.Common;
using TAS.Server.Database;
using System.Collections.Concurrent;
using TAS.Remoting.Server;
using Newtonsoft.Json;

namespace TAS.Server
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Engine : DtoBase, IEngine, IDisposable
    {
        public UInt64 Id { get; set; }
        public UInt64 Instance { get; set; }
        public UInt64 IdArchive { get; set; }
        public ulong IdServerPRI { get; set; }
        public int ServerChannelPRI { get; set; }
        public ulong IdServerSEC { get; set; }
        public int ServerChannelSEC { get; set; }
        public ulong IdServerPRV { get; set; }
        public int ServerChannelPRV { get; set; }
        string _engineName;
        [JsonProperty]
        public string EngineName
        {
            get { return _engineName; }
            set { SetField(ref _engineName, value, "EngineName"); }
        }
        #region Fields

        private readonly IMediaManager _mediaManager;
        [XmlIgnore]
        public IMediaManager MediaManager { get { return _mediaManager; } }
        [XmlIgnore]
        public IGpi LocalGpi { get; private set; }

        Thread _engineThread;
        internal long CurrentTicks;

        private TimeSpan _preloadTime = new TimeSpan(0, 0, 2); // time to preload event
        readonly ObservableSynchronizedCollection<Event> _visibleEvents = new ObservableSynchronizedCollection<Event>(); // list of visible events
        readonly ObservableSynchronizedCollection<Event> _runningEvents = new ObservableSynchronizedCollection<Event>(); // list of events loaded and playing 
        readonly ConcurrentDictionary<VideoLayer, Event> _preloadedEvents = new ConcurrentDictionary<VideoLayer, Event>();
        readonly SynchronizedCollection<IEvent> _rootEvents = new SynchronizedCollection<IEvent>();
        readonly ConcurrentDictionary<ulong, IEvent> _events = new ConcurrentDictionary<ulong, IEvent>();

        private IEvent _forcedNext;

        public event EventHandler<EngineTickEventArgs> EngineTick;
        public event EventHandler<EngineOperationEventArgs> EngineOperation;

        public bool EnableGPIForNewEvents { get; set; }
        [XmlElement("Gpi")]
        public GPINotifier _serGpi { get { return null; } set { _gpi = value; } }
        private GPINotifier _gpi;
        public IGpi Gpi { get { return _gpi; } }

        public RemoteHost Remote { get; set; }
        public TAspectRatioControl AspectRatioControl { get; set; }
        public double VolumeReferenceLoudness { get; set; }

        public int TimeCorrection { get { return (int)_timeCorrection.TotalMilliseconds; } set { _timeCorrection = TimeSpan.FromMilliseconds(value); } }
        protected TimeSpan _timeCorrection;

        #endregion Fields

        #region Constructor
        public Engine()
        {
            _visibleEvents.CollectionOperation += _visibleEventsOperation;
            _runningEvents.CollectionOperation += _runningEventsOperation;
            EngineState = TEngineState.NotInitialized;
            _mediaManager = new MediaManager(this);
            Database.Database.ConnectionStateChanged += _database_ConnectionStateChanged;
        }

        #endregion Constructor

        #region IDisposable implementation

        private bool disposed = false;
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                _visibleEvents.CollectionOperation -= _visibleEventsOperation;
                _runningEvents.CollectionOperation -= _runningEventsOperation;
                foreach (Event e in _rootEvents)
                    e.SaveLoadedTree();
                if (_gpi != null)
                    _gpi.Dispose();
                var remote = Remote;
                if (remote != null)
                    remote.Dispose();
                Database.Database.ConnectionStateChanged -= _database_ConnectionStateChanged;
            }
        }

        #endregion //IDisposable

        private CasparServerChannel _playoutChannelPRI;
        [XmlIgnore]
        public IPlayoutServerChannel PlayoutChannelPRI { get { return _playoutChannelPRI; } }

        private CasparServerChannel _playoutChannelSEC;
        [XmlIgnore]
        public IPlayoutServerChannel PlayoutChannelSEC { get { return _playoutChannelSEC; } }

        private CasparServerChannel _playoutChannelPRV;

        [XmlIgnore]
        public IPlayoutServerChannel PlayoutChannelPRV { get { return _playoutChannelPRV; } }

        long _frameTicks;
        public long FrameTicks { get { return _frameTicks; } }
        RationalNumber _frameRate;
        [XmlIgnore]
        public RationalNumber FrameRate { get { return _frameRate; } }

        public TVideoFormat VideoFormat { get; set; }

        [XmlIgnore]
        public VideoFormatDescription FormatDescription { get; private set; }

        public void Initialize(IEnumerable<IPlayoutServer> servers, IGpi localGpi)
        {
            Debug.WriteLine(this, "Begin initializing");

            var sPRI = servers.FirstOrDefault(S => S.Id == IdServerPRI);
            _playoutChannelPRI = sPRI == null ? null : (CasparServerChannel)sPRI.Channels.FirstOrDefault(c => c.ChannelNumber == ServerChannelPRI);
            var sSEC = servers.FirstOrDefault(S => S.Id == IdServerSEC);
            _playoutChannelSEC = sSEC == null ? null : (CasparServerChannel)sSEC.Channels.FirstOrDefault(c => c.ChannelNumber == ServerChannelSEC);
            var sPRV = servers.FirstOrDefault(S => S.Id == IdServerPRV);
            _playoutChannelPRV = sPRV == null ? null : (CasparServerChannel)sPRV.Channels.FirstOrDefault(c => c.ChannelNumber == ServerChannelPRV);


            LocalGpi = localGpi;
            FormatDescription = VideoFormatDescription.Descriptions[VideoFormat];
            _frameTicks = FormatDescription.FrameTicks;
            _frameRate = FormatDescription.FrameRate;
            var chPRI = PlayoutChannelPRI;
            var chSEC = PlayoutChannelSEC;
            if (chSEC != null
                && chSEC != chPRI)
            {
                ((CasparServer)chSEC.OwnerServer).MediaManager = this.MediaManager as MediaManager;
                chSEC.OwnerServer.Initialize();
                chSEC.OwnerServer.MediaDirectory.DirectoryName = chSEC.ChannelName;
                chSEC.OwnerServer.PropertyChanged += _server_PropertyChanged;
            }

            if (chPRI != null)
            {
                ((CasparServer)chPRI.OwnerServer).MediaManager = this.MediaManager as MediaManager;
                chPRI.OwnerServer.Initialize();
                chPRI.OwnerServer.MediaDirectory.DirectoryName = chPRI.ChannelName;
                chPRI.OwnerServer.PropertyChanged += _server_PropertyChanged;
            }

            MediaManager.Initialize();

            Debug.WriteLine(this, "Reading Root Events");
            this.DbReadRootEvents();

            EngineState = TEngineState.Idle;
            var gpi = _gpi;
            if (gpi != null)
            {
                Debug.WriteLine(this, "Initializing GPI");
                gpi.Started += StartLoaded;
                gpi.Initialize();
                gpi.PropertyChanged += GPI_PropertyChanged;
            }

            if (Remote != null)
            {
                Debug.WriteLine(this, "Initializing Remote interface");
                Remote.Initialize(this);
            }

            if (localGpi != null)
                localGpi.Started += StartLoaded;

            Debug.WriteLine(this, "Creating engine thread");
            _engineThread = new Thread(() =>
            {
                Debug.WriteLine(this, "Engine thread started");
                CurrentTime = AlignDateTime(DateTime.UtcNow + _timeCorrection);
                CurrentTicks = CurrentTime.Ticks;

                List<IEvent> playingEvents = this.DbSearchPlaying();
                IEvent playing = playingEvents.FirstOrDefault(e => e.Layer == VideoLayer.Program && (e.EventType == TEventType.Live || e.EventType == TEventType.Movie));
                if (playing != null)
                {
                    Debug.WriteLine(playing, "Playing event found");
                    if (CurrentTicks < (playing.ScheduledTime + playing.Duration).Ticks)
                    {
                        foreach (Event e in playingEvents)
                        {
                            e.Position = (CurrentTicks - e.ScheduledTime.Ticks) / _frameTicks;
                            _runningEvents.Add(e);
                            _visibleEvents.Add(e);
                        }
                        _engineState = TEngineState.Running;
                        Playing = playing;
                    }
                    else
                        foreach (IEvent e in playingEvents)
                        {
                            e.PlayState = TPlayState.Aborted;
                            e.Save();
                        }
                }
                else
                    foreach (IEvent e in playingEvents)
                    {
                        e.PlayState = TPlayState.Aborted;
                        e.Save();
                    }

                while (!disposed)
                {
                    try
                    {
                        CurrentTime = AlignDateTime(DateTime.UtcNow + _timeCorrection);
                        long nFrames = (CurrentTime.Ticks - CurrentTicks) / _frameTicks;
                        CurrentTicks = CurrentTime.Ticks;
                        Debug.WriteLineIf(nFrames > 1, nFrames, "LateFrame");
                        _tick(nFrames);
                        EngineTick?.Invoke(this, new EngineTickEventArgs(CurrentTime, _getTimeToAttention()));
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e, "Exception in engine tick");
                    }
                    long timeToWait = (_frameTicks - (DateTime.UtcNow.Ticks + _timeCorrection.Ticks - CurrentTicks)) / TimeSpan.TicksPerMillisecond;
                    if (timeToWait > 0)
                        Thread.Sleep((int)timeToWait);
                }
                Debug.WriteLine(this, "Engine thread finished");
            });
            _engineThread.Priority = ThreadPriority.Highest;
            _engineThread.Name = string.Format("Engine main thread for {0}", EngineName);
            _engineThread.IsBackground = true;
            _engineThread.Start();
            Debug.WriteLine(this, "Engine initialized");
        }

        internal void UnInitialize()
        {
            Debug.WriteLine(this, "Aborting engine thread");
            _engineThread.Abort();
            _engineThread.Join();
            EngineState = TEngineState.NotInitialized;

            var chPRI = PlayoutChannelPRI;
            var chSEC = PlayoutChannelSEC;
            if (chSEC != null
                && chSEC != chPRI)
                chSEC.OwnerServer.PropertyChanged -= _server_PropertyChanged;
            if (chPRI != null)
                chPRI.OwnerServer.PropertyChanged -= _server_PropertyChanged;

            if (Remote != null)
            {
                Debug.WriteLine(this, "UnInitializing Remote interface");
                Remote.UnInitialize(this);
            }
            var localGpi = LocalGpi;
            if (localGpi != null)
                localGpi.Started -= StartLoaded;

            var gpi = _gpi;
            if (gpi != null)
            {
                Debug.WriteLine(this, "Uninitializing GPI");
                gpi.Started -= StartLoaded;
                gpi.UnInitialize();
                gpi.PropertyChanged -= GPI_PropertyChanged;
            }

            Debug.WriteLine(this, "Engine uninitialized");
        }

        #region Database
        private void _database_ConnectionStateChanged(object sender, RedundantConnectionStateEventArgs e)
        {
            var h = DatabaseConnectionStateChanged;
            if (h != null)
                h(this, e);
        }

        private void _server_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action<CasparServerChannel, List<Event>> channelConnected = (channel, ve) =>
            {
                foreach (Event ev in ve)
                {
                    channel.ReStart(ev);
                    channel.SetVolume(VideoLayer.Program, _programAudioVolume, 0);
                    if (ev.Layer == VideoLayer.Program || ev.Layer == VideoLayer.Preset)
                    {
                        IMedia media = ev.Media;
                        bool narrow = media != null && (media.VideoFormat == TVideoFormat.PAL || media.VideoFormat == TVideoFormat.NTSC || media.VideoFormat == TVideoFormat.PAL_P);
                        if (AspectRatioControl == TAspectRatioControl.ImageResize || AspectRatioControl == TAspectRatioControl.GPIandImageResize)
                            channel.SetAspect(VideoLayer.Program, narrow);
                    }
                }
            };

            if (e.PropertyName == "IsConnected" && ((IPlayoutServer)sender).IsConnected)
            {
                var ve = _visibleEvents.ToList();
                if (PlayoutChannelPRI != null
                    && sender == PlayoutChannelPRI.OwnerServer)
                    channelConnected(_playoutChannelPRI, ve);
                if (PlayoutChannelSEC != null
                    && sender == PlayoutChannelSEC.OwnerServer
                    && PlayoutChannelSEC != PlayoutChannelPRI)
                    channelConnected(_playoutChannelSEC, ve);
            }
        }



        public ConnectionStateRedundant DatabaseConnectionState
        {
            get { return Database.Database.ConnectionState; }
        }

        public event StateRedundantChangeEventHandler DatabaseConnectionStateChanged;
        #endregion //Database

        #region GPI

        void GPI_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged("GPI" + e.PropertyName);
        }

        public bool GPIConnected
        {
            get { return _gpi != null && _gpi.Connected; }
        }

        private bool _gPIEnabled = true;
        [XmlIgnore]
        public bool GPIEnabled
        {
            get { return _gPIEnabled; }
            set { SetField(ref _gPIEnabled, value, "GPIEnabled"); }
        }

        [XmlIgnore]
        public bool GPIAspectNarrow
        {
            get { return _gpi != null && _gpi.AspectNarrow; }
            set { if (_gpi != null && _gPIEnabled) _gpi.AspectNarrow = value; }
        }

        [XmlIgnore]
        public TCrawl GPICrawl
        {
            get { return _gpi == null ? TCrawl.NoCrawl : (TCrawl)_gpi.Crawl; }
            set { if (_gpi != null && _gPIEnabled) _gpi.Crawl = (int)value; }
        }

        [XmlIgnore]
        public TLogo GPILogo
        {
            get { return _gpi == null ? TLogo.NoLogo : (TLogo)_gpi.Logo; }
            set { if (_gpi != null && _gPIEnabled) _gpi.Logo = (int)value; }
        }

        [XmlIgnore]
        public TParental GPIParental
        {
            get { return _gpi == null ? TParental.None : (TParental)_gpi.Parental; }
            set { if (_gpi != null && _gPIEnabled) _gpi.Parental = (int)value; }
        }

        [XmlIgnore]
        public bool GPIIsMaster
        {
            get { return _gpi != null && _gpi.IsMaster; }
        }
        #endregion // GPI

        #region FixedStartEvents

        readonly SynchronizedCollection<IEvent> _fixedTimeEvents = new SynchronizedCollection<IEvent>();
        internal void AddFixedTimeEvent(Event e)
        {
            _fixedTimeEvents.Add(e);
            FixedTimeEventOperation?.Invoke(this, new CollectionOperationEventArgs<IEvent>(e, TCollectionOperation.Insert));
        }
        internal void RemoveFixedTimeEvent(Event e)
        {
            if (_fixedTimeEvents.Remove(e))
            {
                FixedTimeEventOperation?.Invoke(this, new CollectionOperationEventArgs<IEvent>(e, TCollectionOperation.Remove));
            }
        }

        [XmlIgnore]
        public List<IEvent> FixedTimeEvents { get { lock(_fixedTimeEvents.SyncRoot) return _fixedTimeEvents.ToList(); } }

        public event EventHandler<CollectionOperationEventArgs<IEvent>> FixedTimeEventOperation;

        #endregion // FixedStartEvents

        public TArchivePolicyType ArchivePolicy;
                
        private Event _playing;
        [XmlIgnore]
        public IEvent Playing
        {
            get { return _playing; }
            private set
            {
                var oldPlaying = _playing;
                if (SetField(ref _playing, (Event)value, "Playing"))
                {
                    if (oldPlaying != null)
                        oldPlaying.SubEventChanged -= _playingSubEventsChanged;
                    if (value != null)
                    {
                        value.SubEventChanged += _playingSubEventsChanged;
                        var media = value.Media;
                        SetField(ref _fieldOrderInverted, media == null ? false : media.FieldOrderInverted, "FieldOrderInverted");
                    }
                }
            }
        }

        public IEvent NextToPlay
        {
            get
            {
                var e = _playing as Event;
                if (e != null)
                {
                    e = _successor(e);
                    if (e != null)
                    {
                        if (e.EventType == TEventType.Rundown)
                            return e.FindVisibleSubEvent();
                        else
                            return e;
                    }
                }
                return null;
            }
        }

        public IEvent NextWithRequestedStartTime
        {
            get
            {
                IEvent e = _playing;
                if (e != null)
                    do
                        e = e.GetSuccessor();
                    while (e != null && e.RequestedStartTime == null);
                return e;
            }
        }
    
        #region Preview Routines

        private IMedia _previewMedia;
        public  IMedia PreviewMedia { get { return _previewMedia; } }

        public void PreviewLoad(IMedia media, long seek, long duration, long position, decimal previewAudioVolume)
        {
            Media mediaToLoad = FindPreviewMedia(media);
            if (mediaToLoad != null)
            {
                _previewDuration = duration;
                _previewSeek = seek;
                _previewPosition = position;
                _previewMedia = media;
                _playoutChannelPRV.SetAspect(VideoLayer.Preview, media.VideoFormat == TVideoFormat.NTSC
                                            || media.VideoFormat == TVideoFormat.PAL
                                            || media.VideoFormat == TVideoFormat.PAL_P);
                PreviewLoaded = true;
                PreviewAudioLevel = previewAudioVolume;
                _playoutChannelPRV.Load(mediaToLoad, VideoLayer.Preview, seek+position, duration-position);
                PreviewIsPlaying = false;
                NotifyPropertyChanged("PreviewMedia");
                NotifyPropertyChanged("PreviewPosition");
                NotifyPropertyChanged("PreviewSeek");
            }
        }

        public void PreviewUnload()
        {
            var channel = _playoutChannelPRV;
            if (channel != null)
            {
                if (_previewMedia != null)
                {
                    channel.Clear(VideoLayer.Preview);
                    _previewDuration = 0;
                    _previewPosition = 0;
                    _previewSeek = 0;
                    _previewMedia = null;
                    PreviewLoaded = false;
                    PreviewIsPlaying = false;
                    NotifyPropertyChanged("PreviewMedia");
                    NotifyPropertyChanged("PreviewPosition");
                    NotifyPropertyChanged("PreviewSeek");
                }
            }
        }

        private long _previewDuration;

        private long _previewPosition;

        private long _previewSeek;
        [XmlIgnore]
        public long PreviewSeek { get { return _previewSeek; } }

        [XmlIgnore]
        public long PreviewPosition // from 0 to duration
        {
            get { return _previewPosition; }
            set
            {
                if (_playoutChannelPRV != null && _previewMedia!=null)
                {
                    PreviewPause();
                    long newSeek = value < 0 ? 0 : value;
                    long maxSeek = _previewDuration-1;
                    if (newSeek > maxSeek)
                        newSeek = maxSeek;
                    if (newSeek != _previewPosition)
                    {
                        _playoutChannelPRV.Seek(VideoLayer.Preview, _previewSeek + newSeek);
                        _previewPosition = newSeek;
                    }
                }
            }
        }

        private decimal _previewAudioLevel;
        [XmlIgnore]
        public decimal PreviewAudioLevel
        {
            get { return _previewAudioLevel; }
            set
            {
                if (SetField(ref _previewAudioLevel, value, "PreviewAudioLevel"))
                    _playoutChannelPRV.SetVolume(VideoLayer.Preview, (decimal)Math.Pow(10, (double)value / 20), 0);
            }
        }

        public bool PreviewPlay()
        {
            var channel = _playoutChannelPRV;
            var media = PreviewMedia;
            if (channel != null && channel.Play(VideoLayer.Preview) && media != null)
            {
                PreviewIsPlaying = true;
                return true;
            }
            else
                return false;
        }

        public bool PreviewPause()
        {
            var channel = _playoutChannelPRV;
            if (channel != null && channel.Pause(VideoLayer.Preview))
            {
                PreviewIsPlaying = false;
                return true;
            }
            else
                return false;
        }

        private bool _previewLoaded;

        [XmlIgnore]
        public bool PreviewLoaded {
            get { return _previewLoaded; }
            private set
            {
                if (SetField(ref _previewLoaded, value, "PreviewLoaded"))
                {
                    decimal vol = (_previewLoaded) ? 0 : _programAudioVolume;
                    if (_playoutChannelPRV != null)
                        _playoutChannelPRV.SetVolume(VideoLayer.Program, vol, 0);
                }
            }
        }

        private bool _previewIsPlaying;
        [XmlIgnore]
        public bool PreviewIsPlaying { get { return _previewIsPlaying; } private set { SetField(ref _previewIsPlaying, value, "PreviewIsPlaying"); } }

        public Media FindPreviewMedia(IMedia media)
        {
            IPlayoutServerChannel playoutChannel = _playoutChannelPRV;
            if (media is ServerMedia)
            {
                if (media == null || playoutChannel == null)
                    return (Media)media;
                else
                    return ((ServerDirectory)playoutChannel.OwnerServer.MediaDirectory).FindMediaByMediaGuid(media.MediaGuid);
            }
            else
                return (Media)media;
        }

        //[JsonProperty]
        public VideoFormatDescription PreviewFormatDescription { get
            {
                return this.FormatDescription;
            }
        }

        #endregion // Preview Routines

        #region private methods
        private bool _load(Event aEvent)
        {
            if (aEvent != null && (!aEvent.IsEnabled || aEvent.Length == TimeSpan.Zero))
                aEvent = (Event)aEvent.GetSuccessor();
            if (aEvent == null)
                return false;
            Debug.WriteLine("{0} Load: {1}", CurrentTime.TimeOfDay.ToSMPTETimecodeString(_frameRate), aEvent);
            var eventType = aEvent.EventType;
            if (eventType == TEventType.Live || eventType == TEventType.Movie || eventType == TEventType.StillImage)
            {
                if (PlayoutChannelPRI != null)
                    _playoutChannelPRI.Load(aEvent);
                if (PlayoutChannelSEC != null)
                    _playoutChannelSEC.Load(aEvent);
                _visibleEvents.Add(aEvent);
                if (aEvent.Layer == VideoLayer.Program)
                    Playing = aEvent;
            }
            _run(aEvent);
            aEvent.PlayState = TPlayState.Paused;
            NotifyEngineOperation(aEvent, TEngineOperation.Load);
            foreach (Event se in (aEvent.SubEvents.Where(e => e.ScheduledDelay == TimeSpan.Zero)))
                _load(se);
            return true;
        }

        private bool _loadNext(Event aEvent)
        {
            if (aEvent != null && (!aEvent.IsEnabled || aEvent.Length == TimeSpan.Zero))
                aEvent = aEvent.GetSuccessor() as Event;
            if (aEvent == null)
                return false;
            var eventType = aEvent.EventType;
            Event preloaded;
            if ((eventType == TEventType.Live || eventType == TEventType.Movie || eventType == TEventType.StillImage) && 
                !(_preloadedEvents.TryGetValue(aEvent.Layer, out preloaded) && preloaded == aEvent))
            {
                Debug.WriteLine("{0} LoadNext: {1}", CurrentTime.TimeOfDay.ToSMPTETimecodeString(_frameRate), aEvent);
                _preloadedEvents[aEvent.Layer] = aEvent;
                if (_playoutChannelPRI != null)
                    _playoutChannelPRI.LoadNext(aEvent);
                if (_playoutChannelSEC != null)
                    _playoutChannelSEC.LoadNext(aEvent);
                if (!aEvent.IsHold
                    && _gpi != null
                    && GPIEnabled
                    && _gpi.GraphicsStartDelay < 0)
                {
                    ThreadPool.QueueUserWorkItem(o =>
                    {
                        Thread.Sleep(_preloadTime + TimeSpan.FromMilliseconds(_gpi.GraphicsStartDelay));
                        _setGPIGraphics(_gpi, aEvent);
                    });
                }
            }
            if (aEvent.SubEventsCount > 0)
                foreach (Event se in aEvent.SubEvents)
                {
                    se.PlayState = TPlayState.Scheduled;
                    if (se.ScheduledDelay < _preloadTime)
                        _loadNext(se);
                }
            _run(aEvent);
            return true;
        }

        private bool _play(Event aEvent, bool fromBeginning)
        {
            var eventType = aEvent.EventType;
            if (aEvent != null && (!aEvent.IsEnabled || (aEvent.Length == TimeSpan.Zero && eventType != TEventType.Animation)))
                aEvent = aEvent.GetSuccessor() as Event;
            if (aEvent == null)
                return false;
            Debug.WriteLine("{0} Play: {1}", CurrentTime.TimeOfDay.ToSMPTETimecodeString(_frameRate), aEvent);
            if (aEvent == _forcedNext)
            {
                ForcedNext = null;
                _runningEvents.ToList().ForEach(
                    e =>
                    {
                        if (e.PlayState == TPlayState.Playing)
                        {
                            e.PlayState = e.IsFinished ? TPlayState.Played : TPlayState.Aborted;
                            _runningEvents.Remove(e);
                        }
                        e.Save();
                    });                        
            }
            _run(aEvent);
            if (fromBeginning)
                aEvent.Position = 0;
            if (eventType == TEventType.Live || eventType == TEventType.Movie || eventType == TEventType.StillImage)
            {
                if (_playoutChannelPRI != null)
                    _playoutChannelPRI.Play(aEvent);
                if (_playoutChannelSEC != null)
                    _playoutChannelSEC.Play(aEvent);
                _visibleEvents.Add(aEvent);
                if (aEvent.Layer == VideoLayer.Program)
                {
                    Playing = aEvent;
                    ProgramAudioVolume = (decimal)Math.Pow(10, (double)aEvent.GetAudioVolume() / 20); ;
                    _setAspectRatio(aEvent);
                    if (LocalGpi != null && GPIEnabled)
                        _setGPIGraphics(LocalGpi, aEvent);
                    if (_gpi != null && GPIEnabled)
                    {
                        if (_gpi.GraphicsStartDelay <= 0)
                            _setGPIGraphics(_gpi, aEvent);
                        else
                        {
                            ThreadPool.QueueUserWorkItem(o =>
                            {
                                Thread.Sleep(_gpi.GraphicsStartDelay);
                                _setGPIGraphics(_gpi, aEvent);
                            });
                        }
                    }
                }
                Event removed;
                _preloadedEvents.TryRemove(aEvent.Layer, out removed);
            }
            if (eventType == TEventType.Animation)
            {
                if (_playoutChannelPRI != null)
                    _playoutChannelPRI.Play(aEvent);
                if (_playoutChannelSEC != null)
                    _playoutChannelSEC.Play(aEvent);
                aEvent.PlayState = TPlayState.Played;
            }
            else
            {
                aEvent.PlayState = TPlayState.Playing;
                if (aEvent.SubEventsCount > 0)
                    foreach (Event se in aEvent.SubEvents)
                        if (se.ScheduledDelay == TimeSpan.Zero)
                            _play(se, fromBeginning);
            }
            aEvent.Save();
            if (_pst2Prv)
                _loadPST();
            NotifyEngineOperation(aEvent, TEngineOperation.Play);
            if (aEvent.Layer == VideoLayer.Program
                && (aEvent.EventType == TEventType.Movie || aEvent.EventType == TEventType.Live))
                ThreadPool.QueueUserWorkItem(o => aEvent.AsRunLogWrite());
            return true;
        }

        private void _clearRunning()
        {
            Debug.WriteLine("_clearRunning");
            foreach (Event e in _runningEvents.ToList())
            {
                if (e.PlayState == TPlayState.Playing)
                    e.PlayState = TPlayState.Aborted;
                if (e.PlayState == TPlayState.Fading)
                    e.PlayState = TPlayState.Played;
                if (e.PlayState == TPlayState.Paused)
                    e.PlayState = TPlayState.Scheduled;
                if (e.Modified)
                    e.Save();
            }
            _runningEvents.Clear();
        }

        private void _setAspectRatio(IEvent aEvent)
        {
            if (aEvent == null || !(aEvent.Layer == VideoLayer.Program || aEvent.Layer == VideoLayer.Preset))
                return;
            IMedia media = aEvent.Media;
            bool narrow = media != null && (media.VideoFormat == TVideoFormat.PAL || media.VideoFormat == TVideoFormat.NTSC || media.VideoFormat == TVideoFormat.PAL_P);
            if (AspectRatioControl == TAspectRatioControl.ImageResize || AspectRatioControl == TAspectRatioControl.GPIandImageResize)
            {
                if (_playoutChannelPRI != null)
                    _playoutChannelPRI.SetAspect(aEvent.Layer, narrow);
                if (_playoutChannelSEC != null)
                    _playoutChannelSEC.SetAspect(aEvent.Layer, narrow);
            }
            if (AspectRatioControl == TAspectRatioControl.GPI || AspectRatioControl == TAspectRatioControl.GPIandImageResize)
                if (_gpi != null)
                    _gpi.AspectNarrow = narrow;
        }

        private void _run(Event aEvent)
        {
            if (aEvent == null || aEvent.EventType == TEventType.Animation)
                return;
            lock (_runningEvents.SyncRoot)
            {
                if (!_runningEvents.Contains(aEvent))
                    _runningEvents.Add(aEvent);
            }
        }

        private void _stop(Event aEvent)
        {
            aEvent.PlayState = aEvent.Position == 0 ? TPlayState.Scheduled : aEvent.IsFinished ? TPlayState.Played : TPlayState.Aborted;
            aEvent.Save();
            lock (_visibleEvents.SyncRoot)
                if (_visibleEvents.Contains(aEvent))
                {
                    if (aEvent.EventType != TEventType.Live)
                    {
                        Debug.WriteLine("{0} Stop: {1}", CurrentTime.TimeOfDay.ToSMPTETimecodeString(_frameRate), aEvent);
                        if (_playoutChannelPRI != null)
                            _playoutChannelPRI.Stop(aEvent);
                        if (_playoutChannelSEC != null)
                            _playoutChannelSEC.Stop(aEvent);
                    }
                    _visibleEvents.Remove(aEvent);
                }
            _runningEvents.Remove(aEvent);
            NotifyEngineOperation(aEvent, TEngineOperation.Stop);
        }

        private void _pause(Event aEvent, bool finish)
        {
            lock (_visibleEvents)
                if (_visibleEvents.Contains(aEvent))
                {
                    Debug.WriteLine("{0} Pause: {1}", CurrentTime.TimeOfDay.ToSMPTETimecodeString(_frameRate), aEvent);
                    if (aEvent.EventType != TEventType.Live && aEvent.EventType != TEventType.StillImage)
                    {
                        if (_playoutChannelPRI != null)
                            _playoutChannelPRI.Pause(aEvent);
                        if (_playoutChannelSEC != null)
                            _playoutChannelSEC.Pause(aEvent);
                    }
                    foreach (Event se in aEvent.SubEvents)
                        _pause(se, finish);
                }
            if (finish)
            {
                aEvent.PlayState = TPlayState.Played;
                aEvent.Save();
                _runningEvents.Remove(aEvent);
                NotifyEngineOperation(aEvent, TEngineOperation.Stop);
            }
            else
                NotifyEngineOperation(aEvent, TEngineOperation.Pause);
        }

        private void _loadPST()
        {
            IEvent ev = NextToPlay;
            if (ev != null && PlayoutChannelPRV != null)
            {
                Media media = ((Event)ev).ServerMediaPRV;
                if (media != null)
                {
                    _playoutChannelPRV.Load(media, VideoLayer.Preset, 0, -1);
                    return;
                }
            }
            _playoutChannelPRV.Load(System.Drawing.Color.Black, VideoLayer.Preset);
        }

        private void _restartEvent(Event ev)
        {
            if (_playoutChannelPRI != null)
                _playoutChannelPRI.ReStart(ev);
            if (_playoutChannelSEC != null)
                _playoutChannelSEC.ReStart(ev);
        }

        private object _tickLock = new object();
        private void _tick(long nFrames)
        {
            lock (_tickLock)
            {
                if (EngineState == TEngineState.Running)
                {
                    lock (_runningEvents.SyncRoot)
                        foreach (IEvent e in _runningEvents.Where(ev => ev.PlayState == TPlayState.Playing || ev.PlayState == TPlayState.Fading))
                            e.Position += nFrames;

                    Event playingEvent = _playing;
                    Event succEvent = null;
                    if (playingEvent != null)
                    {
                        succEvent = _successor(playingEvent);
                        if (succEvent != null)
                        {
                            if (playingEvent.Position * _frameTicks >= playingEvent.Duration.Ticks - succEvent.TransitionTime.Ticks)
                            {
                                if (playingEvent.PlayState == TPlayState.Playing)
                                    playingEvent.PlayState = TPlayState.Fading;
                            }
                            if (playingEvent.Position * _frameTicks >= playingEvent.Duration.Ticks - _preloadTime.Ticks)
                                _loadNext(succEvent);
                            if (playingEvent.Position * _frameTicks >= playingEvent.Duration.Ticks - succEvent.TransitionTime.Ticks)
                            {
                                if (succEvent.IsHold && succEvent != _forcedNext)
                                    EngineState = TEngineState.Hold;
                                else
                                    _play(succEvent, true);
                            }
                        }
                        playingEvent = _playing; // in case when succEvent just started 
                        if (playingEvent != null && playingEvent.SubEventsCount > 0)
                        {
                            TimeSpan playingEventPosition = TimeSpan.FromTicks(playingEvent.Position * _frameTicks);
                            var sel = playingEvent.SubEvents.Where(e => e.PlayState == TPlayState.Scheduled);
                            foreach (Event se in sel)
                            {
                                Event preloaded;
                                if (playingEventPosition >= se.ScheduledDelay - _preloadTime - se.TransitionTime
                                    && !(_preloadedEvents.TryGetValue(se.Layer, out preloaded) && se == preloaded))
                                    _loadNext(se);
                                if (playingEventPosition >= se.ScheduledDelay - se.TransitionTime)
                                    _play(se, true);
                            }
                        }
                    }

                    IEnumerable<Event> runningEvents;
                    lock (_runningEvents.SyncRoot)
                        runningEvents = _runningEvents.Where(e => e.PlayState == TPlayState.Playing || e.PlayState == TPlayState.Fading).ToList();
                    foreach (Event e in runningEvents)
                        if (e.IsFinished)
                        {
                            if (succEvent == null)
                                _pause(e, true);
                            else
                                _stop(e);
                        }

                    if (_runningEvents.Count == 0)
                        EngineState = TEngineState.Idle;
                }
                var currentTimeOfDayTicks = CurrentTime.TimeOfDay.Ticks;
                lock (_fixedTimeEvents.SyncRoot)
                {
                    var startEvent = _fixedTimeEvents.FirstOrDefault(e =>
                                                                      e.StartType == TStartType.OnFixedTime
                                                                   && (EngineState == TEngineState.Idle || (e.AutoStartFlags & AutoStartFlags.Force) != AutoStartFlags.None)
                                                                   && (e.PlayState == TPlayState.Scheduled || (e.PlayState != TPlayState.Playing && (e.AutoStartFlags & AutoStartFlags.Force) != AutoStartFlags.None))
                                                                   && e.IsEnabled
                                                                   && ((e.AutoStartFlags & AutoStartFlags.Daily) != AutoStartFlags.None ?
                                                                        currentTimeOfDayTicks >= e.ScheduledTime.TimeOfDay.Ticks && currentTimeOfDayTicks < e.ScheduledTime.TimeOfDay.Ticks + TimeSpan.TicksPerSecond :
                                                                        CurrentTicks >= e.ScheduledTime.Ticks && CurrentTicks < e.ScheduledTime.Ticks + TimeSpan.TicksPerSecond) // auto start only within 1 second slot
                        );
                    if (startEvent != null)
                        Start(startEvent);
                }
                // preview controls
                if (PreviewIsPlaying)
                {
                    if (_previewPosition < _previewDuration - 1)
                    {
                            _previewPosition += nFrames;
                            NotifyPropertyChanged("PreviewPosition");
                    }
                    else
                        PreviewPause();
                }
            }
        }

        private Event _successor(Event playingEvent)
        {
            Event result = _forcedNext as Event;
            if (result == null)
            {
                if (playingEvent != null)
                {
                    result = playingEvent.IsLoop ? playingEvent : playingEvent.GetSuccessor() as Event;
                    if (result == null)
                        result = playingEvent.GetVisualRootTrack().FirstOrDefault(e => e.IsLoop) as Event;
                }
            }
            return result;
        }

        private void _setGPIGraphics(IGpi gpi, Event ev)
        {
            if (ev.GPI.CanTrigger)
            {
                gpi.Crawl = (int)ev.GPI.Crawl;
                gpi.Logo = (int)ev.GPI.Logo;
                gpi.Parental = (int)ev.GPI.Parental;
            }
        }

        private void _playingSubEventsChanged(object sender, CollectionOperationEventArgs<IEvent> e)
        {
            if (_playing != sender)
                return;
            if (e.Operation == TCollectionOperation.Remove)
                _stop((Event)e.Item);
            else
            {
                lock (_tickLock)
                {
                    TPlayState ps = ((Event)sender).PlayState;
                    if ((ps == TPlayState.Playing || ps == TPlayState.Paused)
                        && e.Item.PlayState == TPlayState.Scheduled)
                    {
                        e.Item.Position = ((Event)sender).Position;
                        if (ps == TPlayState.Paused)
                        {
                            if (e.Item.EventType == TEventType.StillImage)
                                _load(e.Item as Event);
                        }
                        else
                            _play(e.Item as Event, false);
                    }
                }
            }
        }

        private TimeSpan _getTimeToAttention()
        {
            IEvent pe = _playing;
            if (pe != null && (pe.PlayState == TPlayState.Playing || pe.PlayState == TPlayState.Paused))
            {
                TimeSpan result = pe.Length - TimeSpan.FromTicks(pe.Position * _frameTicks);
                pe = pe.GetSuccessor();
                while (pe != null)
                {
                    TimeSpan? pauseTime = pe.GetAttentionTime();
                    if (pauseTime != null)
                        return result + pauseTime.Value - pe.TransitionTime;
                    result = result + pe.Length - pe.TransitionTime;
                    pe = pe.GetSuccessor();
                }
                return result;
            }
            return TimeSpan.Zero;
        }

        [XmlIgnore]
        public DateTime CurrentTime { get; private set; }

        public bool DateTimeEqal(DateTime dt1, DateTime dt2)
        {
            return AlignDateTime(dt1) == AlignDateTime(dt2);
        }

        public DateTime AlignDateTime(DateTime dt)
        {
            return new DateTime((dt.Ticks / _frameTicks) * _frameTicks, dt.Kind);
        }

        public TimeSpan AlignTimeSpan(TimeSpan ts)
        {
            return new TimeSpan((ts.Ticks / _frameTicks) * _frameTicks);
        }

        public override string ToString()
        {
            return EngineName;
        }

        #endregion // private methods

        #region IEngine methods

        public void Load(IEvent aEvent)
        {
            Debug.WriteLine(aEvent, "Load");
            lock (_tickLock)
            {
                EngineState = TEngineState.Hold;
                foreach (Event e in _visibleEvents.ToList())
                    _stop(e);
                foreach (Event e in _runningEvents.ToList())
                {
                    _runningEvents.Remove(e);
                    if (e.Position == 0)
                        e.PlayState = TPlayState.Scheduled;
                    else
                        e.PlayState = TPlayState.Aborted;
                }
            }
            _load(aEvent as Event);
        }

        public void StartLoaded()
        {
            Debug.WriteLine("StartLoaded executed");
            lock (_tickLock)
                if (EngineState == TEngineState.Hold)
                {
                    _visibleEvents.Where(e => e.PlayState == TPlayState.Played).ToList().ForEach(e => _stop(e));                    
                    foreach (Event e in _runningEvents.ToList())
                    {
                        if (!(e.PlayState == TPlayState.Playing || e.PlayState == TPlayState.Fading))
                        {
                            _play(e, false);
                            IEvent s = e.GetSuccessor();
                            if (s != null)
                                s.UpdateScheduledTime(true);
                        }
                    }
                    EngineState = TEngineState.Running;
                }
        }

        public void Start(IEvent aEvent)
        {
            Debug.WriteLine(aEvent, "Start");
            lock (_tickLock)
            {
                EngineState = TEngineState.Running;
                foreach (Event e in _visibleEvents.ToList())
                    _stop(e);
                foreach (Event e in _runningEvents.ToList())
                {
                    _runningEvents.Remove(e);
                    if (e.Position == 0)
                        e.PlayState = TPlayState.Scheduled;
                    else
                        e.PlayState = TPlayState.Aborted;
                }
                _play(aEvent as Event, true);
            }
        }

        public void Schedule(IEvent aEvent)
        {
            Debug.WriteLine(aEvent, string.Format("Schedule {0}", aEvent.PlayState));
            lock (_tickLock)
                EngineState = TEngineState.Running;
            _run((Event)aEvent);
            NotifyEngineOperation(aEvent, TEngineOperation.Schedule);
        }

        [XmlIgnore]
        public IEvent ForcedNext
        {
            get { return _forcedNext; }
            set
            {

                lock (_tickLock)
                {
                    var oldForcedNext = _forcedNext as Event;
                    if (SetField(ref _forcedNext, value, "ForcedNext"))
                    {
                        Debug.WriteLine(value, "ForcedNext");
                        NotifyPropertyChanged("NextToPlay");
                        if (value != null)
                            ((Event)value).IsForcedNext = true;
                        if (oldForcedNext != null)
                            oldForcedNext.IsForcedNext = false;
                    }
                }
            }
        }

        public void Clear(VideoLayer aVideoLayer)
        {
            Debug.WriteLine(aVideoLayer, "Clear");
            Event ev;
            lock (_visibleEvents.SyncRoot)
                ev = _visibleEvents.FirstOrDefault(e => e.Layer == aVideoLayer);
            if (ev != null)
            {
                ev.PlayState = TPlayState.Aborted;
                ev.Save();
                _visibleEvents.Remove(ev);
                _runningEvents.Remove(ev);
            }
            if (ev != null)
            {
                ev.PlayState = TPlayState.Scheduled;
                ev.Save();
                _runningEvents.Remove(ev);
            }
            if (_playoutChannelPRI != null)
                _playoutChannelPRI.Clear(aVideoLayer);
            if (_playoutChannelSEC != null)
                _playoutChannelSEC.Clear(aVideoLayer);
            if (aVideoLayer == VideoLayer.Program)
                lock(_tickLock)
                    Playing = null;
        }
        
        public void Clear()
        {
            _clearRunning();
            _visibleEvents.Clear();
            ForcedNext = null;
            if (_playoutChannelPRI != null)
                _playoutChannelPRI.Clear();
            if (_playoutChannelSEC != null)
                _playoutChannelSEC.Clear();
            PreviewUnload();
            NotifyEngineOperation(null, TEngineOperation.Clear);
            ProgramAudioVolume = 1.0m;
            lock (_tickLock)
            {
                EngineState = TEngineState.Idle;
                Playing = null;
            }
        }

        public void Restart()
        {
            foreach (Event e in _visibleEvents.ToList())
                _restartEvent(e);
        }

        public void RestartRundown(IEvent ARundown)
        {
            Action<Event> _rerun = (aEvent) =>
                {
                    if (!_runningEvents.Contains(aEvent))
                        _runningEvents.Add(aEvent);
                    if (aEvent.EventType != TEventType.Rundown)
                    {
                        _visibleEvents.Add(aEvent);
                        _restartEvent(aEvent);
                    }
                };

            Event ev = ARundown as Event;
            while (ev != null)
            {
                if (CurrentTicks >= ev.ScheduledTime.Ticks && CurrentTicks < ev.ScheduledTime.Ticks + ev.Duration.Ticks)
                {
                    ev.Position = (CurrentTicks - ev.ScheduledTime.Ticks) / FrameTicks;
                    var st = ev.StartTime;
                    ev.PlayState = TPlayState.Playing;
                    if (st != ev.StartTime)
                        ev.StartTime = st;
                    _rerun(ev);
                    foreach (Event se in ev.SubEvents)
                        RestartRundown(se);
                    break;
                }
                else
                    ev = ev.GetSuccessor() as Event;
            }
            lock (_tickLock)
                EngineState = TEngineState.Running;
        }

        
        public MediaDeleteDenyReason CanDeleteMedia(PersistentMedia media)
        {
            MediaDeleteDenyReason reason = MediaDeleteDenyReason.NoDeny;
            if (media is PersistentMedia && ((PersistentMedia)media).Protected)
                return new MediaDeleteDenyReason() { Reason = MediaDeleteDenyReason.MediaDeleteDenyReasonEnum.Protected, Media = media };
            ServerMedia serverMedia = media as ServerMedia;
            if (serverMedia == null)
                return reason;
            else
            {
                foreach (Event e in _rootEvents.ToList())
                {
                    reason = e.CheckCanDeleteMedia(serverMedia);
                    if (reason.Reason != MediaDeleteDenyReason.MediaDeleteDenyReasonEnum.NoDeny)
                        return reason;
                }
                return this.DbMediaInUse(serverMedia);
            }
        }

        [XmlIgnore]
        public SynchronizedCollection<IEvent> RootEvents { get { return _rootEvents; } }

        public IEvent AddNewEvent(
                    UInt64 idRundownEvent = 0,
                    UInt64 idEventBinding = 0,
                    VideoLayer videoLayer = VideoLayer.None,
                    TEventType eventType = TEventType.Rundown,
                    TStartType startType = TStartType.None,
                    TPlayState playState = TPlayState.Scheduled,
                    DateTime scheduledTime = default(DateTime),
                    TimeSpan duration = default(TimeSpan),
                    TimeSpan scheduledDelay = default(TimeSpan),
                    TimeSpan scheduledTC = default(TimeSpan),
                    Guid mediaGuid = default(Guid),
                    string eventName = "",
                    DateTime startTime = default(DateTime),
                    TimeSpan startTC = default(TimeSpan),
                    TimeSpan? requestedStartTime = null,
                    TimeSpan transitionTime = default(TimeSpan),
                    TimeSpan transitionPauseTime = default(TimeSpan), 
                    TTransitionType transitionType = TTransitionType.Cut,
                    TEasing transitionEasing = TEasing.Linear,
                    decimal? audioVolume = null,
                    UInt64 idProgramme = 0,
                    string idAux = "",
                    bool isEnabled = true,
                    bool isHold = false,
                    bool isLoop = false,
                    EventGPI gpi = default(EventGPI),
                    AutoStartFlags autoStartFlags = AutoStartFlags.None
                    )
        {
            IEvent result;
            if (!_events.TryGetValue(idRundownEvent, out result))
            {
                if (eventType == TEventType.Animation)
                    result = new AnimatedEvent(this, idRundownEvent, idEventBinding, videoLayer, startType, playState, scheduledTime, duration, scheduledDelay, mediaGuid, eventName, startTime, isEnabled, gpi);
                else
                    result = new Event(this, idRundownEvent, idEventBinding, videoLayer, eventType, startType, playState, scheduledTime, duration, scheduledDelay, scheduledTC, mediaGuid, eventName, startTime, startTC, requestedStartTime, transitionTime, transitionPauseTime, transitionType, transitionEasing, audioVolume, idProgramme, idAux, isEnabled, isHold, isLoop, gpi, autoStartFlags);
                if (idRundownEvent == 0)
                    result.Save();
                if (_events.TryAdd(result.IdRundownEvent, result))
                {
                    result.Saved += _eventSaved;
                    result.Deleted += _eventDeleted;
                }
                if (startType == TStartType.OnFixedTime)
                    _fixedTimeEvents.Add(result);
            }
            return result;
        }

        private void _removeEvent(IEvent aEvent)
        {
            _rootEvents.Remove(aEvent);
            IEvent eventToRemove;
            if (_events.TryRemove(aEvent.IdRundownEvent, out eventToRemove))
            {
                aEvent.Saved -= _eventSaved;
                aEvent.Deleted -= _eventDeleted;
            }
            if (aEvent.StartType == TStartType.OnFixedTime)
                RemoveFixedTimeEvent((Event)aEvent);
            var media = aEvent.Media as ServerMedia;
            if (media != null
                && aEvent.PlayState == TPlayState.Played
                && media.MediaType == TMediaType.Movie
                && ArchivePolicy == TArchivePolicyType.ArchivePlayedAndNotUsedWhenDeleteEvent
                && MediaManager.ArchiveDirectory != null
                && CanDeleteMedia(media).Reason == MediaDeleteDenyReason.MediaDeleteDenyReasonEnum.NoDeny)
                ThreadPool.QueueUserWorkItem(o => MediaManager.ArchiveMedia(new List<IServerMedia>(new [] { media }), true));
        }

        private TEngineState _engineState;
        [XmlIgnore]
        public TEngineState EngineState
        {
            get { return _engineState; }
            private set
            {
                lock (_runningEvents.SyncRoot)
                if (SetField(ref _engineState, value, "EngineState"))
                    {
                        if (value == TEngineState.Hold)
                            foreach (Event ev in _runningEvents.Where(e => (e.PlayState == TPlayState.Playing || e.PlayState == TPlayState.Fading) && e.IsFinished).ToList())
                            {
                                _pause(ev, true);
                                Debug.WriteLine(ev, "Hold: Played");
                            }
                        if (value == TEngineState.Idle && _runningEvents.Count > 0)
                        {
                            foreach (Event ev in _runningEvents.Where(e => (e.PlayState == TPlayState.Playing || e.PlayState == TPlayState.Fading) && e.IsFinished).ToList())
                            {
                                _pause(ev, true);
                                Debug.WriteLine(ev, "Idle: Played");
                            }
                        }
                    }
            }
        }

        private decimal _programAudioVolume = 1;
        [XmlIgnore]
        public decimal ProgramAudioVolume
        {
            get { return _programAudioVolume; }
            set
            {
                if (SetField(ref _programAudioVolume, value, "ProgramAudioVolume"))
                {
                    var playing = Playing;
                    if (playing != null)
                    {
                        int transitioDuration = (int)playing.TransitionTime.ToSMPTEFrames(_frameRate);
                        if (_playoutChannelPRI != null)
                            _playoutChannelPRI.SetVolume(VideoLayer.Program, value, transitioDuration);
                        if (_playoutChannelSEC != null && !_previewLoaded)
                            _playoutChannelSEC.SetVolume(VideoLayer.Program, value, transitioDuration);
                    }
                }
            }
        }

        bool _fieldOrderInverted;
        [XmlIgnore]
        public bool FieldOrderInverted
        {
            get { return _fieldOrderInverted; }
            set { if (SetField(ref _fieldOrderInverted, value, "FieldOrderInverted"))
                {
                    if (_playoutChannelPRI != null)
                        _playoutChannelPRI.SetFieldOrderInverted(VideoLayer.Program, value);
                    if (_playoutChannelSEC != null && !_previewLoaded)
                        _playoutChannelSEC.SetFieldOrderInverted(VideoLayer.Program, value);
                }
            }
        }


        public void ReScheduleAsync(IEvent aEvent)
        {
            ThreadPool.QueueUserWorkItem(o => ReSchedule(aEvent as Event));
        }

        public object RundownSync = new object();

        public void ReSchedule(Event aEvent)
        {
            lock (RundownSync)
            {
                if (aEvent == null)
                    return;
                try
                {
                    if (aEvent.PlayState == TPlayState.Aborted
                        || aEvent.PlayState == TPlayState.Played)
                    {
                        aEvent.PlayState = TPlayState.Scheduled;
                        foreach (Event se in aEvent.SubEvents)
                            ReSchedule(se);
                    }
                    else
                        aEvent.UpdateScheduledTime(false);
                    Event ne = aEvent.Next as Event;
                    if (ne == null)
                        ne = aEvent.GetSuccessor() as Event;
                    ReSchedule(ne);
                }
                finally
                {
                    aEvent.Save();
                }
            }
        }


        #endregion // IEngine methods

        #region IEngine properties

        [XmlIgnore]
        public ICollection<IEvent> VisibleEvents { get { return _visibleEvents.Cast<IEvent>().ToList(); } }

        protected bool SetField<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            NotifyPropertyChanged(propertyName);
            return true;
        }

        #endregion // IEngine properties

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void NotifyEngineOperation(IEvent aEvent, TEngineOperation operation)
        {
            EngineOperation?.Invoke(this, new EngineOperationEventArgs(aEvent, operation));
        }

        public event EventHandler<CollectionOperationEventArgs<IEvent>> VisibleEventsOperation;
        private void _visibleEventsOperation(object o, CollectionOperationEventArgs<Event> e)
        {
            VisibleEventsOperation?.Invoke(o, new CollectionOperationEventArgs<IEvent>(e.Item, e.Operation));
        }

        public event EventHandler<CollectionOperationEventArgs<IEvent>> PreloadedEventsOperation;
        private void _loadedNextEventsOperation(object o, CollectionOperationEventArgs<IEvent> e)
        {
            PreloadedEventsOperation?.Invoke(o, e);
        }

        public event EventHandler<CollectionOperationEventArgs<IEvent>> RunningEventsOperation;
        private void _runningEventsOperation(object sender, CollectionOperationEventArgs<Event> e)
        {
            RunningEventsOperation?.Invoke(sender, new CollectionOperationEventArgs<IEvent>(e.Item, e.Operation));
        }

        public event EventHandler<IEventEventArgs> EventSaved; 
        private void _eventSaved(object sender, EventArgs e)
        {
            EventSaved?.Invoke(this, new IEventEventArgs(sender as IEvent));
        }

        public event EventHandler<IEventEventArgs> EventDeleted;
        private void _eventDeleted(object sender, EventArgs e)
        {
            _removeEvent(sender as Event);
            EventDeleted?.Invoke(this, new IEventEventArgs(sender as IEvent));
        }

        public void SearchMissingEvents()
        {
            this.DbSearchMissing();
        }

        private bool _pst2Prv;

        [XmlIgnore]
        public bool Pst2Prv
        {
            get { return _pst2Prv; }
            set
            {
                if (SetField(ref _pst2Prv, value, "Pst2Prv"))
                {
                    if (value)
                        _loadPST();
                    else
                        if (_playoutChannelPRV != null)
                            _playoutChannelPRV.Clear(VideoLayer.Preset);
                }
            }
        }

    }

}