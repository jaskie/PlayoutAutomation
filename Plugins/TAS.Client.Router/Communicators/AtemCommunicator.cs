using BMDSwitcherAPI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TAS.Common;
using TAS.Server.VideoSwitch.Model;

namespace TAS.Server.VideoSwitch.Communicators
{
    public class AtemCommunicator : IVideoSwitchCommunicator
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private IBMDSwitcherMixEffectBlock _me;
        private IBMDSwitcher _switcher;
        private int _disposed;

        private SemaphoreSlim _inputPortsSemaphore = new SemaphoreSlim(0);
        private SemaphoreSlim _selectedInputPortSemaphore = new SemaphoreSlim(0);

        private List<PortInfo> _inputPorts = new List<PortInfo>();        

        public AtemCommunicator(string ipAddress)
        {
            try
            {                
                Assembly api = Assembly.Load("Plugins/BMDSwitcherAPI64.dll");
                RegistrationServices rs = new RegistrationServices();
                rs.RegisterAssembly(api, AssemblyRegistrationFlags.None);
            }
            catch(Exception ex)
            {
                Logger.Error("Failed to register BMDSwitcherAPI64.dll: {0}", ex.Message);
            }
            
            
            _ = Connect();
        }

        //private void OnAtemReceive(object sender, IReadOnlyList<ICommand> commands)
        //{
        //    foreach(var command in commands)
        //    {
        //        if (command is InputPropertiesGetCommand input)
        //        {
        //            if (_inputPorts == null)
        //                _inputPorts = new List<PortInfo>();

        //            _inputPorts.Add(new PortInfo((short)(input.Id), input.ShortName));
        //        }                
        //    }
            
        //    if (_inputPortsSemaphore.CurrentCount == 0)
        //        _inputPortsSemaphore.Release();
        //}

        //private void OnAtemDisconnected(object sender)
        //{
        //    Logger.Trace("ATEM disconnected");

        //    if (_disposed == 1)
        //        return;

        //    _ = Connect();
        //}

        //private void OnAtemConnection(object sender)
        //{
            
        //}

        public event EventHandler<EventArgs<CrosspointInfo>> OnInputPortChangeReceived;
        public event EventHandler<EventArgs<PortState[]>> OnRouterPortsStatesReceived;
        public event EventHandler<EventArgs<bool>> OnRouterConnectionStateChanged;        

        public async Task<bool> Connect()
        {
            _disposed = default(int);
            _cancellationTokenSource = new CancellationTokenSource();

            while (true)
            {                
                Logger.Debug("Setting up ATEM TVS...");
                try
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                        throw new OperationCanceledException(_cancellationTokenSource.Token);

                    IBMDSwitcherDiscovery discovery = new CBMDSwitcherDiscovery();
                    _BMDSwitcherConnectToFailure failureReason;

                    discovery.ConnectTo("192.168.1.241", out _switcher, out failureReason);
                    if (_switcher != null)
                    {
                        Logger.Trace("Connected to ATEM TVS");
                    }
                    else
                    {
                        Logger.Trace("Could not connect to ATEM. Reconnecting in 3 seconds...");
                        await Task.Delay(3000);
                        continue;
                    }                                                                                                                                           

                    return true;
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException)
                    {
                        Logger.Trace("ATEM connecting canceled");
                        break;
                    }
                    else
                    {
                        Logger.Error(ex);
                        await Task.Delay(3000);
                        continue;
                    }
                }
            }
            return false;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != default(int))
                return;
            
            if (_me == null)
                return;

            
        }

        public async Task<CrosspointInfo> GetCurrentInputPort()
        {
            _me.GetProgramInput(out var inPort);            
            return new CrosspointInfo((short)inPort, -1);
        }

        public async Task<PortInfo[]> GetInputPorts()
        {
            _inputPorts = new List<PortInfo>();

            IntPtr inputIteratorPtr;
            _switcher.CreateIterator(typeof(IBMDSwitcherInputIterator).GUID, out inputIteratorPtr);
            IBMDSwitcherInputIterator inputIterator = Marshal.GetObjectForIUnknown(inputIteratorPtr) as IBMDSwitcherInputIterator;
            if (inputIterator == null)
                return null;
            
            IBMDSwitcherInput input;
            inputIterator.Next(out input);
            while (input != null)
            {
                _BMDSwitcherPortType currentType;
                input.GetPortType(out currentType);

                if (currentType == _BMDSwitcherPortType.bmdSwitcherPortTypeExternal)
                {
                    input.GetInputId(out var id);
                    input.GetShortName(out var name);
                    _inputPorts.Add(new PortInfo((short)id, name));                    
                }                                    

                // Get next input
                inputIterator.Next(out input);
            }

            return _inputPorts.ToArray();
        }

        public void SelectInput(int inPort)
        {
            _me.SetProgramInput(inPort);
        }        
    }
}
