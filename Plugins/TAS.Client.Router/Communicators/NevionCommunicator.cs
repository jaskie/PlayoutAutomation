using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    internal class NevionCommunicator : IRouterCommunicator
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private TcpClient _tcpClient;

        private NetworkStream _stream;
        private readonly Router _router;

        private ConcurrentQueue<string> _requestsQueue = new ConcurrentQueue<string>();
        private ConcurrentQueue<KeyValuePair<ListTypeEnum, string[]>> _responsesQueue =new ConcurrentQueue<KeyValuePair<ListTypeEnum, string[]>>();
        private readonly ConcurrentDictionary<ListTypeEnum, string[]> _responseDictionary = new ConcurrentDictionary<ListTypeEnum, string[]>();

        private readonly Dictionary<ListTypeEnum, SemaphoreSlim> _semaphores = Enum.GetValues(typeof(ListTypeEnum)).Cast<ListTypeEnum>().ToDictionary(t => t, t => new SemaphoreSlim(0));
        
        private readonly SemaphoreSlim _requestQueueSemaphore = new SemaphoreSlim(0);
        private readonly SemaphoreSlim _responsesQueueSemaphore = new SemaphoreSlim(0);

        private CancellationTokenSource _cancellationTokenSource;

        private string _response;
        private int _disposed;

        public NevionCommunicator(IRouter device)
        {
            _router = device as Router;               
        }

        public async Task<bool> ConnectAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            while (_disposed == default(int))
            {                
                _tcpClient = new TcpClient();

                Logger.Debug("Connecting to Nevion...");
                try
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                        throw new OperationCanceledException(_cancellationTokenSource.Token);

                    var connectTask = _tcpClient.ConnectAsync(_router.IpAddress.Split(':')[0], Int32.Parse(_router.IpAddress.Split(':')[1]));
                    await Task.WhenAny(connectTask, Task.Delay(3000, _cancellationTokenSource.Token)).ConfigureAwait(false);

                    if (_tcpClient.Client?.Connected != true)
                    {
                        _tcpClient.Close();
                        continue;
                    }                        

                    Logger.Debug("Nevion connected!");

                    _requestsQueue = new ConcurrentQueue<string>();
                    _responsesQueue = new ConcurrentQueue<KeyValuePair<ListTypeEnum, string[]>>();
                    StartRequestQueueHandler();
                    StartResponseQueueHandler();
                   
                    _stream = _tcpClient.GetStream();
                    StartListener();

                    SignalPresenceWatcher();
                    InputPortWatcher();

                    Logger.Info("Nevion router connected and ready!");

                    AddToRequestQueue($"login {_router.Login} {_router.Password}");
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
           AddToRequestQueue($"x l{_router.Level} {inPort} {string.Join(",", _router.OutputPorts.Select(param => param.ToString()))}");            
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
            Logger.Debug("Nevion communicator disposed");
        }

        public event EventHandler<EventArgs<PortState[]>> ExtendedStatusReceived;        
        public event EventHandler<EventArgs<bool>> ConnectionChanged;
        public event EventHandler<EventArgs<CrosspointInfo>> SourceChanged;

        public async Task<PortInfo[]> GetSources()
        {
            if (!_semaphores.TryGetValue(ListTypeEnum.Input, out var semaphore))
                return null;

            AddToRequestQueue($"inlist l{_router.Level}");
            while (_disposed == default(int))
            {
                try
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                        throw new OperationCanceledException(_cancellationTokenSource.Token);

                    if (!_responseDictionary.TryRemove(ListTypeEnum.Input, out var response))
                        await semaphore.WaitAsync(_cancellationTokenSource.Token).ConfigureAwait(false);                    

                    if (response == null && !_responseDictionary.TryRemove(ListTypeEnum.Input, out response))
                        continue;

                    return response.Select(line =>
                    {
                        var lineParams = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        return lineParams.Length >= 4 ? new PortInfo(short.Parse(lineParams[2].Trim('\"')), lineParams[3].Trim('\"')) : null;
                    }).Where(c => c != null).ToArray();
                }
                catch(Exception ex)
                {
                    if (ex is OperationCanceledException)
                        Logger.Debug("Input ports request cancelled");

                    return null;
                }
            }
            return null;
        }       

        public async Task<CrosspointInfo> GetSelectedSource()
        {
            if (!_semaphores.TryGetValue(ListTypeEnum.CrosspointStatus, out var semaphore))
                return null;

            AddToRequestQueue($"si l{_router.Level} {string.Join(",", _router.OutputPorts)}");
            while (_disposed == default(int))
            {
                try
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                        throw new OperationCanceledException(_cancellationTokenSource.Token);

                    if (!_responseDictionary.TryRemove(ListTypeEnum.CrosspointStatus, out var response))
                        await semaphore.WaitAsync(_cancellationTokenSource.Token).ConfigureAwait(false);

                    if (response == null && !_responseDictionary.TryRemove(ListTypeEnum.CrosspointStatus, out response))
                        continue;

                    return response.Select(line =>
                    {
                        var lineParams = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (lineParams.Length >= 4 && lineParams[0] == "x" &&
                            lineParams[1] == $"l{_router.Level}" &&
                            lineParams[3] == _router.OutputPorts[0].ToString() &&
                            short.TryParse(lineParams[2], out var inPort) &&
                            short.TryParse(lineParams[3], out var outPort))
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
            return null;
        }       

        private async void StartRequestQueueHandler()
        {
            try
            {
                while (_disposed == default(int))
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
                        Logger.Debug($"Nevion message sent: {request}");
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
                    Logger.Error(ex, "Unexpected exception in Nevion request handler");
            }
        }

        private async void StartResponseQueueHandler()
        {
            try
            {
                while (_disposed == default(int))
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
            Logger.Debug("Nevion listener started!");
            while (_disposed == default(int))
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

        /// <summary>
        /// In Nevion it is also ping
        /// </summary>
        private async void SignalPresenceWatcher() 
        {
            if (!_semaphores.TryGetValue(ListTypeEnum.SignalPresence, out var semaphore))
                return;

            AddToRequestQueue($"sspi l{_router.Level}");
            
            while (_disposed == default(int))
            {
                try
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                        throw new OperationCanceledException(_cancellationTokenSource.Token);

                    if (!_responseDictionary.TryRemove(ListTypeEnum.SignalPresence, out var response))
                        await semaphore.WaitAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
                    
                    if (response == null && !_responseDictionary.TryRemove(ListTypeEnum.SignalPresence, out response))
                        continue;                                        

                    var portsSignal = response.Select(line =>
                    {
                        var lineParams = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        return lineParams.Length >= 4 ? new PortState(short.Parse(lineParams[2].Trim('\"')), lineParams[3].Trim('\"') == "p") : null;
                    }).Where(c => c != null).ToArray();
                    ExtendedStatusReceived?.Invoke(this, new EventArgs<PortState[]>(portsSignal));

                    await Task.Delay(3000);
                    AddToRequestQueue($"sspi l{_router.Level}");
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException)
                        Logger.Debug("Router Signal Presence Watcher cancelled");
                    
                    return;
                }
                
            }
        }

        private async void InputPortWatcher()
        {
            if (!_semaphores.TryGetValue(ListTypeEnum.CrosspointChange, out var semaphore))
                return;
            
            while (_disposed == default(int))
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
                        var lineParams = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (lineParams.Length >= 4 && lineParams[0] == "x" &&
                            lineParams[1] == $"l{_router.Level}" &&
                            lineParams[3] == _router.OutputPorts[0].ToString() &&
                            short.TryParse(lineParams[2], out var inPort) &&
                            short.TryParse(lineParams[3], out var outPort))
                            return new CrosspointInfo(inPort, outPort);
                        return null;
                    }).FirstOrDefault(c => c != null);

                    if (crosspoints == null)
                        continue;

                    SourceChanged?.Invoke(this, new EventArgs<CrosspointInfo>(crosspoints));
                }
                catch(Exception ex)
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
            if (lines.Length < 1 || !(lines[0].StartsWith("?") || lines[0].StartsWith("%")))
                return;
            var trimmedLines = lines.Skip(1).Where(param => !string.IsNullOrEmpty(param)).ToArray();

            if (lines[0].Contains($"inlist l{_router.Level}"))
            {
                if (!_semaphores.TryGetValue(ListTypeEnum.Input, out var semaphore))
                    return;

                if (!_responseDictionary.TryAdd(ListTypeEnum.Input, trimmedLines))
                    AddToResponseQueue(ListTypeEnum.Input, trimmedLines);
                else
                    semaphore.Release();

            }                

            else if (lines[0].Contains("si") && lines[0].Contains($"l{_router.Level}"))
            {
                if (!_semaphores.TryGetValue(ListTypeEnum.CrosspointStatus, out var semaphore))
                    return;

                if (!_responseDictionary.TryAdd(ListTypeEnum.CrosspointStatus, trimmedLines))
                    AddToResponseQueue(ListTypeEnum.CrosspointStatus, trimmedLines);
                else
                    semaphore.Release();
            }
                                
            else if (lines[0].Contains($"sspi l{_router.Level}"))
            {
                if (!_semaphores.TryGetValue(ListTypeEnum.SignalPresence, out var semaphore))
                    return;

                if (!_responseDictionary.TryAdd(ListTypeEnum.SignalPresence, trimmedLines))
                    AddToResponseQueue(ListTypeEnum.SignalPresence, trimmedLines);
                else
                    semaphore.Release();                
            }

            else if (lines.Length>1 && lines[0].StartsWith("%") && lines[1].Contains($"x l{_router.Level}"))
            {
                if (!_semaphores.TryGetValue(ListTypeEnum.CrosspointChange, out var semaphore))
                    return;

                if (!_responseDictionary.TryAdd(ListTypeEnum.CrosspointChange, trimmedLines))
                    AddToResponseQueue(ListTypeEnum.CrosspointChange, trimmedLines);
                else
                    semaphore.Release();
            }

            else if (lines[0].Contains("login"))
                if (lines[1].Contains("ok"))
                    Logger.Info("Nevion login ok");
                else if (lines[1].Contains("failed"))
                    Logger.Error("Nevion login incorrect");            
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
    }
}
