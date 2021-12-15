using jNet.RPC;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Database.Common;

namespace TAS.Server.VideoSwitch.Model
{
    public abstract class VideoSwitchBase : Helpers.SocketConnection, IVideoSwitch
    {
        #region Configuration        
        [Hibernate]
        public bool IsEnabled { get; set; }
        [Hibernate]
        public string IpAddress { get; set; }
        [Hibernate]
        public string Login { get; set; }
        [Hibernate]
        public string Password { get; set; }
        [Hibernate]
        public bool Preload { get; set; }
        [DtoMember]
        public IVideoSwitchPort[] Inputs { get => _inputs; set => SetField(ref _inputs, value); }
        #endregion

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private IVideoSwitchPort _selectedInputPort;
        protected CancellationToken CancellationToken;
        private Thread _connectionWatcherThread;
        private IVideoSwitchPort[] _inputs;

        protected VideoSwitchBase(int defaultPort) : base(defaultPort)
        { }

        protected abstract CrosspointInfo GetSelectedSource();

        [DtoMember]
        public IVideoSwitchPort SelectedSource
        {
            get => _selectedInputPort;
            protected set
            {
                if (value == _selectedInputPort)
                    return;
                _selectedInputPort = value;
                NotifyPropertyChanged();
            }
        }

        public event EventHandler Started;

        public async void Connect(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
            try
            {
                await base.Connect(IpAddress, cancellationToken);
                if (!IsConnected)
                    return;
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            _connectionWatcherThread = new Thread(ConnectionWatcherProc)
            {
                Name = $"Connection watcher for {IpAddress}",
                IsBackground = true
            };
            _connectionWatcherThread.Start();
        }

        protected abstract void ConnectionWatcherProc();

        protected void RaiseGpiStarted()
        {
            Started?.Invoke(this, EventArgs.Empty);
        }

        public virtual void SetSource(int sourceId)
        {
        }
    }
}
