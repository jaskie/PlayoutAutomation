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
    public class NevionCommunicator : IRouterCommunicator
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

        private event EventHandler<EventArgs<string>> OnResponseReceived;

        private bool _inputPortsListRequested;
        private bool _currentInputPortRequested;
        private bool _inputSignalPresenceRequested;
        private bool _outputPortsListRequested;
        private bool _loginRequested;

        private string _response;

        public NevionCommunicator(RouterDevice device)
        {
            _device = device;               
            OnResponseReceived += NevionCommunicator_OnResponseReceived;
        }

        private void NevionCommunicator_OnResponseReceived(object sender, EventArgs<string> e)
        {
            _response += e.Item;            
            while (_response.Contains("\n\n"))
            {                
                var command = _response.Substring(0, _response.IndexOf("\n\n", StringComparison.Ordinal) + 2);
                _response = _response.Remove(0, _response.IndexOf("\n\n", StringComparison.Ordinal) + 2);
                Debug.WriteLine(command);
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
                Debug.WriteLine("Nevion request handler canceled");
            }
            catch (Exception ex)
            {
                Logger.Error($"Unexpected exception in Nevion request handler {ex}");
                Debug.WriteLine($"Unexpected exception in Nevion request handler {ex}");
            }
        }

        private void ProcessCommand(string localResponse)
        {
            IList<string> lines = localResponse.Split('\n');            
            if (!lines[0].StartsWith("?") && !lines[0].StartsWith("%"))
                return;

            if (lines[0].Contains("inlist"))
                IoListProcess(lines
                    .Skip(1)
                    .Where(param => !string.IsNullOrEmpty(param))
                    .ToList(),
                    ListTypeEnum.Input);
            else if (lines[0].Contains("outlist"))
                IoListProcess(lines
                    .Skip(1)
                    .Where(param => !string.IsNullOrEmpty(param))
                    .ToList(),
                    ListTypeEnum.Output);
            else if (lines[0].Contains("si"))
                IoListProcess(lines
                    .Skip(1)
                    .Where(param => !string.IsNullOrEmpty(param))
                    .ToList(),
                    ListTypeEnum.CrosspointStatus);
            else if (lines[0].Contains("sspi"))
                IoListProcess(lines
                    .Skip(1)
                    .Where(param => !string.IsNullOrEmpty(param))
                    .ToList(),
                    ListTypeEnum.SignalPresence);
            else if (lines[0].Contains("login"))
                if (lines[1].Contains("ok"))
                    Logger.Info("Nevion login ok");
                else
                    Logger.Error("Nevion login incorrect");

            else if (lines[0].StartsWith("%"))
                IoListProcess(lines
                    .Skip(1)
                    .Where(param => !string.IsNullOrEmpty(param) && param != "%")
                    .ToList(),
                    ListTypeEnum.CrosspointChange);
            

            //Debug.WriteLine("Command processed");

        }        

        private void IoListProcess(IList<string> listResponse, ListTypeEnum listType)
        {
            IList<IRouterPort> ports = new List<IRouterPort>();
            IList<Crosspoint> crosspoints = new List<Crosspoint>();

            foreach (var line in listResponse)
            {                
                var lineParams = line.Split(new []{' '}, StringSplitOptions.RemoveEmptyEntries);
                try
                {
                    switch (listType)
                    {
                        case ListTypeEnum.Input:
                        case ListTypeEnum.Output:
                            ports.Add(new RouterPort(short.Parse(lineParams[2].Trim('\"')), lineParams[3].Trim('\"')));
                            break;
                        case ListTypeEnum.SignalPresence:
                            ports.Add(new RouterPort(short.Parse(lineParams[2].Trim('\"')),
                                lineParams[3].Trim('\"') == "p"));
                            break;
                        case ListTypeEnum.CrosspointChange:
                        case ListTypeEnum.CrosspointStatus:
                            if (lineParams[0] == "x" && lineParams[1].StartsWith("l", StringComparison.Ordinal))
                                crosspoints.Add(new Crosspoint(short.Parse(lineParams[2].Trim('\"')),
                                    short.Parse(lineParams[3].Trim('\"'))));
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to generate port from response");
                    Debug.WriteLine($"Failed to generate port from response [\n{line}\n{ex.Message}]");
                }                
            }
            
            switch(listType)
            {
                case ListTypeEnum.Input:
                        OnInputPortsListReceived?.Invoke(this, new EventArgs<IEnumerable<IRouterPort>>(ports));
                        _inputPortsListRequested = false;
                        break;

                case ListTypeEnum.Output:
                        OnOutputPortsListReceived?.Invoke(this, new EventArgs<IEnumerable<IRouterPort>>(ports));
                        _outputPortsListRequested = false;
                        break;
                case ListTypeEnum.CrosspointStatus:
                        OnInputPortChangeReceived?.Invoke(this, new EventArgs<IEnumerable<Crosspoint>>(crosspoints));
                        _currentInputPortRequested = false;
                        break;
                case ListTypeEnum.CrosspointChange:
                        OnInputPortChangeReceived?.Invoke(this, new EventArgs<IEnumerable<Crosspoint>>(crosspoints));                        
                        break;
                case ListTypeEnum.SignalPresence:
                        OnRouterStateReceived?.Invoke(this, new EventArgs<IEnumerable<IRouterPort>>(ports));
                        _inputSignalPresenceRequested = false;
                        break;
            }           
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

                    Debug.WriteLine("Connecting to Nevion...");
                    _tcpClient = new TcpClient();

                    var connectTask = _tcpClient.ConnectAsync(_device.IpAddress, _device.Port);
                    await Task.WhenAny(connectTask, Task.Delay(3000, _cancellationTokenSource.Token)).ConfigureAwait(false);

                    if (!_tcpClient.Connected)
                        continue;
                   
                    Debug.WriteLine("Nevion connected!");
                    _requestHandlerTask = RequestsHandler();

                    _stream = _tcpClient.GetStream();
                    _listenerTask = Listen();
                    Logger.Info("Nevion router connected and ready!");

                    Login();
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

                    Debug.WriteLine("Exception, attempting to reconnect to Nevion Router in 1s");
                    await Task.Delay(1000);
                }
            }
            return true;
        }

        public void Login()
        {
            if (_loginRequested)
                return;
            _loginRequested = true;
            AddToRequestQueue($"login {_device.Login} {_device.Password}");            
        }

        public void RequestRouterState()
        {
            if (_inputSignalPresenceRequested)
                return;

            _inputSignalPresenceRequested = true;
            AddToRequestQueue($"sspi l{_device.Level}");            
        }

        public void RequestCurrentInputPort()
        {
            if (_currentInputPortRequested)
                return;

            _currentInputPortRequested = true;
            AddToRequestQueue($"si l{_device.Level} {string.Join(",", _device.OutputPorts)}");            
        }        

        public void RequestInputPorts()
        {
            if (_inputPortsListRequested)
                return;

            _inputPortsListRequested = true;
            AddToRequestQueue($"inlist l{_device.Level}");            
        }

        public void RequestOutputPorts()
        {
            if (_outputPortsListRequested)
                return;
            _outputPortsListRequested = true;
            AddToRequestQueue($"outlist l{_device.Level}");               
        }

        public void SelectInput(int inPort)
        {
           AddToRequestQueue($"x l{_device.Level} {inPort} {string.Join(",", _device.OutputPorts.Select(param => param.ToString()))}");            
        }

        private void AddToRequestQueue(string request)
        {
            _requestsQueue.Enqueue(request);
            _requestHandlerSemaphore.Release();
        }

        private async Task<bool> Send(string message)
        {
            try
            {
                var data = System.Text.Encoding.ASCII.GetBytes(string.Concat(message, "\n\n"));
                await _stream.WriteAsync(data, 0, data.Length, _cancellationTokenSource.Token).ConfigureAwait(false);
                Debug.WriteLine($"Nevion message sent: {message}");
                return true;
            }
            catch(TimeoutException)
            {
                Disconnect();
                return false;
            }
            catch(Exception)
            {
                return false;
            }
        }

        private async Task Listen()
        {
            var bytesReceived = new byte[256];

            Debug.WriteLine("Nevion listener started!");
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

                    var bytes = readTask.Result;
                    if (bytes == 0) continue;
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
                        Debug.WriteLine($"Nevion listener encountered error: {ioException}");
                        continue;
                    }

                    Debug.WriteLine($"Nevion listener was closed forcibly: {ioException}\n Attempting to reconnect to Nevion...");
                    Disconnect();
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to read nevion response. {ex.Message}");
                }
            }
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
            OnResponseReceived -= NevionCommunicator_OnResponseReceived;
            Disconnect();           
            _tcpClient?.Close();
            Debug.WriteLine("Nevion communicator disposed");
        }

        public event EventHandler<EventArgs<IEnumerable<IRouterPort>>> OnRouterStateReceived;

        public event EventHandler<EventArgs<IEnumerable<IRouterPort>>> OnInputPortsListReceived;
        public event EventHandler<EventArgs<IEnumerable<IRouterPort>>> OnOutputPortsListReceived;

        public event EventHandler<EventArgs<bool>> OnRouterConnectionStateChanged;
        public event EventHandler<EventArgs<IEnumerable<Crosspoint>>> OnInputPortChangeReceived;

    }
}
