using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
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
    internal class BlackmagicSmartVideoHubCommunicator : IRouterCommunicator
    {
        /// <summary>
        /// In Blackmagic CrosspointStatus and CrosspointChange responses have the same syntax. CrosspointStatus semaphore initial value is set to 1 to help notify ProcessCommand method
        /// about pending request from GetCurrentInputPort method
        /// </summary>
        /// 
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private TcpClient _tcpClient;

        private NetworkStream _stream;
        private readonly RouterBase _router;

        private ConcurrentQueue<string> _requestsQueue = new ConcurrentQueue<string>();
        private ConcurrentQueue<KeyValuePair<ListTypeEnum, string[]>> _responsesQueue = new ConcurrentQueue<KeyValuePair<ListTypeEnum, string[]>>();

        private readonly ConcurrentDictionary<ListTypeEnum, string[]> _responseDictionary = new ConcurrentDictionary<ListTypeEnum, string[]>();

        private readonly Dictionary<ListTypeEnum, SemaphoreSlim> _semaphores = Enum.GetValues(typeof(ListTypeEnum)).Cast<ListTypeEnum>().ToDictionary(t => t, t => new SemaphoreSlim(t == ListTypeEnum.CrosspointStatus ? 1 : 0));

        private readonly SemaphoreSlim _requestQueueSemaphore = new SemaphoreSlim(0);
        private readonly SemaphoreSlim _responsesQueueSemaphore = new SemaphoreSlim(0);

        private CancellationTokenSource _cancellationTokenSource;

        private string _response;
        private int _disposed;

        private PortInfo[] _sources;
        

        public BlackmagicSmartVideoHubCommunicator(IVideoSwitch device)
        {
            _router = device as RouterBase;
        }               

        private async Task<PortInfo[]> GetSources()
        {
            if (!_semaphores.TryGetValue(ListTypeEnum.Input, out var semaphore))
                return null;

            AddToRequestQueue("INPUT LABELS:");
            while (true)
            {
                try
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                        throw new OperationCanceledException(_cancellationTokenSource.Token);

                    if (!_responseDictionary.TryRemove(ListTypeEnum.Input, out var response))
                        await semaphore.WaitAsync(_cancellationTokenSource.Token).ConfigureAwait(false);

                    if (response == null && !_responseDictionary.TryRemove(ListTypeEnum.Input, out response))
                        continue;

                    semaphore.Release(); // reset semaphore to 1

                    return response.Select(line =>
                    {
                        var lineParams = line.Split(new[] {' '}, 2, StringSplitOptions.RemoveEmptyEntries);
                        return lineParams.Length >= 2
                            ? new PortInfo(short.Parse(lineParams[0]), lineParams.ElementAtOrDefault(1) ?? string.Empty)
                            : null;
                    }).Where(c => c != null).ToArray();
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException)
                        Logger.Debug("Input ports request cancelled");

                    return null;
                }
            }
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
                        var data = System.Text.Encoding.ASCII.GetBytes(string.Concat(request, "\n\n"));
                        await _stream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
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

        private async void StartResponseQueueHandler()
        {
            try
            {
                while (true)
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                        throw new OperationCanceledException(_cancellationTokenSource.Token);

                    await _responsesQueueSemaphore.WaitAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
                    while (!_responsesQueue.IsEmpty)
                    {
                        if (_cancellationTokenSource.IsCancellationRequested)
                            throw new OperationCanceledException(_cancellationTokenSource.Token);

                        if (!_responsesQueue.TryDequeue(out var response))
                            continue;

                        if (_responseDictionary.TryAdd(response.Key, response.Value))
                        {
                            if (!_semaphores.TryGetValue(response.Key, out var semaphore))
                                continue;
                            semaphore.Release();
                        }
                        else
                            _responsesQueue.Enqueue(response);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is ObjectDisposedException || ex is System.IO.IOException)
                    Logger.Debug("Router response handler stream closed/disposed.");
                else if (ex is OperationCanceledException)
                    Logger.Debug("Router response handler cancelled");
                else
                    Logger.Error(ex, "Unexpected exception in Blackmagic response handler");
            }
        }

        private async void StartListener()
        {
            var bytesReceived = new byte[256];
            Logger.Debug("Blackmagic listener started!");
            while (true)
            {
                try
                {
                    var bytes = await _stream.ReadAsync(bytesReceived, 0, bytesReceived.Length).ConfigureAwait(false);
                    if (bytes == 0) continue;
                    var response = System.Text.Encoding.ASCII.GetString(bytesReceived, 0, bytes);
                    ParseMessage(response);
                }
                catch (Exception ex)
                {
                    if (ex is ObjectDisposedException || ex is System.IO.IOException)
                    {
                        Logger.Debug("Router listener network stream closed/disposed.");
                        Disconnect();
                    }
                    else
                        Logger.Error(ex);
                    return;
                }
            }
        }

        private async void ConnectionWatcher()
        {
            if (!_semaphores.TryGetValue(ListTypeEnum.SignalPresence, out var semaphore))
                return;

            AddToRequestQueue("PING:");

            while (true)
            {
                try
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                        throw new OperationCanceledException(_cancellationTokenSource.Token);

                    await semaphore.WaitAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
                    await Task.Delay(3000, _cancellationTokenSource.Token).ConfigureAwait(false);
                    AddToRequestQueue("PING:");
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException)
                        Logger.Debug("Router Ping cancelled");

                    return;
                }
            }
        }

        private async void InputPortWatcher()
        {
            if (!_semaphores.TryGetValue(ListTypeEnum.CrosspointChange, out var semaphore))
                return;

            while (true)
            {
                try
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                        throw new OperationCanceledException(_cancellationTokenSource.Token);

                    if (!_responseDictionary.TryRemove(ListTypeEnum.CrosspointChange, out var response))
                        await semaphore.WaitAsync(_cancellationTokenSource.Token).ConfigureAwait(false);

                    if (response == null && !_responseDictionary.TryRemove(ListTypeEnum.CrosspointChange, out response))
                        continue;
                   
                    var crosspoints = response.Select(line =>
                    {
                        var lineParams = line.Split(new[] {' '}, 2, StringSplitOptions.RemoveEmptyEntries);
                        if (lineParams.Length >= 2 &&
                            short.TryParse(lineParams[0], out var outPort) &&
                            outPort == _router.OutputPorts[0] &&
                            short.TryParse(lineParams[1], out var inPort))
                            return new CrosspointInfo(inPort, outPort);

                        return null;
                    }).FirstOrDefault(c => c != null);

                    if (crosspoints == null)
                        continue;

                    SourceChanged?.Invoke(this, new EventArgs<CrosspointInfo>(crosspoints));
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException)
                        Logger.Debug("Input Port Watcher cancelled");

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

        private void AddToResponseQueue(ListTypeEnum type, string[] response)
        {
            _responsesQueue.Enqueue(new KeyValuePair<ListTypeEnum, string[]>(type, response));
            _responsesQueueSemaphore.Release();
        }

        private void ProcessCommand(string response)
        {
            var lines = response.Split('\n');
            if (lines.Length < 1 || lines[0].StartsWith("NAK"))
                return;

            if (lines.Length > 2 && lines[0].StartsWith("ACK") && lines[2] == "")
            {
                if (!_semaphores.TryGetValue(ListTypeEnum.SignalPresence, out var semaphore))
                    return;

                semaphore.Release();
            }

            var trimmedLines = lines.Skip(1).Where(param => !string.IsNullOrEmpty(param)).ToArray();

            if (lines[0].Contains("INPUT LABELS"))
            {
                if (!_semaphores.TryGetValue(ListTypeEnum.Input, out var semaphore))
                    return;

                if (!_responseDictionary.TryAdd(ListTypeEnum.Input, trimmedLines))
                    AddToResponseQueue(ListTypeEnum.Input, trimmedLines);
                else
                    semaphore.Release();
            }

            else if (lines[0].Contains("OUTPUT ROUTING"))
            {
                if (_semaphores.TryGetValue(ListTypeEnum.CrosspointStatus, out var semaphoreStatus) &&
                    semaphoreStatus.CurrentCount == 0 &&
                    !_responseDictionary.TryGetValue(ListTypeEnum.CrosspointStatus, out _))
                {
                    _responseDictionary.TryAdd(ListTypeEnum.CrosspointStatus, trimmedLines);

                    semaphoreStatus.Release();
                    return;
                }


                if (!_semaphores.TryGetValue(ListTypeEnum.CrosspointChange, out var semaphoreChange))
                    return;

                if (!_responseDictionary.TryAdd(ListTypeEnum.CrosspointChange, trimmedLines))
                    AddToResponseQueue(ListTypeEnum.CrosspointChange, trimmedLines);
                else
                    semaphoreChange.Release();
            }
        }

        private void ParseMessage(string response)
        {
            _response += response;
            while (_response.Contains("\n\n"))
            {
                var command = _response.Substring(0, _response.IndexOf("\n\n", StringComparison.Ordinal) + 2);
                _response = _response.Remove(0, _response.IndexOf("\n\n", StringComparison.Ordinal) + 2);                              
                ProcessCommand(command);
            }
        }

        public event EventHandler<EventArgs<PortState[]>> ExtendedStatusReceived;
        public event EventHandler<EventArgs<bool>> ConnectionChanged;
        public event EventHandler<EventArgs<CrosspointInfo>> SourceChanged;
        public event PropertyChangedEventHandler PropertyChanged;

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

        public async Task<CrosspointInfo> GetSelectedSource()
        {
            if (!_semaphores.TryGetValue(ListTypeEnum.CrosspointStatus, out var semaphore))
                return null;

            AddToRequestQueue("VIDEO OUTPUT ROUTING:");
            while (true)
            {
                try
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                        throw new OperationCanceledException(_cancellationTokenSource.Token);

                    if (!_responseDictionary.TryRemove(ListTypeEnum.CrosspointStatus, out var response))
                        await semaphore.WaitAsync(_cancellationTokenSource.Token).ConfigureAwait(false);

                    if (response == null && !_responseDictionary.TryRemove(ListTypeEnum.CrosspointStatus, out response))
                        continue;

                    semaphore.Release(); // reset semaphore to 1
                    return response.Select(line =>
                    {
                        var lineParams = line.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        if (lineParams.Length >= 2 &&
                            short.TryParse(lineParams[0], out var outPort) &&
                            outPort == _router.OutputPorts[0] &&
                            short.TryParse(lineParams[1], out var inPort))
                            return new CrosspointInfo(inPort, outPort);

                        return null;
                    }).FirstOrDefault(c => c != null);
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException)
                        Logger.Debug("Current Input Port request cancelled");

                    return null;
                }
            }
        }

        public async Task<bool> ConnectAsync()
        {
            _disposed = default(int);
            _cancellationTokenSource = new CancellationTokenSource();

            while (true)
            {
                _tcpClient = new TcpClient();

                Logger.Debug("Connecting to Blackmagic...");
                try
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                        throw new OperationCanceledException(_cancellationTokenSource.Token);

                    var connectTask = _tcpClient.ConnectAsync(_router.IpAddress.Split(':')[0], Int32.Parse(_router.IpAddress.Split(':')[1]));
                    await Task.WhenAny(connectTask, Task.Delay(3000, _cancellationTokenSource.Token)).ConfigureAwait(false);

                    if (!_tcpClient.Connected)
                    {
                        _tcpClient.Close();
                        continue;
                    }

                    Logger.Debug("Blackmagic connected!");

                    _requestsQueue = new ConcurrentQueue<string>();
                    _responsesQueue = new ConcurrentQueue<KeyValuePair<ListTypeEnum, string[]>>();
                    StartRequestQueueHandler();
                    StartResponseQueueHandler();

                    _stream = _tcpClient.GetStream();
                    StartListener();

                    ConnectionWatcher();
                    InputPortWatcher();

                    Sources = await GetSources();
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

        public void SetSource(int inPort)
        {
            AddToRequestQueue(_router.OutputPorts.Aggregate("VIDEO OUTPUT ROUTING:\n", (current, outPort) => current + string.Concat(outPort, " ", inPort, "\n")));
        }

        public void Disconnect()
        {
            _cancellationTokenSource?.Cancel();
            _tcpClient?.Close();
            ConnectionChanged?.Invoke(this, new EventArgs<bool>(false));
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != default(int))
                return;
            Disconnect();
            Logger.Debug("Blackmagic communicator disposed");
        }
    }
}