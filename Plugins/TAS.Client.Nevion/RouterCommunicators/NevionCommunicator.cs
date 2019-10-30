using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TAS.Common;
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
        private readonly SemaphoreSlim _requestHandlerSemaphore = new SemaphoreSlim(0);

        private string _response;
        private int _disposed;

        public NevionCommunicator(RouterDevice device)
        {
            _device = device;               
        }

        public async Task<bool> Connect()
        {
            while (_disposed == default(int))
            {
                _tcpClient = new TcpClient();
                var tokensource = new CancellationTokenSource(3000);
                using (tokensource.Token.Register(() => _tcpClient.Close()))
                {
                    Debug.WriteLine("Connecting to Nevion...");
                    try
                    {
                        await _tcpClient.ConnectAsync(_device.IpAddress, _device.Port);
                        if (!_tcpClient.Connected)
                            break;
                        Debug.WriteLine("Nevion connected!");
                        HandleRequests();
                        _requestsQueue = new ConcurrentQueue<string>();
                        _stream = _tcpClient.GetStream();
                        HandleResponses();
                        Logger.Info("Nevion router connected and ready!");
                        Login();
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (ex is ObjectDisposedException || ex is System.IO.IOException)
                            Logger.Debug("Network stream closed");
                        else
                            Logger.Error(ex);
                    }
                }
            }
            return true;
        }

        public void Login()
        {
            AddToRequestQueue($"login {_device.Login} {_device.Password}");            
        }

        public void RequestRouterState()
        {
            AddToRequestQueue($"sspi l{_device.Level}");            
        }

        public void RequestCurrentInputPort()
        {
            AddToRequestQueue($"si l{_device.Level} {string.Join(",", _device.OutputPorts)}");            
        }        

        public void RequestInputPorts()
        {
            AddToRequestQueue($"inlist l{_device.Level}");            
        }

        public void RequestOutputPorts()
        {
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
        
        private async void HandleResponses()
        {
            var bytesReceived = new byte[256];
            Debug.WriteLine("Nevion listener started!");
            while (true)
            {
                try
                {
                    var bytes = await _stream.ReadAsync(bytesReceived, 0, bytesReceived.Length);
                    if (bytes == 0) continue;
                    var response = System.Text.Encoding.ASCII.GetString(bytesReceived, 0, bytes);
                    ParseResponse(response);
                }
                catch (Exception ex)
                {
                    if (ex is ObjectDisposedException || ex is System.IO.IOException)
                        Logger.Debug("Network stream closed");
                    else
                        Logger.Error(ex);
                    return;
                }
            }
        }

        public void Disconnect()
        {
            _tcpClient?.Close();
            _tcpClient = null;
            OnRouterConnectionStateChanged?.Invoke(this, new EventArgs<bool>(false));
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != default(int))
                return;
            Disconnect();
            _requestHandlerSemaphore.Dispose();
            Debug.WriteLine("Nevion communicator disposed");
        }

        public event EventHandler<EventArgs<PortState[]>> OnRouterStateReceived;

        public event EventHandler<EventArgs<PortInfo[]>> OnInputPortsReceived;
        public event EventHandler<EventArgs<bool>> OnRouterConnectionStateChanged;
        public event EventHandler<EventArgs<CrosspointInfo[]>> OnInputPortChangeReceived;


        private void ProcessCommand(string response)
        {
            var lines = response.Split('\n');
            if (lines.Length < 1 || !(lines[0].StartsWith("?") && lines[0].StartsWith("%")))
                return;

            if (lines[0].Contains("inlist"))
                IoListParse(lines
                    .Skip(1)
                    .Where(param => !string.IsNullOrEmpty(param))
                    .ToList(),
                    ListTypeEnum.Input);
            else if (lines[0].Contains("si"))
                IoListParse(lines
                    .Skip(1)
                    .Where(param => !string.IsNullOrEmpty(param))
                    .ToList(),
                    ListTypeEnum.CrosspointStatus);
            else if (lines[0].Contains("sspi"))
                IoListParse(lines
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
                IoListParse(lines
                    .Skip(1)
                    .Where(param => !string.IsNullOrEmpty(param) && param != "%")
                    .ToList(),
                    ListTypeEnum.CrosspointChange);

        }

        private void IoListParse(IList<string> listResponse, ListTypeEnum listType)
        {
            try
            {
                switch (listType)
                {
                    case ListTypeEnum.Input:
                        var ports = listResponse.Select(line =>
                        {
                            var lineParams = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            return lineParams.Length >= 4 ? new PortInfo(short.Parse(lineParams[2].Trim('\"')), lineParams[3].Trim('\"')) : null;
                        }).Where(c => c != null).ToArray();
                        OnInputPortsReceived?.Invoke(this, new EventArgs<PortInfo[]>(ports));
                        break;
                    case ListTypeEnum.SignalPresence:
                        var signals = listResponse.Select(line =>
                        {
                            var lineParams = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            return lineParams.Length >=4 ? new PortState(short.Parse(lineParams[2].Trim('\"')), lineParams[3].Trim('\"') == "p") : null;
                        }).Where(c => c != null).ToArray();
                        OnRouterStateReceived?.Invoke(this, new EventArgs<PortState[]>(signals));
                        break;
                    case ListTypeEnum.CrosspointChange:
                    case ListTypeEnum.CrosspointStatus:
                        var crosspoints = listResponse.Select(line =>
                        {
                            var lineParams = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (lineParams.Length >= 4 && lineParams[0] == "x" &&
                                lineParams[1].StartsWith("l", StringComparison.Ordinal) &&
                                short.TryParse(lineParams[2], out var inPort) &&
                                short.TryParse(lineParams[3], out var outPort))
                                return new CrosspointInfo(inPort, outPort);
                            return null;
                        }).Where(c => c != null).ToArray();
                        OnInputPortChangeReceived?.Invoke(this, new EventArgs<CrosspointInfo[]>(crosspoints));
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to generate port from response");
            }
        }

        private void ParseResponse(string response)
        {
            _response += response;
            while (_response.Contains("\n\n"))
            {
                var command = _response.Substring(0, _response.IndexOf("\n\n", StringComparison.Ordinal) + 2);
                _response = _response.Remove(0, _response.IndexOf("\n\n", StringComparison.Ordinal) + 2);
                Debug.WriteLine(command);
                ProcessCommand(command);
            }
        }

        private async void HandleRequests()
        {
            try
            {
                while (true)
                {
                    await _requestHandlerSemaphore.WaitAsync();
                    while (!_requestsQueue.IsEmpty)
                    {
                        if (!_requestsQueue.TryDequeue(out var request))
                            continue;
                        var data = System.Text.Encoding.ASCII.GetBytes(string.Concat(request, "\n\n"));
                        await _stream.WriteAsync(data, 0, data.Length);
                        Debug.WriteLine($"Nevion message sent: {request}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected exception in Nevion request handler {ex}");
            }
        }
    }
}
