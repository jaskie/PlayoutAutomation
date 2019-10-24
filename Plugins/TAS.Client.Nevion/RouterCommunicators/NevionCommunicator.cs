using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TAS.Server.Router.Model;
using TAS.Common;
using System.Collections.Concurrent;
using TAS.Common.Interfaces;

namespace TAS.Server.Router.RouterCommunicators
{
    public class NevionCommunicator : IRouterCommunicator
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private TcpClient _tcpClient;
        
        private bool _isConnected;
        private bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (_isConnected == value)
                    return;

                _isConnected = value;
                OnRouterConnectionStateChanged?.Invoke(this, new EventArgs<bool>(value));
            }
        }

        private NetworkStream _stream;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private RouterDevice _device;

        private Task _requestHandlerTask;
        private Task _listenerTask;
        private Task<bool> _sendTask;

        private ConcurrentQueue<string> _requestsQueue = new ConcurrentQueue<string>();
        private SemaphoreSlim _requestHandlerSemaphore = new SemaphoreSlim(0);

        public event EventHandler<EventArgs<IEnumerable<IRouterPort>>> OnRouterStateReceived;

        public event EventHandler<EventArgs<IEnumerable<IRouterPort>>> OnInputPortsListReceived;
        public event EventHandler<EventArgs<IEnumerable<IRouterPort>>> OnOutputPortsListReceived;

        public event EventHandler<EventArgs<bool>> OnRouterConnectionStateChanged;
        public event EventHandler<EventArgs<IEnumerable<Crosspoint>>> OnInputPortChangeReceived;       

        private event EventHandler<RouterEventArgs> OnResponseReceived;

        private bool InputPortsListRequested;
        private bool CurrentInputPortRequested;
        private bool InputSignalPresenceRequested;
        private bool OutputPortsListRequested;
        private bool LoginRequested;

        private readonly object _responseLock = new object();
        private string Response;

        public NevionCommunicator(RouterDevice device)
        {
            _device = device;               
            OnResponseReceived += NevionCommunicator_OnResponseReceived;
        }

        private void NevionCommunicator_OnResponseReceived(object sender, RouterEventArgs e)
        {
            string command = String.Empty;
            Response += e.Response;            

            while (Response.Contains("\n\n"))
            {                
                command = Response.Substring(0, Response.IndexOf("\n\n") + 2);
                Response = Response.Remove(0, Response.IndexOf("\n\n") + 2);
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
            catch (OperationCanceledException cEx)
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

            //Debug.WriteLine($"Processing command: {lines[0]}");

            if (lines[0].Contains("inlist"))
                IOListProcess(lines
                    .Skip(1)
                    .Where(param => !String.IsNullOrEmpty(param))
                    .ToList(),
                    Enums.ListType.Input);
            else if (lines[0].Contains("outlist"))
                IOListProcess(lines
                    .Skip(1)
                    .Where(param => !String.IsNullOrEmpty(param))
                    .ToList(),
                    Enums.ListType.Output);
            else if (lines[0].Contains("si"))
                IOListProcess(lines
                    .Skip(1)
                    .Where(param => !String.IsNullOrEmpty(param))
                    .ToList(),
                    Enums.ListType.CrosspointStatus);
            else if (lines[0].Contains("sspi"))
                IOListProcess(lines
                    .Skip(1)
                    .Where(param => !String.IsNullOrEmpty(param))
                    .ToList(),
                    Enums.ListType.SignalPresence);
            else if (lines[0].Contains("login"))
                if (lines[1].Contains("ok"))
                    Logger.Info("Nevion login ok");
                else
                    Logger.Error("Nevion login incorrect");

            else if (lines[0].StartsWith("%"))
                IOListProcess(lines
                    .Skip(1)
                    .Where(param => !String.IsNullOrEmpty(param) && param != "%")
                    .ToList(),
                    Enums.ListType.CrosspointChange);
            

            //Debug.WriteLine("Command processed");

        }        

        private void IOListProcess(IList<string> listResponse, Enums.ListType listType)
        {
            IList<RouterPort> Ports = new List<RouterPort>();
            IList<Crosspoint> Crosspoints = new List<Crosspoint>();
            RouterPort port = null;
            Crosspoint crossPoint = null;

            foreach (var line in listResponse)
            {                
                var lineParams = line.Split(' ');
                try
                {
                    switch(listType)
                    {
                        case Enums.ListType.Input:
                        case Enums.ListType.Output:
                            {
                                Ports.Add(new RouterPort(Int32.Parse(lineParams[2].Trim('\"')), lineParams[3].Trim('\"')));
                                break;
                            }
                        case Enums.ListType.SignalPresence:
                            {
                                Ports.Add(new RouterPort(Int32.Parse(lineParams[2].Trim('\"')), lineParams[3].Trim('\"') == "p" ? true : false));
                                break;
                            }
                        case Enums.ListType.CrosspointChange:
                        case Enums.ListType.CrosspointStatus:
                            {
                                if (lineParams[0] == "x" && lineParams[1].StartsWith("l"))
                                    Crosspoints.Add(new Crosspoint(Int32.Parse(lineParams[2].Trim('\"')), Int32.Parse(lineParams[3].Trim('\"'))));
                                else continue;
                                break;
                            }
                    }                    
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to generate port from response. [\n{line}\n{ex.Message}]");
                    Debug.WriteLine($"Failed to generate port from response. [\n{line}\n{ex.Message}]");
                }                
            }
            
            switch(listType)
            {
                case Enums.ListType.Input:
                    {
                        OnInputPortsListReceived?.Invoke(this, new EventArgs<IEnumerable<IRouterPort>>(Ports));
                        InputPortsListRequested = false;
                        break;
                    }

                case Enums.ListType.Output:
                    {
                        OnOutputPortsListReceived?.Invoke(this, new EventArgs<IEnumerable<IRouterPort>>(Ports));
                        OutputPortsListRequested = false;
                        break;
                    }

                case Enums.ListType.CrosspointStatus:
                    {
                        OnInputPortChangeReceived?.Invoke(this, new EventArgs<IEnumerable<Crosspoint>>(Crosspoints));
                        CurrentInputPortRequested = false;
                        break;
                    }
                case Enums.ListType.CrosspointChange:
                    {
                        OnInputPortChangeReceived?.Invoke(this, new EventArgs<IEnumerable<Crosspoint>>(Crosspoints));                        
                        break;
                    }

                case Enums.ListType.SignalPresence:
                    {
                        OnRouterStateReceived?.Invoke(this, new EventArgs<IEnumerable<IRouterPort>>(Ports));
                        InputSignalPresenceRequested = false;
                        break;
                    }
            }           
        }

        public async Task<bool> Connect()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            while (true)
            {
                try
                {
                    Debug.WriteLine("Connecting to Nevion...");
                    _tcpClient = new TcpClient();

                    var connectTask = _tcpClient.ConnectAsync(_device.IP, _device.Port);
                    await Task.WhenAny(connectTask, Task.Delay(3000, _cancellationTokenSource.Token)).ConfigureAwait(false);

                    if (!_tcpClient.Connected)
                        continue;

                    IsConnected = true;
                    Debug.WriteLine("Nevion connected!");
                    _requestHandlerTask = RequestsHandler();

                    _stream = _tcpClient.GetStream();
                    _listenerTask = Listen();
                    Logger.Info("Nevion router connected and ready!");

                    Login();
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
            if (LoginRequested)
                return;
            LoginRequested = true;
            AddToRequestQueue($"login {_device.Login} {_device.Password}");            
        }

        public void RequestRouterState()
        {
            if (InputSignalPresenceRequested)
                return;

            InputSignalPresenceRequested = true;
            AddToRequestQueue($"sspi l{_device.Level}");            
        }

        public void RequestCurrentInputPort()
        {
            if (CurrentInputPortRequested)
                return;

            CurrentInputPortRequested = true;
            AddToRequestQueue($"si l{_device.Level} {String.Join(",", _device.OutputPorts)}");            
        }        

        public void RequestInputPorts()
        {
            if (InputPortsListRequested)
                return;

            InputPortsListRequested = true;
            AddToRequestQueue($"inlist l{_device.Level}");            
        }

        public void RequestOutputPorts()
        {
            if (OutputPortsListRequested)
                return;
            OutputPortsListRequested = true;
            AddToRequestQueue($"outlist l{_device.Level}");               
        }

        public void SelectInput(int inPort)
        {
           AddToRequestQueue($"x l{_device.Level} {inPort} {String.Join(",", _device.OutputPorts.Select(param => param.ToString()))}");            
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
                var data = Encoding.ASCII.GetBytes(String.Concat(message, "\n\n"));
                await _stream.WriteAsync(data, 0, data.Length, _cancellationTokenSource.Token).ConfigureAwait(false);
                Debug.WriteLine($"Nevion message sent: {message}");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task Listen()
        {
            Byte[] bytesReceived = new Byte[256];
            string response = String.Empty;
            int bytes = 0;
            Task<int> readTask = null;

            Debug.WriteLine("Nevion listener started!");
            while (true)
            {
                try
                {
                    readTask = _stream.ReadAsync(bytesReceived, 0, bytesReceived.Length, _cancellationTokenSource.Token);
                    await Task.WhenAny(readTask, Task.Delay(-1, _cancellationTokenSource.Token)).ConfigureAwait(false);

                    if (_cancellationTokenSource.IsCancellationRequested)
                        throw new OperationCanceledException(_cancellationTokenSource.Token);

                    if (!readTask.IsCompleted)
                        continue;

                    if ((bytes = readTask.Result) != 0)
                    {
                        response = System.Text.Encoding.ASCII.GetString(bytesReceived, 0, bytes);
                        OnResponseReceived?.Invoke(this, new RouterEventArgs(response));

                        bytes = 0;
                    }
                }
                catch (OperationCanceledException cancelEx)
                {
                    Debug.WriteLine($"Listener canceled");
                    break;
                }
                catch (System.IO.IOException ioException)
                {
                    if (_tcpClient.Connected)
                    {
                        Debug.WriteLine($"Nevion listener encountered error: {ioException}");
                        continue;
                    }

                    Debug.WriteLine("Nevion listener was closed forcibly: {ioException}\n Attempting to reconnect to Nevion...");
                    _cancellationTokenSource.Cancel();
                    IsConnected = false;
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

            _sendTask?.Wait();
            _listenerTask?.Wait();
            _requestHandlerTask?.Wait();
        }

        public void Dispose()
        {
            OnResponseReceived -= NevionCommunicator_OnResponseReceived;
            Disconnect();           
            _tcpClient?.Close();
            
            Debug.WriteLine("Nevion communicator disposed");
        }        
    }
}
