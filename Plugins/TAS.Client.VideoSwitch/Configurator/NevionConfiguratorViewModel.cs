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
    public class NevionConfiguratorViewModel : ConfiguratorViewModelBase
    {
        private bool _preload;
        private string _login;
        private string _password;
        private int _level;
        private string _ipAddress;                     
        private List<PortInfo> _ports;
        private readonly Nevion _nevion;

        public NevionConfiguratorViewModel(IEngineProperties engine) : base(engine)
        {
            _nevion = engine.VideoSwitch as Nevion ?? new Nevion();
            _nevion.PropertyChanged += Nevion_PropertyChanged;
            CommandAddPort = new UiCommand(AddOutputPort, CanAddPort);
            CommandDeletePort = new UiCommand(DeleteOutputPort);
            Load();
        }

        public override string PluginName => "Nevion video router";

        public override IPlugin Model => _nevion;

        private void Nevion_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IVideoSwitch.SelectedSource))
                NotifyPropertyChanged(nameof(SelectedSource));
            else if (e.PropertyName == nameof(IVideoSwitch.IsConnected))
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
            if (IpAddress?.Length > 0)
                return true;

            return false;
        }

        protected override void Connect(object obj)
        {
            _nevion.IpAddress = IpAddress;
            _nevion.Login = _login;
            _nevion.Password = _password;
            _nevion.Level = _level;
            _nevion.OutputPorts = _ports.Select(p => p.Id).ToArray();

            _nevion.Connect();
        }

        protected override void Disconnect(object obj)
        {
            _nevion.Disconnect();
            base.Disconnect(obj);
        }

        public override void Load()
        {
            _ports = new List<PortInfo>();
            Ports = CollectionViewSource.GetDefaultView(_ports);

            Level = _nevion.Level;
            IpAddress = _nevion.IpAddress;
            Login = _nevion.Login;
            Password = _nevion.Password;
            Preload = _nevion.Preload;
            Ports.Refresh();
            IsModified = false;
        }

        protected override void OnDispose()
        {
            _nevion.PropertyChanged -= Nevion_PropertyChanged;
        }

        public override void Save()
        {
            _nevion.Level = Level;
            _nevion.IpAddress = IpAddress;
            _nevion.Login = Login;
            _nevion.Password = Password;
            _nevion.Preload = Preload;
            Engine.VideoSwitch = _nevion;
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
        public IVideoSwitchPort SelectedSource
        {
            get => _nevion.SelectedSource;
            set
            {
                if (_nevion?.SelectedSource == value)
                    return;
                _nevion?.SetSource(value.PortId);
            }
        }
    }
}
