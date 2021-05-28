using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using TAS.Common;
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
        private VideoSwitcherTransitionStyle _videoSwitcherTransitionStyle;

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

        public int Level { get; set; }

        public event EventHandler<EventArgs<CrosspointInfo>> SourceChanged;
        public event EventHandler<EventArgs<bool>> ConnectionStatusChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public AtemCommunicator()
        {            
            //ensure MTA
            Task.Run(() => _atem = new BMDSwitcherWrapper()).Wait();            
            
            _atem.ProgramInputChanged += Switcher_ProgramInputChanged;
            _atem.Disconnected += Atem_Disconnected;
        }

        private void Atem_Disconnected(object sender, EventArgs e)
        {
            ConnectionStatusChanged?.Invoke(this, new EventArgs<bool>(false));
        }

        private void Switcher_ProgramInputChanged(object sender, MixEffectEventArgs e)
        {
            SourceChanged?.Invoke(this, new EventArgs<CrosspointInfo>(new CrosspointInfo((short)e.ProgramInput, -1)));
        }        

        public bool IsConnected { get; set; }
                
        public async void Connect(string address, CancellationToken cancellationToken)
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

                    await Task.Run(() => _atem.Connect(address, Level), cancellationToken);
                    
                    if (IsConnected)
                    {                        
                        Logger.Trace("Could not connect to ATEM. Reconnecting in 3 seconds...");
                        Thread.Sleep(3000);
                        continue;
                    }

                    Sources = _atem.GetInputPorts();
                    SetTransitionStyle(_videoSwitcherTransitionStyle);
                    Logger.Trace("Connected to ATEM TVS");                                                                                    
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
                        Thread.Sleep(3000);
                        continue;
                    }
                }
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != default(int))
                return;
            
            _cancellationTokenSource.Cancel();
        }

        public CrosspointInfo GetSelectedSource()
        {
            return new CrosspointInfo((short)_atem.GetCurrentInputPort(), -1); 
        }       
                
        public void SetSource(int inPort)
        {
            _atem.SetProgram(inPort); 
        }

        public void Preload(int sourceId)
        {
            _atem.SetPreview(sourceId);
        }

        public void SetTransitionStyle(VideoSwitcherTransitionStyle transitionStyle)
        {
            _atem.SetTransition(transitionStyle);
            _videoSwitcherTransitionStyle = transitionStyle;
        }

        public void SetMixSpeed(byte rate)
        {
            _atem.SetMixSpeed(rate);
        }

        public void Take()
        {
            _atem.Take();
        }

        public void Disconnect()
        {
            _atem.Disconnect();
        }
    }
}
