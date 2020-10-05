using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Server.VideoSwitch.Helpers;
using TAS.Server.VideoSwitch.Model;
using TAS.Server.VideoSwitch.Model.Interfaces;

namespace TAS.Server.VideoSwitch.Communicators
{    
    public class AtemCommunicator : IVideoSwitchCommunicator
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();        
        private int _disposed;

        private VideoSwitcher _device;
        private BMDSwitcherWrapper _atem;

        private PortInfo[] _sources;
        public PortInfo[] Sources
        {
            get => _sources;
            set
            {
                if (value == _sources)
                    return;
                _sources = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Sources)));
            }
        }

        public event EventHandler<EventArgs<CrosspointInfo>> SourceChanged;
        public event EventHandler<EventArgs<PortState[]>> ExtendedStatusReceived;
        public event EventHandler<EventArgs<bool>> ConnectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public AtemCommunicator(IRouter device)
        {            
            _device = device as VideoSwitcher;

            //ensure MTA
            Task.Run(() => _atem = new BMDSwitcherWrapper()).Wait();            
            
            _atem.ProgramInputChanged += Switcher_ProgramInputChanged;
            _atem.Disconnected += Atem_Disconnected;
        }

        private void Atem_Disconnected(object sender, EventArgs e)
        {
            ConnectionChanged?.Invoke(this, new EventArgs<bool>(false));
        }

        private void Switcher_ProgramInputChanged(object sender, MixEffectEventArgs e)
        {
            SourceChanged?.Invoke(this, new EventArgs<CrosspointInfo>(new CrosspointInfo((short)e.ProgramInput, -1)));
        }        
                
        public async Task<bool> ConnectAsync()
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

                    bool isConnected = false;
                    await Task.Run(() => isConnected = _atem.Connect(_device.IpAddress, _device.Level));
                    
                    if (!isConnected)
                    {                        
                        Logger.Trace("Could not connect to ATEM. Reconnecting in 3 seconds...");
                        await Task.Delay(3000);
                        continue;
                    }

                    Sources = await Task.Run(() => _atem.GetInputPorts());
                    SetTransitionStyle(_device.DefaultEffect);
                    Logger.Trace("Connected to ATEM TVS");                                                                                    
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
            
            _cancellationTokenSource.Cancel();
        }

        public async Task<CrosspointInfo> GetSelectedSource()
        {
            return await Task.Run(() => new CrosspointInfo((short)_atem.GetCurrentInputPort(), -1)); 
        }       
                
        public async void SetSource(int inPort)
        {
            //ensure MTA
            await Task.Run(() => _atem.SetProgram(inPort)); 
        }

        public async Task Preload(int sourceId)
        {
            //ensure MTA
            await Task.Run(() => _atem.SetPreview(sourceId));
        }

        public void SetTransitionStyle(VideoSwitcherTransitionStyle transitionStyle)
        {
            Task.Run(() => _atem.SetTransition(transitionStyle));
        }

        public void SetMixSpeed(double rate)
        {
            Task.Run(() => _atem.SetMixSpeed(100));
        }

        public async Task Take()
        {
            //ensure MTA
            await Task.Run(() => _atem.Take());
        }        
    }
}
