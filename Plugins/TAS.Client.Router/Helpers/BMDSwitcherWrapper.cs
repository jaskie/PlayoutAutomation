using BMDSwitcherAPI;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using TAS.Common;
using TAS.Server.VideoSwitch.Model;

namespace TAS.Server.VideoSwitch.Helpers
{
    public class BMDSwitcherWrapper
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private IBMDSwitcher _switcher;
        private IBMDSwitcherMixEffectBlock _me;
        private IBMDSwitcherTransitionMixParameters _mixParams;
        private IBMDSwitcherTransitionParameters _transitionParams;
        private IBMDSwitcherDiscovery _discovery;
        private _BMDSwitcherConnectToFailure failureReason;

        private bool _takeExecuting;

        private uint _transitionRate;
        private uint _framesRemaining;

        private readonly object _syncObject = new object();
        
        private AtemMonitor _atemMonitor = new AtemMonitor();       
        public event EventHandler<MixEffectEventArgs> ProgramInputChanged;
        public event EventHandler Disconnected;

        private SemaphoreSlim _waitForTransitionEndSemaphore = new SemaphoreSlim(1);

        public BMDSwitcherWrapper()
        {                                    
            _discovery = new CBMDSwitcherDiscovery();
            _atemMonitor.TransitionFramesRemainingChanged += AtemMonitor_TransitionFramesRemainingChanged;
            _atemMonitor.ProgramInputChanged += AtemMonitor_ProgramInputChanged;
            _atemMonitor.ConnectionChanged += AtemMonitor_ConnectionChanged;            
        }

        private void AtemMonitor_TransitionFramesRemainingChanged(object sender, EventArgs e)
        {
            _me?.GetTransitionFramesRemaining(out _framesRemaining);
            Logger.Trace("Transition frames left: {0}", _framesRemaining);

            if (_framesRemaining < _transitionRate)
                return;
            
            lock(_syncObject)
            {
                Logger.Trace("Take finished");
                _takeExecuting = false;                
                _waitForTransitionEndSemaphore.Release();
            }            
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
                //Task.Run(() =>
                //{
                _me = GetMixEffectBlock(level);
                if (_me == null)
                {
                    Logger.Trace("Could not get MixEffectBlock. Check level settings");
                    return false;
                }

                _me.AddCallback(_atemMonitor);                
                //_mixParams = GetBlockMixEffectParameters(_me);
                _transitionParams = QueryInterfaceWrapper.GetObject<IBMDSwitcherTransitionParameters>(_me);
                
                _mixParams = QueryInterfaceWrapper.GetObject<IBMDSwitcherTransitionMixParameters>(_me);
                _mixParams.GetRate(out _transitionRate);
                _me.GetTransitionFramesRemaining(out _framesRemaining);
                Logger.Trace("Transition speed: {0}", _transitionRate);
                //}).Wait();                                               
            }
            catch
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
            long source = -1;           
            _me.GetProgramInput(out source);           

            return (int)source;
        }
        public void SetProgram(int source)
        {
            while(_takeExecuting)
            {
                Logger.Trace("Waiting Program");
                _waitForTransitionEndSemaphore.Wait();
            }

            Logger.Trace("Setting program {0}", source);
            _me.SetProgramInput(source);

            if (_waitForTransitionEndSemaphore.CurrentCount == 0)
                _waitForTransitionEndSemaphore.Release();
        }
        public void SetPreview(int source)
        {
            while(_takeExecuting)
            {
                Logger.Trace("Waiting Preview");
                _waitForTransitionEndSemaphore.Wait();
            }

            Logger.Trace("Setting preview {0}", source);
            _me.SetPreviewInput(source);

            if (_waitForTransitionEndSemaphore.CurrentCount == 0)
                _waitForTransitionEndSemaphore.Release();
        }
        public void Take()
        {
            lock(_syncObject)
                _takeExecuting = true;

            _me.PerformAutoTransition();
        }
        public void SetMixSpeed(uint mixSpeed)
        {
            _mixParams.SetRate(mixSpeed);   
        }

        public void SetTransition(VideoSwitcherTransitionStyle videoSwitchEffect)
        {
            switch(videoSwitchEffect)
            {
                case VideoSwitcherTransitionStyle.Mix:
                    _transitionParams?.SetNextTransitionStyle(_BMDSwitcherTransitionStyle.bmdSwitcherTransitionStyleMix);
                    break;

                case VideoSwitcherTransitionStyle.Dip:
                    _transitionParams?.SetNextTransitionStyle(_BMDSwitcherTransitionStyle.bmdSwitcherTransitionStyleDip);
                    break;

                case VideoSwitcherTransitionStyle.Wipe:
                    _transitionParams?.SetNextTransitionStyle(_BMDSwitcherTransitionStyle.bmdSwitcherTransitionStyleWipe);
                    break;                
            }
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
