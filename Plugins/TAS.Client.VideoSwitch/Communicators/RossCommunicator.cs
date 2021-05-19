using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Server.VideoSwitch.Model;
using TAS.Server.VideoSwitch.Model.Interfaces;

namespace TAS.Server.VideoSwitch.Communicators
{   
    /// <summary>
    /// Class to communicate with Ross MC-1 MCR switcher using Pressmaster protocol (default on port 9001)
    /// </summary>
    public class RossCommunicator : IVideoSwitchCommunicator
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public event EventHandler<EventArgs<CrosspointInfo>> SourceChanged;
        public event EventHandler<EventArgs<bool>> ConnectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        private TcpClient _tcpClient;
        private CancellationTokenSource _shutdownTokenSource = new CancellationTokenSource();
        private int _disposed;
        private bool _transitionTypeChanged;
        private bool _takeExecuting;
        private VideoSwitcherTransitionStyle _videoSwitcherTransitionStyle;

        private readonly object _syncObject = new object();

        private ConcurrentQueue<string> _requestsQueue = new ConcurrentQueue<string>();        
        private readonly ConcurrentDictionary<ListTypeEnum, int> _responseDictionary = new ConcurrentDictionary<ListTypeEnum, int>();
        private readonly Dictionary<ListTypeEnum, SemaphoreSlim> _semaphores = Enum.GetValues(typeof(ListTypeEnum)).Cast<ListTypeEnum>().ToDictionary(t => t, t => new SemaphoreSlim(t == ListTypeEnum.CrosspointStatus ? 1 : 0));

        private readonly SemaphoreSlim _requestQueueSemaphore = new SemaphoreSlim(0);        
        private readonly SemaphoreSlim _waitForTransitionEndSemaphore = new SemaphoreSlim(0);

        private PortInfo[] _sources;
        private List<Thread> _threads;
        

        private void RequestQueueHandlerProc()
        {
            try
            {
                while (!_shutdownTokenSource.IsCancellationRequested)
                {

                    _requestQueueSemaphore.Wait(_shutdownTokenSource.Token);
                    while (!_requestsQueue.IsEmpty)
                    {
                        if (!_requestsQueue.TryDequeue(out var request))
                            continue;

                        var stringBytes = request.Split(' ');
                        var bytes = new byte[stringBytes.Length];

                        for(int i=0; i<bytes.Length; ++i)
                        {
                            if (!byte.TryParse(stringBytes[i], System.Globalization.NumberStyles.HexNumber, null, out var b))
                            {
                                Console.WriteLine("Could not serialize message");
                                return;
                            }
                            bytes[i]= b;
                        }                        
                        _tcpClient.GetStream().Write(bytes, 0, bytes.Length);
                        Logger.Debug($"Ross message sent: {request}");
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is ObjectDisposedException || ex is System.IO.IOException)
                    Logger.Debug("Router request handler stream closed/disposed.");
                else if (ex is OperationCanceledException)
                    Logger.Debug("Router request handler cancelled");
                else
                    Logger.Error(ex, "Unexpected exception in Blackmagic request handler");
            }
        }
     
        private void ParseCommand(IList<string> message)
        {
            if (message.Count < 2 || (message[0] != "FE" && message[0] != "FF"))
                return;
            

            switch (message[1])
            {
                //Program has been set
                case "49":
                    if (message[2] != "7F")
                    {
                        try
                        {
                            if (short.TryParse(message[2], System.Globalization.NumberStyles.HexNumber, null, out var inPort))
                            {
                                if (_semaphores.TryGetValue(ListTypeEnum.CrosspointStatus, out var semaphoreStatus) &&
                                    semaphoreStatus.CurrentCount == 0 &&
                                    !_responseDictionary.TryGetValue(ListTypeEnum.CrosspointStatus, out _))
                                {
                                    _responseDictionary.TryAdd(ListTypeEnum.CrosspointStatus, inPort);
                                    semaphoreStatus.Release();                                                                        
                                }
                                
                                SourceChanged?.Invoke(this, new EventArgs<CrosspointInfo>(new CrosspointInfo(inPort, -1)));
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn(ex, "Could not parse 'Input changed' data {0}", string.Join(" ", message));
                        }                        
                    }
                    //Tally extended message formula
                    else if (byte.TryParse(message[3], out var aa) && byte.TryParse(message[4], out var bb))
                    {
                        SourceChanged?.Invoke(this, new EventArgs<CrosspointInfo>(new CrosspointInfo((short)((bb & 0x7F) | ((aa & 0x7F) << 7)), -1)));
                        return;
                    }
                    break;

                //Response from non functional command. I use it as ping
                case "5E":
                    if (_semaphores.TryGetValue(ListTypeEnum.SignalPresence, out var semaphore))
                        semaphore.Release();

                    Logger.Trace("Ross ping successful");
                    break;

                case "4F":
                    lock (_syncObject)
                    {
                        _takeExecuting = false;
                        _waitForTransitionEndSemaphore.Release();
                    }                    
                    break;
            }
        }

        private void ParseMessage(string[] response)
        {
            var buffer = new List<string>();
            
            foreach (var r in response)
            {
                if (buffer.Count == 0)
                {
                    if (r == "FF" || r == "FE")                    
                        buffer.Add(r);
                    
                }
                else if (r == "FF" || r == "FE")
                {
                    ParseCommand(buffer);
                    buffer.Clear();
                    buffer.Add(r);
                }
                else
                    buffer.Add(r);

            }

            if (buffer.Count > 0)
                ParseCommand(buffer);           
        }

        private void ListenerProc()
        {
            var bytesReceived = new byte[256];
            Logger.Debug("Ross listener started!");
            while (!_shutdownTokenSource.IsCancellationRequested)
            {
                try
                {
                    var bytes = _tcpClient.GetStream().Read(bytesReceived, 0, bytesReceived.Length);
                    if (bytes == 0)
                    {
                        Logger.Debug("Remote endpoint closed connection.");
                        Disconnect();
                        return;
                    }                    
                    ParseMessage(BitConverter.ToString(bytesReceived, 0, bytes).Split('-'));
                }
                catch (Exception ex)
                {
                    if (ex is ObjectDisposedException || ex is System.IO.IOException)
                    {
                        Logger.Debug("Network stream closed/disposed.");
                        Disconnect();
                    }
                    else
                        Logger.Error(ex);
                    return;
                }
            }
        }

        private void AddToRequestQueue(string request)
        {
            if (_requestsQueue.Contains(request))
                return;

            _requestsQueue.Enqueue(request);
            _requestQueueSemaphore.Release();
        }        

        private void ConnectionWatcherProc()
        {
            //PING, non functional command
            const string pingCommand = "FF 1E";

            if (!_semaphores.TryGetValue(ListTypeEnum.SignalPresence, out var semaphore))
                return;

            while (!_shutdownTokenSource.IsCancellationRequested)
            {
                    AddToRequestQueue(pingCommand);
                    if (semaphore.Wait(3000, _shutdownTokenSource.Token))
                        _shutdownTokenSource.Token.WaitHandle.WaitOne(3000);
            }
            Logger.Debug("Connection watcher thread finished.");
        }

        public void Disconnect()
        {
            _shutdownTokenSource?.Cancel();
            _shutdownTokenSource = null;
            _tcpClient?.Close();
            _tcpClient = null;
            _threads?.ForEach(t => t.Join());
            _threads = null;
            ConnectionChanged?.Invoke(this, new EventArgs<bool>(false));
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != default(int))
                return;
            Disconnect();
            Logger.Debug("Ross communicator disposed");
        }

        public CrosspointInfo GetSelectedSource()
        {
            if (!_semaphores.TryGetValue(ListTypeEnum.CrosspointStatus, out var semaphore))
                return null;

            AddToRequestQueue($"FF 02");

            while (true)
            {
                try
                {
                    if (_shutdownTokenSource.IsCancellationRequested)
                        throw new OperationCanceledException(_shutdownTokenSource.Token);

                    if (!_responseDictionary.TryRemove(ListTypeEnum.CrosspointStatus, out var response))
                        semaphore.Wait(_shutdownTokenSource.Token);

                    if (!_responseDictionary.TryRemove(ListTypeEnum.CrosspointStatus, out response))
                        continue;

                    semaphore.Release(); // reset semaphore to 1

                    return new CrosspointInfo((short)response, -1);
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException)
                        Logger.Debug("Current Input Port request cancelled");
                    
                    return null;
                }
            }
        }

        private string SerializeInputIndex(int b)
        {
            if (b < 21)
                return BitConverter.ToString(new byte[] { (byte)b });

            return BitConverter.ToString(new byte[] { 127, (byte)((b >> 7) & 0x7F), (byte)(b & 0x7F) });
        }

        public void SetSource(int inPort)
        {
            while (_takeExecuting)
            {
                Logger.Trace("Waiting Program");
                _waitForTransitionEndSemaphore.Wait(1000, _shutdownTokenSource.Token);
            }

            AddToRequestQueue($"FF 09 {SerializeInputIndex(inPort)}");

            if (_waitForTransitionEndSemaphore.CurrentCount == 0)
                _waitForTransitionEndSemaphore.Release();

            if (!_transitionTypeChanged)
                return;
            SetTransitionStyle(_videoSwitcherTransitionStyle);
            _transitionTypeChanged = false;
        }

        public bool Connect(string address)
        {
            Debug.Assert(_threads is null);
            _shutdownTokenSource = new CancellationTokenSource();
            _threads = new List<Thread>();

            while (true)
            {
                _tcpClient = new TcpClient();

                Logger.Debug("Connecting to Ross...");
                try
                {
                    if (_shutdownTokenSource.IsCancellationRequested)
                        throw new OperationCanceledException(_shutdownTokenSource.Token);
                    var addressParts = address.Split(new char[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (addressParts.Length == 0)
                        throw new ApplicationException($"Invalid address provided: {address}");
                    int port = 9001;
                    if (addressParts.Length > 1)
                        int.TryParse(addressParts[1], out port);
                    _tcpClient.Connect(addressParts[0], port);

                    if (!_tcpClient.Connected)
                    {
                        _tcpClient.Close();
                        continue;
                    }

                    Logger.Debug("Ross connected!");

                    _requestsQueue = new ConcurrentQueue<string>();

                    _threads.Add(new Thread(RequestQueueHandlerProc) 
                    { 
                        IsBackground = true,
                        Name = $"{nameof(RossCommunicator)} request queue handler thread"
                    });
                    _threads.Add(new Thread(ListenerProc)
                    { 
                        IsBackground = true,
                        Name = $"{nameof(RossCommunicator)} listener thread"
                    });
                    _threads.Add(new Thread(ConnectionWatcherProc)
                    {
                        IsBackground = true,
                        Name = $"{nameof(RossCommunicator)} connection watcher thread"
                    });
                    _threads.ForEach(t => t.Start());

                    SetTransitionStyle(_videoSwitcherTransitionStyle);
                    _transitionTypeChanged = false;

                    Logger.Info("Ross router connected and ready!");

                    return true;
                }
                catch (Exception ex)
                {
                    if (ex is ObjectDisposedException || ex is System.IO.IOException)
                        Logger.Debug("Network stream closed");
                    else if (ex is OperationCanceledException)
                        Logger.Debug("Router connecting canceled");
                    else
                        Logger.Error(ex);

                    break;
                }
            }
            return false;
        }

        public void SetTransitionStyle(VideoSwitcherTransitionStyle videoSwitchEffect)
        {
            switch(videoSwitchEffect)
            {
                case VideoSwitcherTransitionStyle.VFade:
                    AddToRequestQueue($"FF 01 01");
                    break;
                case VideoSwitcherTransitionStyle.FadeAndTake:
                    AddToRequestQueue($"FF 01 02");
                    break;
                case VideoSwitcherTransitionStyle.Mix:
                    AddToRequestQueue($"FF 01 03");
                    break;
                case VideoSwitcherTransitionStyle.TakeAndFade:
                    AddToRequestQueue($"FF 01 04");
                    break;
                case VideoSwitcherTransitionStyle.Cut:
                    AddToRequestQueue($"FF 01 05");
                    break;
                case VideoSwitcherTransitionStyle.WipeLeft:
                    AddToRequestQueue($"FF 01 06");
                    break;
                case VideoSwitcherTransitionStyle.WipeTop:
                    AddToRequestQueue($"FF 01 07");
                    break;
                case VideoSwitcherTransitionStyle.WipeReverseLeft:
                    AddToRequestQueue($"FF 01 10");
                    break;
                case VideoSwitcherTransitionStyle.WipeReverseTop:
                    AddToRequestQueue($"FF 01 11");
                    break;

                default:
                    return;
            }
            _videoSwitcherTransitionStyle = videoSwitchEffect;
            _transitionTypeChanged = true;
        }

        public void Preload(int sourceId)
        {
            while (_takeExecuting)
            {
                Logger.Trace("Waiting Preload");
                _waitForTransitionEndSemaphore.Wait();
            }

            Logger.Trace("Setting preview {0}", sourceId);
            AddToRequestQueue($"FF 0B {SerializeInputIndex(sourceId)}");

            if (_waitForTransitionEndSemaphore.CurrentCount == 0)
                _waitForTransitionEndSemaphore.Release();
        }
       
        public void SetMixSpeed(double rate)
        {
            AddToRequestQueue($"FF 03 {rate}");
        }

        public void Take()
        {
            lock (_syncObject)            
                _takeExecuting = true;

            Logger.Trace("Executing take");
            AddToRequestQueue("FF 0F");
        }

        public PortInfo[] Sources
        {
            get => _sources;
            set
            {
                if (value == _sources)
                    return;
                _sources = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Sources)));
            }
        }       
    }
}
