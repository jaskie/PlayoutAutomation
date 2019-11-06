using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Server.Model;

namespace TAS.Server.RouterCommunicators
{
    internal class NevionCommunicator : IRouterCommunicator
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private TcpClient _tcpClient;

        private NetworkStream _stream;
        private readonly RouterDevice _device;

        private ConcurrentQueue<string> _requestsQueue;
        private ConcurrentQueue<KeyValuePair<ListTypeEnum,string[]>> _responsesQueue;
        private readonly ConcurrentDictionary<ListTypeEnum, string[]> _responseDictionary = new ConcurrentDictionary<ListTypeEnum, string[]>();

        private readonly Dictionary<ListTypeEnum, SemaphoreSlim> _semaphores = Enum.GetValues(typeof(ListTypeEnum)).Cast<ListTypeEnum>().ToDictionary(t => t, t => new SemaphoreSlim(0));
        
        private readonly SemaphoreSlim _requestQueueSemaphore = new SemaphoreSlim(0);
        private readonly SemaphoreSlim _responsesQueueSemaphore = new SemaphoreSlim(0);

        private CancellationTokenSource _cancellationTokenSource;

        private string _response;
        private int _disposed;

        public NevionCommunicator(RouterDevice device)
        {
            _device = device;               
        }

        public async Task<bool> Connect()
        {
            _disposed = default(int);
            _cancellationTokenSource = new CancellationTokenSource();

            while (true)
            {                
                _tcpClient = new TcpClient();

                Debug.WriteLine("Connecting to Nevion...");
                try
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                        throw new OperationCanceledException(_cancellationTokenSource.Token);

                    var connectTask = _tcpClient.ConnectAsync(_device.IpAddress, _device.Port);
                    await Task.WhenAny(connectTask, Task.Delay(3000, _cancellationTokenSource.Token)).ConfigureAwait(false);

                    if (!_tcpClient.Connected)
                        continue;


                    Debug.WriteLine("Nevion connected!");

                    _requestsQueue = new ConcurrentQueue<string>();
                    _responsesQueue = new ConcurrentQueue<KeyValuePair<ListTypeEnum, string[]>>();
                    StartRequestQueueHandler();
                    StartResponseQueueHandler();

                    _requestsQueue = new ConcurrentQueue<string>();
                    _stream = _tcpClient.GetStream();
                    StartListener();

                    SignalPresenceWatcher();
                    InputPortWatcher();

                    Logger.Info("Nevion router connected and ready!");

                    AddToRequestQueue($"login {_device.Login} {_device.Password}");
                    return true;
                }
                catch(OperationCanceledException)
                {
                    Logger.Debug("Router connecting canceled");
                    break;
                }
                catch (Exception ex)
                {
                    if (ex is ObjectDisposedException || ex is System.IO.IOException)
                        Logger.Debug("Network stream closed");
                    else
                        Logger.Error(ex);

                    break;
                }

            }
            return false;
        }         

        public void SelectInput(int inPort)
        {
           AddToRequestQueue($"x l{_device.Level} {inPort} {string.Join(",", _device.OutputPorts.Select(param => param.ToString()))}");            
        }                

        public void Disconnect()
        {
            _cancellationTokenSource.Cancel();
            _tcpClient?.Close();            
            OnRouterConnectionStateChanged?.Invoke(this, new EventArgs<bool>(false));
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != default(int))
                return;
            Disconnect();           
            Debug.WriteLine("Nevion communicator disposed");
        }

        public event EventHandler<EventArgs<PortState[]>> OnRouterPortsStatesReceived;        
        public event EventHandler<EventArgs<bool>> OnRouterConnectionStateChanged;
        public event EventHandler<EventArgs<CrosspointInfo>> OnInputPortChangeReceived;

        public async Task<PortInfo[]> GetInputPorts()
        {
            if (!_semaphores.TryGetValue(ListTypeEnum.Input, out var semaphore))
                return null;

            AddToRequestQueue($"inlist l{_device.Level}");
            while (true)
            {
                if (!_responseDictionary.TryRemove(ListTypeEnum.Input, out var response))
                    await semaphore.WaitAsync().ConfigureAwait(false);

                if (response == null && !_responseDictionary.TryRemove(ListTypeEnum.Input, out response))                
                    continue;
                
                return response.Select(line =>
                {
                    var lineParams = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    return lineParams.Length >= 4 ? new PortInfo(short.Parse(lineParams[2].Trim('\"')), lineParams[3].Trim('\"')) : null;
                }).Where(c => c != null).ToArray();
            }
        }       

        public async Task<CrosspointInfo> GetCurrentInputPort()
        {
            if (!_semaphores.TryGetValue(ListTypeEnum.CrosspointStatus, out var semaphore))
                return null;

            AddToRequestQueue($"si l{_device.Level} {string.Join(",", _device.OutputPorts)}");
            while (true)
            {
                if (!_responseDictionary.TryRemove(ListTypeEnum.CrosspointStatus, out var response))
                    await semaphore.WaitAsync().ConfigureAwait(false);

                if (response == null && !_responseDictionary.TryRemove(ListTypeEnum.CrosspointStatus, out response))                
                    continue;                

                return response.Select(line =>
                {
                    var lineParams = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (lineParams.Length >= 4 && lineParams[0] == "x" &&
                        lineParams[1] == $"l{_device.Level}" &&
                        lineParams[3] == _device.OutputPorts[0].ToString() &&
                        short.TryParse(lineParams[2], out var inPort) &&
                        short.TryParse(lineParams[3], out var outPort))
                            return new CrosspointInfo(inPort, outPort);
                    return null;
                }).Where(c => c != null).First();
            }
        }       

        private async void StartRequestQueueHandler()
        {
            try
            {
                while (true)
                {
                    await _requestQueueSemaphore.WaitAsync().ConfigureAwait(false);
                    while (!_requestsQueue.IsEmpty)
                    {
                        if (!_requestsQueue.TryDequeue(out var request))
                            continue;
                        var data = System.Text.Encoding.ASCII.GetBytes(string.Concat(request, "\n\n"));
                        await _stream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                        Debug.WriteLine($"Nevion message sent: {request}");
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is ObjectDisposedException || ex is System.IO.IOException)
                    Debug.WriteLine("Router request handler stream closed/disposed.");
                else
                    Logger.Error(ex, "Unexpected exception in Nevion request handler");
            }
        }

        private async void StartResponseQueueHandler()
        {
            try
            {
                while (true)
                {
                    await _responsesQueueSemaphore.WaitAsync().ConfigureAwait(false);
                    while (!_responsesQueue.IsEmpty)
                    {
                        if (!_responsesQueue.TryDequeue(out var response))
                            continue;

                        if (_responseDictionary.TryAdd(response.Key, response.Value))
                        {
                            if (_semaphores.TryGetValue(response.Key, out var semaphore))
                            {
                                semaphore.Release();
                                continue;
                            }                          
                        }
                            
                        else
                            _responsesQueue.Enqueue(response);                        
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is ObjectDisposedException || ex is System.IO.IOException)
                    Debug.WriteLine("Router request handler stream closed/disposed.");
                else
                    Logger.Error(ex, "Unexpected exception in Nevion request handler");
            }
        }

        private async void StartListener()
        {
            var bytesReceived = new byte[256];
            Debug.WriteLine("Nevion listener started!");
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
                        Debug.WriteLine("Router listener network stream closed/disposed.");
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

            AddToRequestQueue($"sspi l{_device.Level}");
            
            while (true)
            {
                if (!_responseDictionary.TryRemove(ListTypeEnum.SignalPresence, out var response))
                    await semaphore.WaitAsync().ConfigureAwait(false);
                
                if (response == null && !_responseDictionary.TryRemove(ListTypeEnum.SignalPresence, out response))
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                        break;

                    continue;
                }
                                                   
                var portsSignal = response.Select(line =>
                {
                    var lineParams = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    return lineParams.Length >= 4 ? new PortState(short.Parse(lineParams[2].Trim('\"')), lineParams[3].Trim('\"') == "p") : null;
                }).Where(c => c != null).ToArray();
                OnRouterPortsStatesReceived?.Invoke(this, new EventArgs<PortState[]>(portsSignal));

                await Task.Delay(3000);
                AddToRequestQueue($"sspi l{_device.Level}");                
            }
        }

        private async void InputPortWatcher()
        {
            if (!_semaphores.TryGetValue(ListTypeEnum.CrosspointChange, out var semaphore))
                return;
            
            while (true)
            {
                if (!_responseDictionary.TryRemove(ListTypeEnum.CrosspointChange, out var response))                                   
                    await semaphore.WaitAsync().ConfigureAwait(false);                                    
               
                if (response == null && !_responseDictionary.TryRemove(ListTypeEnum.CrosspointChange, out response))
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                        break;
                    continue;
                }                    
               
                var crosspoints = response.Select(line =>
                {
                    var lineParams = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (lineParams.Length >= 4 && lineParams[0] == "x" &&
                        lineParams[1] == $"l{_device.Level}" &&
                        lineParams[3] == _device.OutputPorts[0].ToString() &&
                        short.TryParse(lineParams[2], out var inPort) &&
                        short.TryParse(lineParams[3], out var outPort))
                            return new CrosspointInfo(inPort, outPort);
                    return null;
                }).FirstOrDefault(c => c != null);

                if (crosspoints == null)
                    continue;

                OnInputPortChangeReceived?.Invoke(this, new EventArgs<CrosspointInfo>(crosspoints));
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

            if (lines[0].Contains($"inlist l{_device.Level}"))
            {
                if (!_semaphores.TryGetValue(ListTypeEnum.Input, out var semaphore))
                    return;

                if (!_responseDictionary.TryAdd(ListTypeEnum.Input, trimmedLines))
                    AddToResponseQueue(ListTypeEnum.Input, trimmedLines);
                else
                    semaphore.Release();

            }                

            else if (lines[0].Contains("si") && lines[0].Contains($"l{_device.Level}"))
            {
                if (!_semaphores.TryGetValue(ListTypeEnum.CrosspointStatus, out var semaphore))
                    return;

                if (!_responseDictionary.TryAdd(ListTypeEnum.CrosspointStatus, trimmedLines))
                    AddToResponseQueue(ListTypeEnum.CrosspointStatus, trimmedLines);
                else
                    semaphore.Release();
            }
                                
            else if (lines[0].Contains($"sspi l{_device.Level}"))
            {
                if (!_semaphores.TryGetValue(ListTypeEnum.SignalPresence, out var semaphore))
                    return;

                if (!_responseDictionary.TryAdd(ListTypeEnum.SignalPresence, trimmedLines))
                    AddToResponseQueue(ListTypeEnum.SignalPresence, trimmedLines);
                else
                    semaphore.Release();                
            }

            else if (lines.Length>1 && lines[0].StartsWith("%") && lines[1].Contains($"x l{_device.Level}"))
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
                //Debug.WriteLine(command);                
                ProcessCommand(command);
            }
        }        
    }
}
