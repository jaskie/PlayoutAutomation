using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TAS.Common;
using TAS.Server.VideoSwitch.Model;

namespace TAS.Server.VideoSwitch.Communicators
{
    public class RossCommunicator : IVideoSwitchCommunicator
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public event EventHandler<EventArgs<CrosspointInfo>> OnInputPortChangeReceived;
        
        //Ross does not have API for sources download.
        public event EventHandler<EventArgs<PortState[]>> OnRouterPortsStatesReceived;
        
        public event EventHandler<EventArgs<bool>> OnRouterConnectionStateChanged;

        private TcpClient _tcpClient;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private int _disposed;
        private VideoSwitch _mc;
        private bool _transitionTypeChanged;

        private ConcurrentQueue<string> _requestsQueue = new ConcurrentQueue<string>();        
        private readonly ConcurrentDictionary<ListTypeEnum, int> _responseDictionary = new ConcurrentDictionary<ListTypeEnum, int>();
        private readonly Dictionary<ListTypeEnum, SemaphoreSlim> _semaphores = Enum.GetValues(typeof(ListTypeEnum)).Cast<ListTypeEnum>().ToDictionary(t => t, t => new SemaphoreSlim(t == ListTypeEnum.CrosspointStatus ? 1 : 0));

        private readonly SemaphoreSlim _requestQueueSemaphore = new SemaphoreSlim(0);
        private readonly SemaphoreSlim _responsesQueueSemaphore = new SemaphoreSlim(0);        

        public RossCommunicator(VideoSwitch videoSwitch)
        {
            _mc = videoSwitch;
        }

        public async Task<bool> Connect()
        {
            _disposed = default(int);
            _cancellationTokenSource = new CancellationTokenSource();

            while (true)
            {
                _tcpClient = new TcpClient();

                Logger.Debug("Connecting to Ross...");
                try
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                        throw new OperationCanceledException(_cancellationTokenSource.Token);

                    var connectTask = _tcpClient.ConnectAsync(_mc.IpAddress.Split(':')[0], Int32.Parse(_mc.IpAddress.Split(':')[1]));
                    await Task.WhenAny(connectTask, Task.Delay(3000, _cancellationTokenSource.Token)).ConfigureAwait(false);

                    if (!_tcpClient.Connected)
                    {
                        _tcpClient.Close();
                        continue;
                    }

                    Logger.Debug("Blackmagic connected!");

                    _requestsQueue = new ConcurrentQueue<string>();
                    
                    StartRequestQueueHandler();                                        
                    StartListener();
                    ConnectionWatcher();

                    SetTransitionEffect(_mc.DefaultEffect);
                    _transitionTypeChanged = false;

                    Logger.Info("Blackmagic router connected and ready!");

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

        private async void StartRequestQueueHandler()
        {
            try
            {
                while (true)
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                        throw new OperationCanceledException(_cancellationTokenSource.Token);

                    await _requestQueueSemaphore.WaitAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
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

                        await _tcpClient.GetStream().WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                        Logger.Debug($"Blackmagic message sent: {request}");
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
            if (message.Count < 3 || (message[0] != "FE" && message[0] != "FF"))
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

                                OnInputPortChangeReceived?.Invoke(this, new EventArgs<CrosspointInfo>(new CrosspointInfo(inPort, -1)));
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
                        OnInputPortChangeReceived?.Invoke(this, new EventArgs<CrosspointInfo>(new CrosspointInfo((short)((bb & 0x7F) | ((aa & 0x7F) << 7)), -1)));
                        return;
                    }
                    break;

                //Response from non functional command. I use it as ping
                case "5E":
                    if (_semaphores.TryGetValue(ListTypeEnum.SignalPresence, out var semaphore))
                        semaphore.Release();

                    Logger.Trace("Ross ping successful");
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

        private async void StartListener()
        {
            var bytesReceived = new byte[256];
            Logger.Debug("Ross listener started!");
            while (true)
            {
                try
                {
                    var bytes = await _tcpClient.GetStream().ReadAsync(bytesReceived, 0, bytesReceived.Length).ConfigureAwait(false);
                    if (bytes == 0) 
                        continue;
                    
                    ParseMessage(BitConverter.ToString(bytesReceived, 0, bytes).Split('-'));
                }
                catch (Exception ex)
                {
                    if (ex is ObjectDisposedException || ex is System.IO.IOException)
                    {
                        Logger.Debug("Ross listener network stream closed/disposed.");
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

        private async void ConnectionWatcher()
        {
            const string pingCommand = "FF 1E";

            if (!_semaphores.TryGetValue(ListTypeEnum.SignalPresence, out var semaphore))
                return;
            
            //PING, non functional command
            AddToRequestQueue(pingCommand);

            while (true)
            {
                try
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                        throw new OperationCanceledException(_cancellationTokenSource.Token);

                    await semaphore.WaitAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
                    await Task.Delay(3000, _cancellationTokenSource.Token).ConfigureAwait(false);
                    AddToRequestQueue(pingCommand);
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException)
                        Logger.Debug("Router Ping cancelled");

                    return;
                }
            }
        }

        public void Disconnect()
        {
            _cancellationTokenSource?.Cancel();
            _tcpClient?.Close();
            OnRouterConnectionStateChanged?.Invoke(this, new EventArgs<bool>(false));
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != default(int))
                return;
            Disconnect();
            Logger.Debug("Ross communicator disposed");
        }

        public async Task<CrosspointInfo> GetCurrentInputPort()
        {
            if (!_semaphores.TryGetValue(ListTypeEnum.CrosspointStatus, out var semaphore))
                return null;

            AddToRequestQueue($"FF 02");

            while (true)
            {
                try
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                        throw new OperationCanceledException(_cancellationTokenSource.Token);

                    if (!_responseDictionary.TryRemove(ListTypeEnum.CrosspointStatus, out var response))
                        await semaphore.WaitAsync(_cancellationTokenSource.Token).ConfigureAwait(false);

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

        public async Task<PortInfo[]> GetInputPorts()
        {
            return await Task.Run(() =>             
            {
                var portInfos = new List<PortInfo>();

                foreach (var port in _mc.InputPorts)
                {
                    portInfos.Add(new PortInfo(port.PortId, port.PortName));
                }

                return portInfos.ToArray();
            });             
        }

        private string SerializeInputIndex(int b)
        {
            if (b < 21)
                return BitConverter.ToString(new byte[] { (byte)b });

            return BitConverter.ToString(new byte[] { 127, (byte)((b >> 7) & 0x7F), (byte)(b & 0x7F) });
        }

        public void SelectInput(int inPort)
        {
            AddToRequestQueue($"FF 09 {SerializeInputIndex(inPort)}");
            
            if (!_transitionTypeChanged)
                return;
            SetTransitionEffect(_mc.DefaultEffect);
            _transitionTypeChanged = false;
        }

        public void SetTransitionEffect(VideoSwitchEffect videoSwitchEffect)
        {
            switch(videoSwitchEffect)
            {
                case VideoSwitchEffect.Cut:
                    AddToRequestQueue($"FF 05");
                    break;
                case VideoSwitchEffect.Fade:
                    AddToRequestQueue($"FF 01");
                    break;
                case VideoSwitchEffect.Mix:
                    AddToRequestQueue($"FF 03");
                    break;
                default:
                    return;
            }

            _transitionTypeChanged = true;
        }
    }
}
