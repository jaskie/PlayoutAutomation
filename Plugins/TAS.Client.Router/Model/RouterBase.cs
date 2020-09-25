using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Database.Common;
using TAS.Server.VideoSwitch.Communicators;
using TAS.Server.VideoSwitch.Model.Interfaces;

namespace TAS.Server.VideoSwitch.Model
{
    internal abstract class RouterBase : IRouter
    {
        #region Configuration        
        [Hibernate]
        public bool IsEnabled { get; set; }
        [Hibernate]
        public string IpAddress { get; set; }
        [Hibernate]
        public CommunicatorType Type { get; set; }
        [Hibernate]
        public int Level { get; set; }
        [Hibernate]
        public string Login { get; set; }
        [Hibernate]
        public string Password { get; set; }
        [Hibernate]
        public short[] OutputPorts { get; set; }
        [Hibernate]
        public bool Preload { get; }        
        [Hibernate]
        public PortInfo GpiPort { get; set; }
        #endregion
        
        private bool _isConnected;
        

        public RouterBase(CommunicatorType type)
        {
            Type = type;
            switch (type)
            {
                case CommunicatorType.Nevion:
                    Communicator = new NevionCommunicator(this);
                    break;
                case CommunicatorType.BlackmagicSmartVideoHub:
                    Communicator = new BlackmagicSmartVideoHubCommunicator(this);
                    break;
                case CommunicatorType.Atem:
                    Communicator = new AtemCommunicator(this);
                    break;
                case CommunicatorType.Ross:
                    Communicator = new RossCommunicator(this);
                    break;
                default:
                    return;
            }
            Communicator.ConnectionChanged += _communicator_ConnectionChanged;
        }

        protected IRouterCommunicator Communicator { get; private set; }

        private void _communicator_ConnectionChanged(object sender, EventArgs<bool> e)
        {
            IsConnected = e.Value;
            if (e.Value)
                return;

            _ = ConnectAsync();
        }

        

        public IList<IVideoSwitchPort> Sources => throw new NotImplementedException();

        public IVideoSwitchPort SelectedSource => throw new NotImplementedException();

        public bool IsConnected 
        { 
            get => _isConnected; 
            set
            {
                if (_isConnected == value)
                    return;
                _isConnected = value;
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler Started;

        public abstract Task<bool> ConnectAsync();        

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void SetSource(int inputId)
        {
            throw new NotImplementedException();
        }

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
