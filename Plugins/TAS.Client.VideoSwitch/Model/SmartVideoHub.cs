using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Server.VideoSwitch.Communicators;
using TAS.Server.VideoSwitch.Helpers;

namespace TAS.Server.VideoSwitch.Model
{
    public class SmartVideoHub: RouterBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly MessageRequest<object> _connectionWatcherRequest = new MessageRequest<object>();
        private PortInfo[] _outputs;
        private CrosspointInfo[] _routing;
        private string _responseBuffer;

        public SmartVideoHub() : base(9990)
        {
        }

        protected override void ConnectionWatcherProc()
        {
            Thread.Sleep(5000);
            while (!CancellationToken.IsCancellationRequested)
            {
                try
                {
                    SendString("PING:\n");
                    _connectionWatcherRequest.WaitForResult(CancellationToken, 5000);
                    Thread.Sleep(5000);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    Disconnect();
                }
            }
        }

        protected override CrosspointInfo GetSelectedSource()
        {
            throw new NotImplementedException();
        }

        protected override void OnMessageReceived(byte[] message)
        {
            _responseBuffer += System.Text.Encoding.ASCII.GetString(message);
            int i;
            while ((i = _responseBuffer.IndexOf("\n\n")) >= 0)
            {
                var command = _responseBuffer.Substring(0, i + 2);
                _responseBuffer = _responseBuffer.Remove(0, i + 2);
                ProcessSection(command);
            }
        }

        private void SendString(string message)
        {
            Logger.Trace("Sending: {0}", message);
            Send(System.Text.Encoding.ASCII.GetBytes(message + "\n"));
        }


        private void ProcessSection(string section)
        {
            Logger.Trace("Received: {0}", section);

            if (section.Length == 0 || section.StartsWith("NAK"))
            {
                Logger.Warn("Response not recognized: {0}", section);
                return;
            }

            if (section.StartsWith("ACK"))
            {
                var lines = section.Split('\n');
                if (lines.Length > 2 && lines[2] == "")
                    _connectionWatcherRequest.SetResult(lines);
                return;
            }

            var separatorPos = section.IndexOf(':');
            if (separatorPos < 0)
            {
                Logger.Warn("Response not recognized: {0}", section);
                return;
            }
            var sectionName = section.Substring(0, separatorPos);
            switch (sectionName)
            {
                case "VIDEOHUB DEVICE":
                    ProcessDeviceInformation(section);
                    break;
                case "INPUT LABELS":
                    ProcessInputLabels(section);
                    break;
                case "OUTPUT LABELS":
                    ProcessOutputLabels(section);
                    break;
                case "VIDEO OUTPUT ROUTING":
                    ProcessOutputRouting(section);
                    break;
            }
        }

        private void ProcessOutputLabels(string section)
        {
            var lines = section.Split('\n');
            if (lines.Length < 2)
                return;
            foreach (var line in lines)
            {
                if (line == "OUTPUT LABELS:")
                    continue;
                var parts = line.Split(new char[] { ' ' }, 2);
                if (parts.Length < 2)
                    continue;
                if (!short.TryParse(parts[0], out var id))
                    continue;
                var output = AllOutputs.FirstOrDefault(p => p.Id == id);
                if (output is null)
                    continue;
                output.Name = parts[1];
            }
        }

        private void ProcessOutputRouting(string section)
        {
            var lines = section.Split('\n');
            if (lines.Length < 2)
                return;
            var routing = new List<CrosspointInfo>();
            foreach (var line in lines)
            {
                if (line == "VIDEO OUTPUT ROUTING:")
                    continue;
                var parts = line.Split(' ');
                if (parts.Length != 2)
                    continue;
                if (!(short.TryParse(parts[0], out var outPort) && short.TryParse(parts[1], out var inPort)))
                    continue;
                routing.Add(new CrosspointInfo(inPort, outPort));
            }
            _routing = routing.ToArray();
        }

        private void ProcessInputLabels(string section)
        {
            var lines = section.Split('\n');
            if (lines.Length < 2)
                return;
            foreach (var line in lines)
            {
                if (line == "INPUT LABELS:")
                    continue;
                var parts = line.Split(new char[] { ' ' }, 2);
                if (parts.Length < 2)
                    continue;
                if (!short.TryParse(parts[0], out var id))
                    continue;
                var input = Inputs.FirstOrDefault(p => p.Id == id);
                if (input is null)
                    continue;
                input.Name = parts[1];
            }
        }

        private void ProcessDeviceInformation(string response)
        {
            var lines = response.Split('\n');
            foreach (var line in lines)
            {
                var separatorPos = line.IndexOf(':');
                if (separatorPos < 0)
                    continue;
                var parameterName = line.Substring(0, separatorPos);
                switch (parameterName)
                {
                    case "Video inputs":
                        if (int.TryParse(line.Substring(separatorPos + 1), out var count))
                        {
                            short index = 0;
                            Inputs = Enumerable.Range(0, count).Select(i => new PortInfo(index++, $"Input {index}")).ToArray();
                        }
                        break;
                    case "Video outputs":
                        if (int.TryParse(line.Substring(separatorPos + 1), out count))
                        {
                            short index = 0;
                            AllOutputs = Enumerable.Range(0, count).Select(i =>
                            {
                                var port = new PortInfo(index++, $"Output {index}");
                                port.PropertyChanged += Port_PropertyChanged;
                                return port;
                            }).ToArray();
                        }
                        break;
                }
            }
        }

        private void Port_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            
        }
    }
}
