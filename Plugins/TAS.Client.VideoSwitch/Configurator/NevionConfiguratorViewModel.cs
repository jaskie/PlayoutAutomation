using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using TAS.Client.Common;
using TAS.Common.Interfaces;
using TAS.Server.VideoSwitch.Model;

namespace TAS.Server.VideoSwitch.Configurator
{
    internal class NevionConfiguratorViewModel : ConfiguratorViewModelBase
    {
        private bool _preload;
        private string _login;
        private string _password;
        private int _level;
        private string _ipAddress;                     
        private List<PortInfo> _ports;

        public NevionConfiguratorViewModel(Router router) : base(router)
        {
            CommandAddPort = new UiCommand(AddOutputPort, CanAddPort);
            CommandDeletePort = new UiCommand(DeleteOutputPort);
        }        

        private void TestRouter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IRouter.SelectedSource))
                NotifyPropertyChanged(nameof(SelectedTestSource));
            else if (e.PropertyName == nameof(IRouter.Sources))
                NotifyPropertyChanged(nameof(TestSources));
            else if (e.PropertyName == nameof(IRouter.IsConnected))
                NotifyPropertyChanged(nameof(IsConnected));
        }

        private void DeleteOutputPort(object obj)
        {
            if (!(obj is PortInfo port))
                return;
            _ports.Remove(port);
            Ports.Refresh();
        }

        private bool CanAddPort(object obj)
        {
            return IsEnabled;
        }

        private void AddOutputPort(object obj)
        {
            var lastItem = _ports.LastOrDefault();
            _ports.Add(new PortInfo((short)(lastItem == null ? 0 : lastItem.Id + 1), String.Empty));
            IsModified = true;
            Ports.Refresh();
        }

        protected override bool CanConnect(object obj)
        {
            if (TestRouter == null && IpAddress?.Length > 0)
                return true;

            return false;
        }

        protected override void Connect(object obj)
        {            
            TestRouter = new Router(CommunicatorType.Nevion)
            {
                IpAddress = IpAddress,
                Login = _login,
                Password = _password,
                Level = _level
            };

            TestRouter.OutputPorts = _ports.Select(p => p.Id).ToArray();

            TestRouter.PropertyChanged += TestRouter_PropertyChanged;
            _ = TestRouter.ConnectAsync();
        }

        protected override void Disconnect(object obj)
        {
            TestRouter.PropertyChanged -= TestRouter_PropertyChanged;
            base.Disconnect(obj);
        }

        protected override void Init()
        {
            _ports = new List<PortInfo>();
            Ports = CollectionViewSource.GetDefaultView(_ports);

            Level = 0;
            IpAddress = null;
            Login = null;
            Password = null;
            Preload = false;

            if (Router == null)
            {
                Ports.Refresh();
                IsModified = false;
                return;
            }

            Preload = Router.Preload;
            IpAddress = Router.IpAddress;
            Login = Router.Login;
            Password = Router.Password;
            Level = Router.Level;            

            if (Router?.OutputPorts != null)
                foreach (var port in Router.OutputPorts)
                    _ports.Add(new PortInfo(port, null));

            Ports.Refresh();
            IsModified = false;
        }

        protected override void OnDispose()
        {

        }

        public override void Save()
        {
            Router = new Router
            {
                Type = CommunicatorType.Nevion,                
                IpAddress = _ipAddress,
                Login = _login,
                Password = _password,
                Level = _level,
                IsEnabled = IsEnabled,
                Preload = _preload
            };

            Router.OutputPorts = _ports.Select(p => p.Id).ToArray();
            IsModified = false;
        }

        public override bool CanSave()
        {
            if (IsModified && _ipAddress?.Length>0 && _login?.Length > 0 && _password?.Length > 0)
                return true;
            return false;
        }

        public UiCommand CommandAddPort { get; }
        public UiCommand CommandDeletePort { get; }
        public ICollectionView Ports { get; private set; }                
        public string IpAddress { get => _ipAddress; set => SetField(ref _ipAddress, value); }                
        public string Login { get => _login; set => SetField(ref _login, value); }
        public string Password { get => _password; set => SetField(ref _password, value); }
        public int Level { get => _level; set => SetField(ref _level, value); }
        public bool Preload { get => _preload; set => SetField(ref _preload, value); }
        public IVideoSwitchPort SelectedTestSource
        {
            get => TestRouter?.SelectedSource;
            set
            {
                if (TestRouter?.Sources == value)
                    return;

                if (value == null)
                    return;

                TestRouter?.SetSource(value.PortId);
            }
        }
    }
}
