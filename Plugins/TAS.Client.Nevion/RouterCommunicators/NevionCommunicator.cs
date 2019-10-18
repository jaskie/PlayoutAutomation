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

namespace TAS.Server.Router.RouterCommunicators
{
    public class NevionCommunicator : IRouterCommunicator
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private RouterDevice Device;
        //private SynchronizedCollection<string> _requestsQueue = new SynchronizedCollection<string>();
        private ConcurrentQueue<string> _requestsQueue = new ConcurrentQueue<string>();
        private SemaphoreSlim _requestHandlerSemaphore = new SemaphoreSlim(0);

        public event EventHandler<RouterEventArgs> OnInputSignalPresenceListReceived;

        public event EventHandler<RouterEventArgs> OnInputPortsListReceived;
        public event EventHandler<RouterEventArgs> OnOutputPortsListReceived;

        public event EventHandler<RouterEventArgs> OnInputPortChangeReceived;
        public event EventHandler<RouterEventArgs> OnOutputPortChangeReceived;

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
            lock (_responseLock)
            {
                if (e.Response.Contains("\n\n"))
                {
                    string localResponse = String.Concat(Response, e.Response);
                    Response = String.Empty;

                    ProcessCommand(localResponse);
                }
                else
                    Response = String.Concat(Response, e.Response);
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
                        Debug.WriteLine($"Port {port.ID} added");
                    }                      
                    else
                        port = new RouterPort(Int32.Parse(lineParams[2].Trim('\"')), lineParams[3].Trim('\"') == "p" ? true : false);

                    Ports.Add(port);
                    
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to generate port from response. \n{line}\n{ex.Message}");
                }                
            }
            
            switch(listType)
            {
                case Enums.ListType.Input:
                    {
                        OnInputPortsListReceived?.Invoke(this, new RouterEventArgs(Ports));
                        InputPortsListRequested = false;
                        break;
                    }

                case Enums.ListType.Output:
                    {
                        OnOutputPortsListReceived?.Invoke(this, new RouterEventArgs(Ports));
                        OutputPortsListRequested = false;
                        break;
                    }

                case Enums.ListType.CrosspointStatus:
                    {
                        OnInputPortChangeReceived?.Invoke(this, new RouterEventArgs(Ports));
                        CurrentInputPortRequested = false;
                        break;
                    }
                case Enums.ListType.CrosspointChange:
                    {
                        OnInputPortChangeReceived?.Invoke(this, new RouterEventArgs(Ports));                        
                        break;
                    }

                case Enums.ListType.SignalPresence:
                    {
                        OnInputSignalPresenceListReceived?.Invoke(this, new RouterEventArgs(Ports));
                        InputSignalPresenceRequested = false;
                        break;
                    }
            }           
        }

        public bool Connect(string ip, int port)
        {
            Task.Run(async() =>
            {
                while (true)
                {
                    try
                    {
                        Debug.WriteLine("Connecting to Nevion...");
                        _tcpClient = new TcpClient();
                        if (!_tcpClient.ConnectAsync(ip, port).Wait(3000))
                            continue;


                        if (!_tcpClient.Connected)
                            return;

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

        public void SwitchInput(RouterPort inPort, IEnumerable<RouterPort> outPorts)
        {
            _requestsQueue.Enqueue($"x l{Device.Level} {inPort.ID} {String.Join(",", outPorts.Select(param => param.ID.ToString()))}");
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
                            //Debug.Write(response);
                            OnResponseReceived?.Invoke(this, new RouterEventArgs(response));

                            bytesReceived = new byte[256];
                            bytes = 0;
                        }
                    }
                    catch(OperationCanceledException cancelEx)
                    {
                        Debug.WriteLine($"Listener canceled");
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
