using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TAS.Common;
using TAS.Server.VideoSwitch.Helpers;
using TAS.Server.VideoSwitch.Model;
using TAS.Server.VideoSwitch.Model.Interfaces;

namespace TAS.Server.VideoSwitch.Communicators
{
    internal class SmartVideoHubCommunicator : SocketConnection, IRouterCommunicator
    {
        /// <summary>
        /// In Blackmagic CrosspointStatus and CrosspointChange responses have the same syntax. CrosspointStatus semaphore initial value is set to 1 to help notify ProcessCommand method
        /// about pending request from GetCurrentInputPort method
        /// </summary>
        /// 
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private ConcurrentQueue<string> _requestsQueue = new ConcurrentQueue<string>();
        private ConcurrentQueue<KeyValuePair<ListTypeEnum, string[]>> _responsesQueue = new ConcurrentQueue<KeyValuePair<ListTypeEnum, string[]>>();

        private readonly ConcurrentDictionary<ListTypeEnum, string[]> _responseDictionary = new ConcurrentDictionary<ListTypeEnum, string[]>();

        private readonly Dictionary<ListTypeEnum, SemaphoreSlim> _semaphores = Enum.GetValues(typeof(ListTypeEnum)).Cast<ListTypeEnum>().ToDictionary(t => t, t => new SemaphoreSlim(t == ListTypeEnum.CrosspointStatus ? 1 : 0));

        private readonly SemaphoreSlim _requestQueueSemaphore = new SemaphoreSlim(0);
        private readonly SemaphoreSlim _responsesQueueSemaphore = new SemaphoreSlim(0);

        private PortInfo[] _sources;
        private CancellationToken _cancellationToken;

        public SmartVideoHubCommunicator() : base(9990)
        { }

        private PortInfo[] GetSources()
        {
            if (!_semaphores.TryGetValue(ListTypeEnum.Input, out var semaphore))
                return null;

            SendString("INPUT LABELS:");
            while (true)
            {
                try
                {
                    if (_cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(_cancellationToken);

                    if (!_responseDictionary.TryRemove(ListTypeEnum.Input, out var response))
                        semaphore.Wait(_cancellationToken);

                    if (response == null && !_responseDictionary.TryRemove(ListTypeEnum.Input, out response))
                        continue;

                    semaphore.Release(); // reset semaphore to 1

                    return response.Select(line =>
                    {
                        var lineParams = line.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        return lineParams.Length >= 2
                            ? new PortInfo(short.Parse(lineParams[0]), lineParams.ElementAtOrDefault(1) ?? string.Empty)
                            : null;
                    }).Where(c => c != null).ToArray();
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException)
                        Logger.Debug("Input ports request cancelled");

                    return null;
                }
            }
        }

        private void SendString(string s)
        {
            var data = System.Text.Encoding.ASCII.GetBytes(s);
            Send(data);
        }

        private async void ConnectionWatcher()
        {
            if (!_semaphores.TryGetValue(ListTypeEnum.SignalPresence, out var semaphore))
                return;

            AddToRequestQueue("PING:");

            while (true)
            {
                try
                {
                    if (_cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(_cancellationToken);

                    await semaphore.WaitAsync(_cancellationToken).ConfigureAwait(false);
                    await Task.Delay(3000, _cancellationToken).ConfigureAwait(false);
                    AddToRequestQueue("PING:");
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException)
                        Logger.Debug("Router Ping cancelled");

                    return;
                }
            }
        }

        private async void InputPortWatcher()
        {
            if (!_semaphores.TryGetValue(ListTypeEnum.CrosspointChange, out var semaphore))
                return;

            while (true)
            {
                try
                {
                    if (_cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(_cancellationToken);

                    if (!_responseDictionary.TryRemove(ListTypeEnum.CrosspointChange, out var response))
                        await semaphore.WaitAsync(_cancellationToken).ConfigureAwait(false);

                    if (response == null && !_responseDictionary.TryRemove(ListTypeEnum.CrosspointChange, out response))
                        continue;

                    var crosspoints = response.Select(line =>
                    {
                        var lineParams = line.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        if (lineParams.Length >= 2 &&
                            short.TryParse(lineParams[0], out var outPort) &&
                            //TODO:fixme
                            //outPort == _router.OutputPorts[0] &&
                            short.TryParse(lineParams[1], out var inPort))
                            return new CrosspointInfo(inPort, outPort);

                        return null;
                    }).FirstOrDefault(c => c != null);

                    if (crosspoints == null)
                        continue;

                    SourceChanged?.Invoke(this, new EventArgs<CrosspointInfo>(crosspoints));
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException)
                        Logger.Debug("Input Port Watcher cancelled");

                    return;
                }
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
            if (lines.Length < 1 || lines[0].StartsWith("NAK"))
                return;

            if (lines.Length > 2 && lines[0].StartsWith("ACK") && lines[2] == "")
            {
                if (!_semaphores.TryGetValue(ListTypeEnum.SignalPresence, out var semaphore))
                    return;

                semaphore.Release();
            }

            var trimmedLines = lines.Skip(1).Where(param => !string.IsNullOrEmpty(param)).ToArray();

            if (lines[0].Contains("INPUT LABELS"))
            {
                if (!_semaphores.TryGetValue(ListTypeEnum.Input, out var semaphore))
                    return;

                if (!_responseDictionary.TryAdd(ListTypeEnum.Input, trimmedLines))
                    AddToResponseQueue(ListTypeEnum.Input, trimmedLines);
                else
                    semaphore.Release();
            }

            else if (lines[0].Contains("OUTPUT ROUTING"))
            {
                if (_semaphores.TryGetValue(ListTypeEnum.CrosspointStatus, out var semaphoreStatus) &&
                    semaphoreStatus.CurrentCount == 0 &&
                    !_responseDictionary.TryGetValue(ListTypeEnum.CrosspointStatus, out _))
                {
                    _responseDictionary.TryAdd(ListTypeEnum.CrosspointStatus, trimmedLines);

                    semaphoreStatus.Release();
                    return;
                }


                if (!_semaphores.TryGetValue(ListTypeEnum.CrosspointChange, out var semaphoreChange))
                    return;

                if (!_responseDictionary.TryAdd(ListTypeEnum.CrosspointChange, trimmedLines))
                    AddToResponseQueue(ListTypeEnum.CrosspointChange, trimmedLines);
                else
                    semaphoreChange.Release();
            }
        }

        private void ParseMessage(string response)
        {
            while (response.Contains("\n\n"))
            {
                var command = response.Substring(0, response.IndexOf("\n\n", StringComparison.Ordinal) + 2);
                response = response.Remove(0, response.IndexOf("\n\n", StringComparison.Ordinal) + 2);
                ProcessCommand(command);
            }
        }

        public event EventHandler<EventArgs<bool>> ConnectionStatusChanged;
        public event EventHandler<EventArgs<CrosspointInfo>> SourceChanged;

        public PortInfo[] Sources
        {
            get => _sources;
            set => SetField(ref _sources, value);
        }

        public CrosspointInfo GetSelectedSource()
        {
            if (!_semaphores.TryGetValue(ListTypeEnum.CrosspointStatus, out var semaphore))
                return null;

            AddToRequestQueue("VIDEO OUTPUT ROUTING:");
            while (true)
            {
                try
                {
                    if (_cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(_cancellationToken);

                    if (!_responseDictionary.TryRemove(ListTypeEnum.CrosspointStatus, out var response))
                        semaphore.Wait(_cancellationToken);

                    if (response == null && !_responseDictionary.TryRemove(ListTypeEnum.CrosspointStatus, out response))
                        continue;

                    semaphore.Release(); // reset semaphore to 1
                    return response.Select(line =>
                    {
                        var lineParams = line.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        if (lineParams.Length >= 2 &&
                            short.TryParse(lineParams[0], out var outPort) &&
                            //TODO: fix
                            //                          outPort == _router.OutputPorts[0] &&
                            short.TryParse(lineParams[1], out var inPort))
                            return new CrosspointInfo(inPort, outPort);

                        return null;
                    }).FirstOrDefault(c => c != null);
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException)
                        Logger.Debug("Current Input Port request cancelled");

                    return null;
                }
            }
        }

        public override void Connect(string address, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;

            base.Connect(address, cancellationToken);
            _requestsQueue = new ConcurrentQueue<string>();
            _responsesQueue = new ConcurrentQueue<KeyValuePair<ListTypeEnum, string[]>>();
            ConnectionWatcher();
            InputPortWatcher();

            Sources = GetSources();
        }
    


        public void SetSource(int inPort)
        {
            //TODO: fix
            //AddToRequestQueue(_router.OutputPorts.Aggregate("VIDEO OUTPUT ROUTING:\n", (current, outPort) => current + string.Concat(outPort, " ", inPort, "\n")));
        }

        protected override void OnMessageReceived(byte[] message)
        {
            throw new NotImplementedException();
        }
    }
}