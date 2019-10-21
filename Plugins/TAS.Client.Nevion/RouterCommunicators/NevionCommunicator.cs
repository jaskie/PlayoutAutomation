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
        private RouterDevice Device;
        
        private ConcurrentQueue<string> _requestsQueue = new ConcurrentQueue<string>();
        private SemaphoreSlim _requestHandlerSemaphore = new SemaphoreSlim(0);

        public event EventHandler<EventArgs<IEnumerable<IRouterPort>>> OnInputSignalPresenceListReceived;

        public event EventHandler<EventArgs<IEnumerable<IRouterPort>>> OnInputPortsListReceived;
        public event EventHandler<EventArgs<IEnumerable<IRouterPort>>> OnOutputPortsListReceived;

        public event EventHandler<EventArgs<bool>> OnRouterConnectionStateChanged;
        public event EventHandler<EventArgs<IEnumerable<IRouterPort>>> OnInputPortChangeReceived;       

        private event EventHandler<RouterEventArgs> OnResponseReceived;

        private bool InputPortsListRequested;
        private bool CurrentInputPortRequested;
        private bool InputSignalPresenceRequested;
        private bool OutputPortsListRequested;

        private readonly object _responseLock = new object();
        private string Response;

        public NevionCommunicator(RouterDevice device)
        {
            Device = device;   
            
            OnResponseReceived += NevionCommunicator_OnResponseReceived;
        }

        private void NevionCommunicator_OnResponseReceived(object sender, RouterEventArgs e)
        {
            string command = String.Empty;
            Response += e.Response;            

            while (Response.Contains("\n\n"))
            {
                //Debug.WriteLine($"{Response.IndexOf("\n\n")}/{Response.Length}");
                command = Response.Substring(0, Response.IndexOf("\n\n") + 2);
                Response = Response.Remove(0, Response.IndexOf("\n\n") + 2);
                //Debug.WriteLine($"[{command}]");
                ProcessCommand(command);                
            }            
        }

        private void RequestsHandler()
        {
            Task.Run(async() => 
            {
                try
                {
                    while (true)
                    {                       
                        if (_requestsQueue.Count < 1)
                            await _requestHandlerSemaphore.WaitAsync(_cancellationTokenSource.Token);

                        if (_cancellationTokenSource.IsCancellationRequested)
                            throw new OperationCanceledException();

                        while(!_requestsQueue.IsEmpty)
                        {
                            if (!_tcpClient.Connected)
                                break;            
                            
                            if (_requestsQueue.TryDequeue(out var request))
                                Send(request);
                        }                                              
                        
                    }
                }
                catch(OperationCanceledException cEx)
                {
                    Debug.WriteLine("Nevion request handler canceled");
                }
                catch(Exception ex)
                {
                    Debug.WriteLine($"Unexpected exception in Nevion request handler {ex}");
                }
                
            }, _cancellationTokenSource.Token);
            
        }

        private void ProcessCommand(string localResponse)
        {
            IList<string> lines = localResponse.Split('\n');            
            if (!lines[0].StartsWith("?") && !lines[0].StartsWith("%"))
                return;

            Debug.WriteLine($"Processing command: {lines[0]}");
           
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

            else if (lines[0].StartsWith("%"))
                IOListProcess(lines                                       
                    .Skip(1)
                    .Where(param=> !String.IsNullOrEmpty(param) && param != "%")
                    .ToList(),
                    Enums.ListType.CrosspointChange);
            

            Debug.WriteLine("Command processed");

        }        

        private void IOListProcess(IList<string> listResponse, Enums.ListType listType)
        {
            IList<RouterPort> Ports = new List<RouterPort>();
            RouterPort port = null;

            foreach (var line in listResponse)
            {                
                var lineParams = line.Split(' ');
                try
                {                    
                    if (listType != Enums.ListType.SignalPresence)
                    {                        
                        port = new RouterPort(Int32.Parse(lineParams[2].Trim('\"')), lineParams[3].Trim('\"'));
                        //Debug.WriteLine($"Port {port.PortID} added");
                    }                      
                    else
                        port = new RouterPort(Int32.Parse(lineParams[2].Trim('\"')), lineParams[3].Trim('\"') == "p" ? true : false);

                    Ports.Add(port);
                    
                }
                catch (Exception ex)
                {
                    //Debug.WriteLine($"Failed to generate port from response. [\n{line}\n{ex.Message}]");
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
                        OnInputPortChangeReceived?.Invoke(this, new EventArgs<IEnumerable<IRouterPort>>(Ports));
                        CurrentInputPortRequested = false;
                        break;
                    }
                case Enums.ListType.CrosspointChange:
                    {
                        OnInputPortChangeReceived?.Invoke(this, new EventArgs<IEnumerable<IRouterPort>>(Ports));                        
                        break;
                    }

                case Enums.ListType.SignalPresence:
                    {
                        OnInputSignalPresenceListReceived?.Invoke(this, new EventArgs<IEnumerable<IRouterPort>>(Ports));
                        InputSignalPresenceRequested = false;
                        break;
                    }
            }           
        }

        public bool Connect(string ip, int port)
        {
            Task.Run(async() =>
            {
                _cancellationTokenSource = new CancellationTokenSource();
                while (true)
                {
                    try
                    {
                        Debug.WriteLine("Connecting to Nevion...");
                        _tcpClient = new TcpClient();

                        if (!_tcpClient.ConnectAsync(ip, port).Wait(3000))
                            continue;

                        if (!_tcpClient.Connected)
                            break;

                        IsConnected = true;
                        Debug.WriteLine("Connected!");
                        RequestsHandler();

                        _stream = _tcpClient.GetStream();
                        Listen();

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
            }).Wait();

            return true;
        }

        public void RequestSignalPresence()
        {
            if (InputSignalPresenceRequested)
                return;

            InputSignalPresenceRequested = true;
            _requestsQueue.Enqueue($"sspi l{Device.Level}");
            _requestHandlerSemaphore.Release();
        }

        public void RequestCurrentInputPort()
        {
            if (CurrentInputPortRequested)
                return;

            CurrentInputPortRequested = true;
            _requestsQueue.Enqueue($"si l{Device.Level} {String.Join(",", Device.OutputPorts)}");
            _requestHandlerSemaphore.Release();
        }        

        public void RequestInputPorts()
        {
            if (InputPortsListRequested)
                return;

            InputPortsListRequested = true;
            _requestsQueue.Enqueue($"inlist l{Device.Level}");
            _requestHandlerSemaphore.Release();
        }

        public void RequestOutputPorts()
        {
            if (OutputPortsListRequested)
                return;
            OutputPortsListRequested = true;
            _requestsQueue.Enqueue($"outlist l{Device.Level}");
            _requestHandlerSemaphore.Release();             
        }

        public void SelectInput(int inPort, IEnumerable<IRouterPort> outPorts)
        {
            _requestsQueue.Enqueue($"x l{Device.Level} {inPort} {String.Join(",", outPorts.Select(param => param.PortID.ToString()))}");
            _requestHandlerSemaphore.Release();
        }

        private bool Send(string message)
        {
            return Task.Run(async() =>
            {
                try
                {                    
                    var data = Encoding.ASCII.GetBytes(String.Concat(message, "\n\n"));
                    await _stream.WriteAsync(data, 0, data.Length, _cancellationTokenSource.Token);
                    Debug.WriteLine($"Nevion message sent: {message}");
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
            await Task.Run(async() =>
            {
                Byte[] bytesReceived = new Byte[256];
                string response = String.Empty;
                int bytes = 0;
                
                Debug.WriteLine("Nevion listener started!");
                while (true)
                {
                    try
                    {
                        if (_cancellationTokenSource.IsCancellationRequested)                                                    
                            throw new OperationCanceledException(_cancellationTokenSource.Token);
                                                    

                        if ((bytes = await _stream.ReadAsync(bytesReceived, 0, bytesReceived.Length, _cancellationTokenSource.Token)) != 0)
                        {
                            response = System.Text.Encoding.ASCII.GetString(bytesReceived, 0, bytes);
                            //Debug.Write($"[{response}]");
                            OnResponseReceived?.Invoke(this, new RouterEventArgs(response));
                            
                            bytes = 0;
                        }
                    }
                    catch(OperationCanceledException cancelEx)
                    {
                        Debug.WriteLine($"Listener canceled");
                        break;
                    }
                    catch(System.IO.IOException ioException)
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
                    catch(Exception ex)
                    {
                        Debug.WriteLine($"Failed to read nevion response. {ex.Message}");
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
            Debug.WriteLine("Nevion communicator disposed");
        }        
    }
}
