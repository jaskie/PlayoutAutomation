using BMDSwitcherAPI;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TAS.Server.VideoSwitch.Model;

namespace TAS.Server.VideoSwitch.Helpers
{
    public class BMDSwitcherWrapper
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private IBMDSwitcher _switcher;
        private IBMDSwitcherMixEffectBlock _me;
        private IBMDSwitcherDiscovery _discovery;
        private _BMDSwitcherConnectToFailure failureReason;
        
        private AtemMonitor _atemMonitor = new AtemMonitor();       
        public event EventHandler<MixEffectEventArgs> ProgramInputChanged;
        public event EventHandler Disconnected;

        public BMDSwitcherWrapper()
        {                                    
            _discovery = new CBMDSwitcherDiscovery();          
            _atemMonitor.ProgramInputChanged += AtemMonitor_ProgramInputChanged;
            _atemMonitor.ConnectionChanged += AtemMonitor_ConnectionChanged;
        }

        private void AtemMonitor_ConnectionChanged(object sender, EventArgs e)
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
            _switcher = null;
            _me = null;
        }

        private void AtemMonitor_ProgramInputChanged(object sender, EventArgs e)
        {                     
            ProgramInputChanged?.Invoke(this, new MixEffectEventArgs((int)GetCurrentInputPort()));
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

        public bool Connect(string ipAddress, int level)
        {
            try
            {
                _discovery.ConnectTo(ipAddress, out _switcher, out failureReason);

                if (_switcher == null)
                    return false;

                _switcher.AddCallback(_atemMonitor);

                //ensure MTA
                Task.Run(() =>
                {
                    _me = GetMixEffectBlock(level);
                    _me?.AddCallback(_atemMonitor);
                }).Wait();               
                
                if (_me == null)
                {
                    Logger.Trace("Could not get MixEffectBlock. Check level settings");
                    return false;
                }
            }
            catch(Exception ex)
            {                
                switch(failureReason)
                {
                    case _BMDSwitcherConnectToFailure.bmdSwitcherConnectToFailureNoResponse:
                        Logger.Trace("No response from switcher");
                        break;

                    case _BMDSwitcherConnectToFailure.bmdSwitcherConnectToFailureIncompatibleFirmware:
                        Logger.Warn("Incompatible firmware version");
                        break;
                }
            }

            if (_switcher != null && _me != null)
                return true;
            return false;
        }

        public int GetCurrentInputPort()
        {
            long inPort = -1;           
            _me.GetProgramInput(out inPort);           

            return (int)inPort;
        }
        public void SelectInput(int inPort)
        {                        
            _me.SetProgramInput(inPort);         
        }
        public PortInfo[] GetInputPorts()
        {
            var inputPorts = new List<PortInfo>();

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
                    inputPorts.Add(new PortInfo((short)id, String.Concat(longName, '(', shortName, ')')));
                }

                // Get next input
                inputIterator.Next(out input);
            }

            return inputPorts.ToArray();
        }       
    }
}
