using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TAS.Server.Router.Model;
using TAS.Common;
using TAS.Common.Interfaces;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace TAS.Server.Router.RouterCommunicators
{
    public class BlackmagicSVHCommunicator : IRouterCommunicator
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

        private ConcurrentQueue<string> _requestsQueue = new ConcurrentQueue<string>();
        private SemaphoreSlim _requestHandlerSemaphore = new SemaphoreSlim(0);
        private bool InputPortsListRequested;
        private bool CurrentInputPortRequested;
        private bool RouterStateRequested;
        private bool OutputPortsListRequested;        

        private readonly object _responseLock = new object();
        private string Response;

        public event EventHandler<EventArgs<IEnumerable<IRouterPort>>> OnRouterStateReceived;
        public event EventHandler<EventArgs<IEnumerable<IRouterPort>>> OnInputPortsListReceived;
        public event EventHandler<EventArgs<IEnumerable<IRouterPort>>> OnOutputPortsListReceived;
        public event EventHandler<EventArgs<bool>> OnRouterConnectionStateChanged;
        public event EventHandler<EventArgs<IEnumerable<Crosspoint>>> OnInputPortChangeReceived;
        private event EventHandler<RouterEventArgs> OnResponseReceived;        

        public BlackmagicSVHCommunicator(RouterDevice device)
        {
            _device = device;
            OnResponseReceived += BlackmagicCommunicator_OnResponseReceived;
        }

        private void BlackmagicCommunicator_OnResponseReceived(object sender, RouterEventArgs e)
        {
            string command = String.Empty;
            Response += e.Response;

            while (Response.Contains("\n\n"))
            {
                command = Response.Substring(0, Response.IndexOf("\n\n") + 2);
                Response = Response.Remove(0, Response.IndexOf("\n\n") + 2);

                ProcessCommand(command);
            }
        }

        private void RequestsHandler()
        {
            Task.Run(async () =>
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

                            if (_requestsQueue.TryDequeue(out var request))
                                Send(request);
                        }
                    }
                }
                catch (OperationCanceledException cEx)
                {
                    Logger.Info("Blackmagic request handler canceled");
                    Debug.WriteLine("Blackmagic request handler canceled");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Unexpected exception in Blackmagic request handler {ex}");
                    Debug.WriteLine($"Unexpected exception in Blackmagic request handler {ex}");
                }

            }, _cancellationTokenSource.Token);

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
                    OnRouterStateReceived?.Invoke(this, new EventArgs<IEnumerable<IRouterPort>>(null));
            }
                

            Debug.WriteLine($"Processing command: {lines[0]}");

            if (lines[0].Contains("INPUT LABELS"))
                IOListProcess(lines
                    .Skip(1)
                    .Where(param => !String.IsNullOrEmpty(param))
                    .ToList(),
                    Enums.ListType.Input);
            else if (lines[0].Contains("OUTPUT LABELS"))
                IOListProcess(lines
                    .Skip(1)
                    .Where(param => !String.IsNullOrEmpty(param))
                    .ToList(),
                    Enums.ListType.Output);
            else if (lines[0].Contains("OUTPUT ROUTING"))
                IOListProcess(lines
                    .Skip(1)
                    .Where(param => !String.IsNullOrEmpty(param))
                    .ToList(),
                    Enums.ListType.CrosspointChange);            
            
            Debug.WriteLine("Command processed");
        }

        private void IOListProcess(IList<string> listResponse, Enums.ListType listType)
        {
            IList<RouterPort> Ports = new List<RouterPort>();
            IList<Crosspoint> Crosspoints = new List<Crosspoint>();            

            foreach (var line in listResponse)
            {
                var lineParams = line.Split(' ');
                try
                {
                    switch (listType)
                    {
                        case Enums.ListType.Input:
                        case Enums.ListType.Output:
                            {
                                Ports.Add(new RouterPort(Int32.Parse(line.Split(' ').FirstOrDefault()), line.Remove(0, line.IndexOf(' ') + 1)));
                                break;
                            }                       
                        case Enums.ListType.CrosspointChange:
                        case Enums.ListType.CrosspointStatus:
                            {
                                Crosspoints.Add(new Crosspoint(Int32.Parse(lineParams[1]), Int32.Parse(lineParams[0])));                                
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

            switch (listType)
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
            }
        }

        public bool Connect()
        {
            Task.Run(async () =>
            {
                _cancellationTokenSource = new CancellationTokenSource();
                while (true)
                {
                    try
                    {
                        Debug.WriteLine("Connecting to Blackmagic...");
                        _tcpClient = new TcpClient();

                        if (!_tcpClient.ConnectAsync(_device.IP, _device.Port).Wait(3000))
                            continue;

                        if (!_tcpClient.Connected)
                            break;

                        IsConnected = true;
                        Debug.WriteLine("Blackmagic connected!");
                        RequestsHandler();

                        _stream = _tcpClient.GetStream();
                        Listen();
                        Logger.Info("Blackmagic router connected and ready!");
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
            }).Wait();

            return true;
        }       

        public void RequestRouterState()
        {
            if (RouterStateRequested)
                return;
            RouterStateRequested = true;
            AddToRequestQueue("PING:");
            return;
        }

        public void RequestCurrentInputPort()
        {
            if (CurrentInputPortRequested)
                return;

            CurrentInputPortRequested = true;
            AddToRequestQueue($"VIDEO OUTPUT ROUTING:");
        }

        public void RequestInputPorts()
        {
            if (InputPortsListRequested)
                return;

            InputPortsListRequested = true;
            AddToRequestQueue($"INPUT LABELS");
        }

        public void RequestOutputPorts()
        {
            if (OutputPortsListRequested)
                return;
            OutputPortsListRequested = true;
            AddToRequestQueue($"OUTPUT LABELS");
        }

        public void SelectInput(int inPort)
        {
            string request = "VIDEO OUTPUT ROUTING:\n";

            foreach (var outPort in _device.OutputPorts)
            {
                request += String.Concat(outPort, " ", inPort, "\n");
            }
            AddToRequestQueue(request);
        }

        private void AddToRequestQueue(string request)
        {
            _requestsQueue.Enqueue(request);
            _requestHandlerSemaphore.Release();
        }

        private bool Send(string message)
        {
            return Task.Run(async () =>
            {
                try
                {
                    byte[] data;
                    if (message.Last() != '\n')
                        data = Encoding.ASCII.GetBytes(String.Concat(message, "\n\n"));
                    else
                        data = Encoding.ASCII.GetBytes(String.Concat(message, "\n"));
                    await _stream.WriteAsync(data, 0, data.Length, _cancellationTokenSource.Token);
                    Debug.WriteLine($"Blackmagic message sent: {message}");
                    return true;
                }
                catch
                {
                    return false;
                }
            }).Result;
        }


        private async void Listen()
        {
            await Task.Run(async () =>
            {
                Byte[] bytesReceived = new Byte[256];
                string response = String.Empty;
                int bytes = 0;

                Debug.WriteLine("Blackmagic listener started!");
                while (true)
                {
                    try
                    {
                        if (_cancellationTokenSource.IsCancellationRequested)
                            throw new OperationCanceledException(_cancellationTokenSource.Token);


                        if ((bytes = await _stream.ReadAsync(bytesReceived, 0, bytesReceived.Length, _cancellationTokenSource.Token)) != 0)
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
                            Debug.WriteLine($"Blackmagic listener encountered error: {ioException}");
                            continue;
                        }

                        Debug.WriteLine("Blackmagic listener was closed forcibly: {ioException}\n Attempting to reconnect to Blackmagic...");
                        _cancellationTokenSource.Cancel();
                        IsConnected = false;
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to read Blackmagic response. {ex.Message}");
                    }
                }
            }, _cancellationTokenSource.Token);
        }

        public void Disconnect()
        {
            _cancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            Disconnect();
            _tcpClient?.Close();
            Debug.WriteLine("Blackmagic communicator disposed");
        }
    }
}
