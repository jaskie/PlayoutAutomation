using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TAS.Client.Router.Model;
using TAS.Common;

namespace TAS.Client.Router.RouterCommunicators
{
    public class NevionCommunicator : IRouterCommunicator
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private CancellationTokenSource cancellationToken = new CancellationTokenSource();

        public event EventHandler<RouterEventArgs> OnInputPortsListReceived;
        public event EventHandler<RouterEventArgs> OnOutputPortsListReceived;

        public event EventHandler<RouterEventArgs> OnInputPortChangeReceived;
        public event EventHandler<RouterEventArgs> OnOutputPortChangeReceived;

        private event EventHandler<RouterEventArgs> OnResponseReceived;

        private readonly object _responseLock = new object();
        private string Response;

        public NevionCommunicator()
        {
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
            Debug.Write($"Processing command... First line: {lines[0]}");
            if (!lines[0].StartsWith("?"))
                return;                        
            
            if (lines[0].Contains("inlist"))
                IOListProcess(lines.Skip(1).ToList(), Enums.ListType.Input);
            else if (lines[0].Contains("outlist"))
                IOListProcess(lines.Skip(1).ToList(), Enums.ListType.Output);
            Debug.Write("Command processed");

        }

        private void IOListProcess(IList<string> listResponse, Enums.ListType listType)
        {
            IList<RouterPort> Ports = new List<RouterPort>();
            foreach (var line in listResponse)
            {
                var lineParams = line.Split(' ');
                try
                {
                    var port = new RouterPort(Int32.Parse(lineParams[2]), lineParams[4]);
                    Ports.Add(port);
                    Debug.WriteLine($"Port {port.ID} added");
                }
                catch
                {
                    Debug.WriteLine($"Failed to generate port from response. \n {line}");
                }                
            }
            
            if (listType == Enums.ListType.Input)
            {
                Debug.WriteLine("InputList event send");
                OnInputPortsListReceived?.Invoke(this, new RouterEventArgs(Ports));
            }
                
            else if (listType == Enums.ListType.Output)
                OnOutputPortsListReceived?.Invoke(this, new RouterEventArgs(Ports));
        }

        public async Task<bool> Connect(string ip, int port)
        {
            while (true)
            { 
                try
                {
                    Debug.WriteLine("Connecting to Nevion...");
                    _tcpClient = new TcpClient(ip, port);
                    
                    if (!_tcpClient.Connected)
                        return false;
                    Debug.WriteLine("Connected!");

                    _stream = _tcpClient.GetStream();
                    Send("login admin password");
                    Listen();                   

                    break;
                }
                catch
                {
                    Debug.WriteLine("Attempting to reconnect to Nevion Router in 1s");
                    await Task.Delay(1000);
                }
            }
            return true;
        }

        public bool RequestInputPorts()
        {
            if (Send("inlist l1"))
                return true;
            return false;
        }

        public bool SwitchInput(RouterPort inPort, IEnumerable<RouterPort> outPorts)
        {
            if (Send($"x l1 {inPort} {String.Join(",", outPorts.Select(param => param.ToString()))}"))
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
                    await _stream.WriteAsync(data, 0, data.Length, cancellationToken.Token);
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
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        bytes = await _stream.ReadAsync(bytesReceived, 0, bytesReceived.Length, cancellationToken.Token);
                        if (bytes != 0)
                        {
                            response = System.Text.Encoding.ASCII.GetString(bytesReceived, 0, bytes);
                            Debug.Write($"{response}");
                            OnResponseReceived?.Invoke(this, new RouterEventArgs(response));
                            
                            bytesReceived = new byte[256];
                            bytes = 0;
                        }
                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine($"Failed to read nevion response. {ex.Message}");
                    }
                }
                Debug.WriteLine("Nevion listener stopped!");
            });
        }

        public void Disconnect()
        {
            cancellationToken.Cancel();
        }

        public void Dispose()
        {
            Disconnect();
            _stream.Close();
            _tcpClient.Close();
        }        
    }
}
