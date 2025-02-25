using jNet.RPC;
using jNet.RPC.Server;
using LibAtem.Common;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    [DtoType(typeof(IRouter))]
    public class AtemController : ServerObjectBase, IRouter, IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly LibAtem.Net.AtemClient _atemClient;
        private bool _isConnected;
        private readonly MixEffectBlockId _mixEffectBlockIndex;
        private readonly object _inputPortsLock = new object();
        private readonly List<AtemInputPort> _inputPorts = new List<AtemInputPort>();
        private IRouterPort _selectedRouterPort;

        public AtemController(AtemDevice atemDevice)
        {
            _atemClient = new LibAtem.Net.AtemClient(atemDevice.Address, false);
            _mixEffectBlockIndex = (MixEffectBlockId)atemDevice.MixEffectBlockIndex - 1;
            _atemClient.OnConnection += OnConnection;
            _atemClient.OnDisconnect += OnDisconnect;
            _atemClient.OnReceive += OnReceive;
            _atemClient.Connect();
        }

        [DtoMember]
        public IList<IRouterPort> InputPorts { get { lock (_inputPortsLock) return _inputPorts.ToArray(); } }

        [DtoMember]
        public IRouterPort SelectedInputPort { get { return _selectedRouterPort; } private set { SetField(ref _selectedRouterPort, value); } }

        [DtoMember]
        public bool IsConnected
        {
            get => _isConnected;
            private set => SetField(ref _isConnected, value);
        }

        public void SelectInput(int inputId)
        {
            var command = new LibAtem.Commands.MixEffects.ProgramInputSetCommand { Index = _mixEffectBlockIndex, Source = (VideoSource)inputId };
            _atemClient.SendCommand(command);
        }

        private void OnConnection(object sender)
        {
            IsConnected = true;
            Logger.Info("Connected to ATEM");
        }

        private void OnDisconnect(object sender)
        {
            IsConnected = false;
            lock(_inputPortsLock)
                _inputPorts.Clear();
            NotifyPropertyChanged(nameof(InputPorts));
            Logger.Info("Disconnected from ATEM");
        }

        private void OnReceive(object sender, IReadOnlyList<LibAtem.Commands.ICommand> commands)
        {
            var portInfoCommands = commands.OfType<LibAtem.Commands.Settings.InputPropertiesGetCommand>();
            if (portInfoCommands.Any())
            {
                lock (_inputPortsLock)
                {
                    foreach (var command in portInfoCommands)
                    {
                        var existingPort = _inputPorts.FirstOrDefault(port => port.VideoSource == command.Id);
                        if (existingPort != null)
                            existingPort.UpdateName(command);
                        else
                            _inputPorts.Add(new AtemInputPort(command));
                    }
                }
                Logger.Info("Received {0} input ports", portInfoCommands.Count());
                NotifyPropertyChanged(nameof(InputPorts));
            }
            var inputGetCommandOnSelectedMe = commands.OfType<LibAtem.Commands.MixEffects.ProgramInputGetCommand>().FirstOrDefault(x => x.Index == _mixEffectBlockIndex);
            if (inputGetCommandOnSelectedMe != null)
                SelectedInputPort = _inputPorts.FirstOrDefault(port => port.VideoSource == inputGetCommandOnSelectedMe.Source);
        }

        public void Dispose()
        {
            _atemClient.OnConnection -= OnConnection;
            _atemClient.OnDisconnect -= OnDisconnect;
            _atemClient.OnReceive -= OnReceive;
            _atemClient.Dispose();
        }
    }
}
