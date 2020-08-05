using BMDSwitcherAPI;
using System;
using System.Collections.Generic;
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
        IBMDSwitcherDiscovery _discovery;
        private int _disposed;

        private SemaphoreSlim _inputPortsSemaphore = new SemaphoreSlim(0);
        private SemaphoreSlim _selectedInputPortSemaphore = new SemaphoreSlim(0);

        private List<PortInfo> _inputPorts = new List<PortInfo>();
        private VideoSwitch _device;        

        public event EventHandler<EventArgs<CrosspointInfo>> OnInputPortChangeReceived;
        public event EventHandler<EventArgs<PortState[]>> OnRouterPortsStatesReceived;
        public event EventHandler<EventArgs<bool>> OnRouterConnectionStateChanged;

        public AtemCommunicator(VideoSwitch device)
        {            
            _device = device;            
        }       

        private IBMDSwitcherMixEffectBlock GetMixEffectBlock(int index)
        {
            IntPtr meIteratorPtr;
            _switcher.CreateIterator(typeof(IBMDSwitcherMixEffectBlockIterator).GUID, out meIteratorPtr);
            IBMDSwitcherMixEffectBlockIterator meIterator = Marshal.GetObjectForIUnknown(meIteratorPtr) as IBMDSwitcherMixEffectBlockIterator;
            if (meIterator == null)
                return null;

            int i = 0;
            while (true)
            {                
                meIterator.Next(out var me);

                if (me != null)
                {
                    if (i == index)
                        return me;
                    ++i;
                }
                else
                {
                    return null;
                }                    
            }            
        }
        
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

                    _discovery = new CBMDSwitcherDiscovery();
                    _BMDSwitcherConnectToFailure failureReason;

                    _discovery.ConnectTo("192.168.1.241", out _switcher, out failureReason);
                    if (_switcher == null)
                    {
                        Logger.Trace("Could not connect to ATEM. Reconnecting in 3 seconds...");
                        await Task.Delay(3000);
                        continue;
                    }                    

                    Logger.Trace("Connected to ATEM TVS");
                    _me = GetMixEffectBlock(0);                    
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
                    input.GetShortName(out var shortName);
                    input.GetLongName(out var longName);
                    _inputPorts.Add(new PortInfo((short)id, String.Concat(longName, '(', shortName, ')')));                    
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
