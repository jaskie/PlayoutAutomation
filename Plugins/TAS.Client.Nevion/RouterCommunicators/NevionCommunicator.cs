using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TAS.Server.Router.Model;
using TAS.Common;

namespace TAS.Server.Router.RouterCommunicators
{
    public class NevionCommunicator : IRouterCommunicator
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private RouterDevice Device;

        public event EventHandler<RouterEventArgs> OnInputPortsListReceived;
        public event EventHandler<RouterEventArgs> OnOutputPortsListReceived;

        public event EventHandler<RouterEventArgs> OnInputPortChangeReceived;
        public event EventHandler<RouterEventArgs> OnOutputPortChangeReceived;

        private event EventHandler<RouterEventArgs> OnResponseReceived;

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
                        
            foreach (var line in listResponse)
            {                
                var lineParams = line.Split(' ');
                try
                {
                    var port = new RouterPort(Int32.Parse(lineParams[2].Trim('\"')), lineParams[3].Trim('\"'));
                    Ports.Add(port);
                    Debug.WriteLine($"Port {port.ID} added");
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
                        break;
                    }

                case Enums.ListType.Output:
                    {
                        OnOutputPortsListReceived?.Invoke(this, new RouterEventArgs(Ports));
                        break;
                    }

                case Enums.ListType.CrosspointStatus:
                case Enums.ListType.CrosspointChange:
                    {
                        OnInputPortChangeReceived?.Invoke(this, new RouterEventArgs(Ports));
                        break;
                    }
            }           
        }

        public async Task<bool> Connect(string ip, int port)
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
                        return false;
                    Debug.WriteLine("Connected!");

                    _stream = _tcpClient.GetStream();                   
                    Listen();
                    //Send("login admin password");
                    break;
                }
                catch
                {
                    if (cancellationTokenSource.IsCancellationRequested)
                        break;

                    Debug.WriteLine("Exception, attempting to reconnect to Nevion Router in 1s");
                    await Task.Delay(1000);
                }
            }
            return true;
        }      

        public bool RequestCurrentInputPort()
        {
            if (Send($"si l{Device.Level} {String.Join(",", Device.OutputPorts)}"))
                return true;
            return false;
        }        

        public bool RequestInputPorts()
        {
            if (Send($"inlist l{Device.Level}"))
                return true;
            return false;
        }

        public bool RequestOutputPorts()
        {
            if (Send($"outlist l{Device.Level}"))
                return true;
            return false;
        }

        public bool SwitchInput(RouterPort inPort, IEnumerable<RouterPort> outPorts)
        {
            if (Send($"x l{Device.Level} {inPort.ID} {String.Join(",", outPorts.Select(param => param.ID.ToString()))}"))
                return true;
            return false;
        }

        private bool Send(string message)
        {
            return Task.Run(async() =>
            {
                try
                {                    
                    var data = Encoding.ASCII.GetBytes(String.Concat(message, "\n\n"));
                    await _stream.WriteAsync(data, 0, data.Length, cancellationTokenSource.Token);
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
                        if (cancellationTokenSource.IsCancellationRequested)                                                    
                            throw new OperationCanceledException(cancellationTokenSource.Token);
                        
                            

                        if ((bytes = await _stream.ReadAsync(bytesReceived, 0, bytesReceived.Length, cancellationTokenSource.Token)) != 0)
                        {
                            response = System.Text.Encoding.ASCII.GetString(bytesReceived, 0, bytes);
                            //Debug.Write(response);
                            OnResponseReceived?.Invoke(this, new RouterEventArgs(response));

                            bytesReceived = new byte[256];
                            bytes = 0;
                        }
                    }
                    catch(OperationCanceledException canceledEx)
                    {
                        Debug.WriteLine($"Listener canceled");
                        break;
                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine($"Failed to read nevion response. {ex.Message}");
                    }
                }                
            }, cancellationTokenSource.Token);
        }

        public void Disconnect()
        {
            cancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            Disconnect();           
            _tcpClient?.Close();
            Debug.WriteLine("Nevion communicator disposed");
        }        
    }
}
