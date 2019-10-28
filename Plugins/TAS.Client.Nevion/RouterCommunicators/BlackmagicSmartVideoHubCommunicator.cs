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
    public class BlackmagicSmartVideoHubCommunicator : IRouterCommunicator
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private TcpClient _tcpClient;           

        private NetworkStream _stream;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly RouterDevice _device;

        private Task _requestHandlerTask;
        private Task _listenerTask;
        private Task<bool> _sendTask;        

        private ConcurrentQueue<string> _requestsQueue = new ConcurrentQueue<string>();
        private readonly SemaphoreSlim _requestHandlerSemaphore = new SemaphoreSlim(0);
        private bool _inputPortsListRequested;
        private bool _currentInputPortRequested;
        private bool _routerStateRequested;
        private bool _outputPortsListRequested;        

        private string _response;

        public event EventHandler<EventArgs<IEnumerable<IRouterPort>>> OnRouterStateReceived;
        public event EventHandler<EventArgs<IEnumerable<IRouterPort>>> OnInputPortsListReceived;
        public event EventHandler<EventArgs<IEnumerable<IRouterPort>>> OnOutputPortsListReceived;
        public event EventHandler<EventArgs<bool>> OnRouterConnectionStateChanged;
        public event EventHandler<EventArgs<IEnumerable<Crosspoint>>> OnInputPortChangeReceived;
        private event EventHandler<EventArgs<string>> OnResponseReceived;        

        public BlackmagicSmartVideoHubCommunicator(RouterDevice device)
        {
            _device = device;
            OnResponseReceived += BlackmagicCommunicator_OnResponseReceived;
        }              

        public async Task<bool> Connect()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            while (true)
            {
                try
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                        throw new OperationCanceledException(_cancellationTokenSource.Token);
                    Debug.WriteLine("Connecting to Blackmagic...");
                    _tcpClient = new TcpClient();                    

                    var connectTask = _tcpClient.ConnectAsync(_device.IpAddress, _device.Port);
                    await Task.WhenAny(connectTask, Task.Delay(3000, _cancellationTokenSource.Token)).ConfigureAwait(false);

                    if (!_tcpClient.Connected)
                        continue;
                    
                    Debug.WriteLine("Blackmagic connected!");
                    _requestHandlerTask = RequestsHandler();

                    _stream = _tcpClient.GetStream();
                    _listenerTask = Listen();
                    Logger.Info("Blackmagic router connected and ready!");
                    break;
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("Connecting canceled");
                    break;
                }
                catch
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                        break;

                    Debug.WriteLine("Exception, attempting to reconnect to Blackmagic Router in 1s");
                    await Task.Delay(1000);
                }
            }
            return true;
        }       

        public void RequestRouterState()
        {
            if (_routerStateRequested)
                return;
            _routerStateRequested = true;
            AddToRequestQueue("PING:");
        }

        public void RequestCurrentInputPort()
        {
            if (_currentInputPortRequested)
                return;

            _currentInputPortRequested = true;
            AddToRequestQueue("VIDEO OUTPUT ROUTING:");
        }

        public void RequestInputPorts()
        {
            if (_inputPortsListRequested)
                return;

            _inputPortsListRequested = true;
            AddToRequestQueue("INPUT LABELS");
        }

        public void RequestOutputPorts()
        {
            if (_outputPortsListRequested)
                return;
            _outputPortsListRequested = true;
            AddToRequestQueue("OUTPUT LABELS");
        }

        public void SelectInput(int inPort)
        {
            var request = _device.OutputPorts.Aggregate("VIDEO OUTPUT ROUTING:\n", (current, outPort) => current + string.Concat(outPort, " ", inPort, "\n"));
            AddToRequestQueue(request);
        }

        private void BlackmagicCommunicator_OnResponseReceived(object sender, EventArgs<string> e)
        {
            _response += e.Item;

            while (_response.Contains("\n\n"))
            {
                var command = _response.Substring(0, _response.IndexOf("\n\n", StringComparison.Ordinal) + 2);
                _response = _response.Remove(0, _response.IndexOf("\n\n", StringComparison.Ordinal) + 2);

                ProcessCommand(command);
            }
        }

        private async Task RequestsHandler()
        {
            try
            {
                while (true)
                {
                    if (_requestsQueue.Count < 1)
                        await _requestHandlerSemaphore.WaitAsync(_cancellationTokenSource.Token);

                    if (_cancellationTokenSource.IsCancellationRequested)
                        throw new OperationCanceledException();

                    while (!_requestsQueue.IsEmpty)
                    {
                        if (!_tcpClient.Connected)
                            break;

                        if (!_requestsQueue.TryDequeue(out var request))
                            continue;

                        _sendTask = Send(request);
                        await _sendTask.ConfigureAwait(false);

                    }
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Info("Blackmagic request handler canceled");
                Debug.WriteLine("Blackmagic request handler canceled");
            }
            catch (Exception ex)
            {
                Logger.Error($"Unexpected exception in Blackmagic request handler {ex}");
                Debug.WriteLine($"Unexpected exception in Blackmagic request handler {ex}");
            }
        }

        private async Task<bool> Send(string message)
        {
            try
            {
                var data = System.Text.Encoding.ASCII.GetBytes(message.Last() != '\n' ? string.Concat(message, "\n\n") : string.Concat(message, "\n"));
                await _stream.WriteAsync(data, 0, data.Length, _cancellationTokenSource.Token).ConfigureAwait(false);
                Debug.WriteLine($"Blackmagic message sent: {message}");
                return true;
            }
            catch(TimeoutException)
            {
                Debug.WriteLine("Router send timeout.");
                Disconnect();
                return false;
            }
            catch
            {
                return false;
            }
        }

        private async Task Listen()
        {
            var bytesReceived = new byte[256];

            Debug.WriteLine("Blackmagic listener started!");
            while (true)
            {
                try
                {                    
                    var readTask = _stream.ReadAsync(bytesReceived, 0, bytesReceived.Length, _cancellationTokenSource.Token);
                    await Task.WhenAny(readTask, Task.Delay(-1, _cancellationTokenSource.Token)).ConfigureAwait(false);
                    
                    if (_cancellationTokenSource.IsCancellationRequested)
                        throw new OperationCanceledException(_cancellationTokenSource.Token);

                    if (!readTask.IsCompleted)
                        continue;

                    int bytes;
                    if ((bytes = readTask.Result) == 0)
                        continue;
                    var response = System.Text.Encoding.ASCII.GetString(bytesReceived, 0, bytes);
                    OnResponseReceived?.Invoke(this, new EventArgs<string>(response));
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("Listener canceled");
                    break;
                }
                catch (System.IO.IOException ioException)
                {
                    if (_tcpClient.Connected)
                    {
                        Debug.WriteLine($"Blackmagic listener encountered error: {ioException}");
                        continue;
                    }
                    Logger.Error(ioException, "Blackmagic listener was closed forcibly, attempting to reconnect to Blackmagic.");
                    Debug.WriteLine($"Blackmagic listener was closed forcibly: {ioException}\n Attempting to reconnect to Blackmagic...");

                    Disconnect();
                    break;
                }                
                catch (Exception ex)
                {
                    Logger.Error(ex, "Unknown Blackmagic listener error");
                    Debug.WriteLine($"Blackmagic listener error. {ex.Message}");
                }
            }
        }

        private void ProcessCommand(string localResponse)
        {
            IList<string> lines = localResponse.Split('\n').ToList();
            if (lines[0].StartsWith("NAK"))
                return;

            if (lines[0].StartsWith("ACK"))
            {
                lines.RemoveAt(1);
                lines.RemoveAt(0);
                if (lines[0] == "")
                {
                    OnRouterStateReceived?.Invoke(this, new EventArgs<IEnumerable<IRouterPort>>(null));
                    _routerStateRequested = false;
                    return;
                }

            }

            Debug.WriteLine($"Processing command: {lines[0]}");

            if (lines[0].Contains("INPUT LABELS"))
                IoListProcess(lines
                    .Skip(1)
                    .Where(param => !string.IsNullOrEmpty(param))
                    .ToList(),
                    ListTypeEnum.Input);
            else if (lines[0].Contains("OUTPUT LABELS"))
                IoListProcess(lines
                    .Skip(1)
                    .Where(param => !string.IsNullOrEmpty(param))
                    .ToList(),
                    ListTypeEnum.Output);
            else if (lines[0].Contains("OUTPUT ROUTING"))
                IoListProcess(lines
                    .Skip(1)
                    .Where(param => !string.IsNullOrEmpty(param))
                    .ToList(),
                    ListTypeEnum.CrosspointChange);

            Debug.WriteLine("Command processed");
        }

        private void IoListProcess(IList<string> listResponse, ListTypeEnum listType)
        {
            var ports = new List<IRouterPort>();
            var crosspoints = new List<Crosspoint>();

            foreach (var line in listResponse)
            {
                var lineParams = line.Split(' ');
                try
                {
                    switch (listType)
                    {
                        case ListTypeEnum.Input:
                        case ListTypeEnum.Output:
                        {
                            var split = line.Split(new []{' '}, 2, StringSplitOptions.RemoveEmptyEntries);
                                if (split.Length < 1 || !short.TryParse(split[0], out var numer))
                                    throw  new ArgumentException("Too few parameters");
                                ports.Add(new RouterPort(numer, split.ElementAtOrDefault(1) ?? string.Empty));
                                break;
                            }
                        case ListTypeEnum.CrosspointChange:
                        case ListTypeEnum.CrosspointStatus:
                            {
                                crosspoints.Add(new Crosspoint(short.Parse(lineParams[1]), short.Parse(lineParams[0])));
                                break;
                            }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to generate port from response. [Line: {line} message: {ex.Message}]");
                    Debug.WriteLine($"Failed to generate port from response. [\n{line}\n{ex.Message}]");
                }
            }

            switch (listType)
            {
                case ListTypeEnum.Input:
                    {
                        OnInputPortsListReceived?.Invoke(this, new EventArgs<IEnumerable<IRouterPort>>(ports));
                        _inputPortsListRequested = false;
                        break;
                    }

                case ListTypeEnum.Output:
                    {
                        OnOutputPortsListReceived?.Invoke(this, new EventArgs<IEnumerable<IRouterPort>>(ports));
                        _outputPortsListRequested = false;
                        break;
                    }

                case ListTypeEnum.CrosspointStatus:
                    {
                        OnInputPortChangeReceived?.Invoke(this, new EventArgs<IEnumerable<Crosspoint>>(crosspoints));
                        _currentInputPortRequested = false;
                        break;
                    }
                case ListTypeEnum.CrosspointChange:
                    {
                        OnInputPortChangeReceived?.Invoke(this, new EventArgs<IEnumerable<Crosspoint>>(crosspoints));
                        break;
                    }
            }
        }

        private void AddToRequestQueue(string request)
        {
            _requestsQueue.Enqueue(request);
            _requestHandlerSemaphore.Release();
        }        

        public void Disconnect()
        {
            _cancellationTokenSource.Cancel();
            _requestsQueue = new ConcurrentQueue<string>();
          
            _sendTask?.Wait();
            _listenerTask?.Wait();
            _requestHandlerTask?.Wait();
            OnRouterConnectionStateChanged?.Invoke(this, new EventArgs<bool>(false));
        }

        public void Dispose()
        {
            OnResponseReceived -= BlackmagicCommunicator_OnResponseReceived;
            Disconnect();
            _tcpClient?.Close();
            
            Debug.WriteLine("Blackmagic communicator disposed");
        }
    }
}
