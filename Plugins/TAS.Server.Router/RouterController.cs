﻿using System;
using System.Collections.Generic;
using TAS.Common;
using TAS.Common.Interfaces;
using System.Linq;
using jNet.RPC.Server;
using TAS.Server.Model;
using TAS.Server.RouterCommunicators;
using jNet.RPC;
using System.Threading.Tasks;

namespace TAS.Server
{

    [DtoType(typeof(IRouter))]
    public class RouterController : ServerObjectBase, IRouter
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IRouterCommunicator _routerCommunicator;
        private readonly RouterDevice _device;
        private IRouterPort _selectedInputPort;
        private bool _isConnected;
        private bool _isDisposed;
        private List<IRouterPort> _inputPorts = new List<IRouterPort>();

        public RouterController(RouterDevice device)
        {
            _device = device;
            switch (_device.Type)
            {
                case RouterTypeEnum.Nevion:
                    _routerCommunicator = new NevionCommunicator(_device);
                    break;
                case RouterTypeEnum.BlackmagicSmartVideoHub:
                    _routerCommunicator = new BlackmagicSmartVideoHubCommunicator(_device);
                    break;
                default:
                    return;
            }

            _routerCommunicator.OnInputPortChangeReceived += Communicator_OnInputPortChangeReceived;
            _routerCommunicator.OnRouterPortsStatesReceived += Communicator_OnRouterPortStateReceived;
            _routerCommunicator.OnRouterConnectionStateChanged += Communicator_OnRouterConnectionStateChanged;
            Init();
        }

        [DtoMember]
        public IRouterPort SelectedInputPort
        {
            get => _selectedInputPort;
            set => SetField(ref _selectedInputPort, value);
        }

        [DtoMember]
        public IRouterPort[] InputPorts => _inputPorts.ToArray();

        [DtoMember]
        public bool IsConnected
        {
            get => _isConnected;
            private set => SetField(ref _isConnected, value);
        }

        public bool SwitchOnPreload => _device.SwitchOnLoad;

        public void SelectInputPort(int inPort, bool instant)
        {
            if (instant || _device.SwitchDelay <= 0)
                _routerCommunicator.SelectInput(inPort);
            else
                Task.Delay(_device.SwitchDelay).ContinueWith(_ => _routerCommunicator.SelectInput(inPort));
        }

        private async void Init()
        {
            if (_routerCommunicator == null)
                return;

            try
            {
                IsConnected = await _routerCommunicator.Connect();
                if (IsConnected)
                    ParseInputMeta(await _routerCommunicator.GetInputPorts());
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private async void ParseInputMeta(PortInfo[] ports)
        {
            if (ports == null)
                return;

            foreach (var port in ports)
            {
                if (_inputPorts.FirstOrDefault(inPort => inPort.PortId == port.Id && inPort.PortName != port.Name) is RouterPort foundPort)
                    foundPort.PortName = port.Name;
                else if (_inputPorts.All(inPort => inPort.PortId != port.Id))
                    _inputPorts.Add(new RouterPort(port.Id, port.Name));
            }
            var selectedInput = await _routerCommunicator.GetCurrentInputPort();

            if (selectedInput == null)
            {
                ParseInputMeta(ports);
                return;
            }

            if (SelectedInputPort == null || SelectedInputPort.PortId != selectedInput.InPort)
                SelectedInputPort = _inputPorts.FirstOrDefault(port => port.PortId == selectedInput.InPort);
        }

        private void Communicator_OnRouterConnectionStateChanged(object sender, EventArgs<bool> e)
        {
            IsConnected = e.Value;
            if (e.Value)
                return;

            Init();
        }

        private void Communicator_OnRouterPortStateReceived(object sender, EventArgs<PortState[]> e)
        {
            foreach (var port in _inputPorts)
                ((RouterPort)port).IsSignalPresent = e.Value?.FirstOrDefault(param => param.PortId == port.PortId)?.IsSignalPresent;
        }

        private void Communicator_OnInputPortChangeReceived(object sender, EventArgs<CrosspointInfo> e)
        {
            if (_device.OutputPorts.Length == 0)
                return;
            var port = _device.OutputPorts[0];
            var changedIn = e.Value.OutPort == port ? e.Value : null;
            if (changedIn == null)
                return;
            SelectedInputPort = _inputPorts.FirstOrDefault(param => param.PortId == changedIn.InPort);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;
            _routerCommunicator.OnInputPortChangeReceived -= Communicator_OnInputPortChangeReceived;
            _routerCommunicator.OnRouterPortsStatesReceived -= Communicator_OnRouterPortStateReceived;
            _routerCommunicator.OnRouterConnectionStateChanged -= Communicator_OnRouterConnectionStateChanged;
            _routerCommunicator.Dispose();
        }
    }
}
