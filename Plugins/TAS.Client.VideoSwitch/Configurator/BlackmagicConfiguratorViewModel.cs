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
    internal class BlackmagicConfiguratorViewModel : ConfiguratorViewModelBase
    {       
        private string _ipAddress;
        private bool _preload;
        private List<PortInfo> _ports;

        public BlackmagicConfiguratorViewModel(Router router) : base(router)
        {
            CommandAddPort = new UiCommand(AddOutputPort, CanAddPort);
            CommandDeletePort = new UiCommand(DeleteOutputPort);
        }

        private void TestRouter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IVideoSwitch.SelectedSource))
                NotifyPropertyChanged(nameof(SelectedTestSource));
            else if (e.PropertyName == nameof(IVideoSwitch.Sources))
                NotifyPropertyChanged(nameof(TestSources));
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
            if (TestRouter == null && IpAddress?.Length > 0)
                return true;

            return false;
        }

        protected override void Connect(object obj)
        {
            TestRouter = new Router(CommunicatorType.BlackmagicSmartVideoHub)
            {
                IpAddress = IpAddress,            
            };

            TestRouter.OutputPorts = _ports.Select(p => p.Id).ToArray();

            TestRouter.PropertyChanged += TestRouter_PropertyChanged;
            TestRouter.Connect();
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

            IpAddress = null;
            Preload = false;

            if (Router == null)
            {
                Ports.Refresh();
                IsModified = false;
                return;
            }
            
            IpAddress = Router.IpAddress;
            Preload = Router.Preload;

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
                Type = CommunicatorType.BlackmagicSmartVideoHub,                
                IpAddress = _ipAddress,
                IsEnabled = IsEnabled,
                Preload = _preload
            };

            Router.OutputPorts = _ports.Select(p => p.Id).ToArray();

            IsModified = false;
        }

        public override bool CanSave()
        {
            if (IsModified && _ipAddress?.Length > 0)
                return true;

            return false;
        }

        public UiCommand CommandAddPort { get; }
        public UiCommand CommandDeletePort { get; }
        public ICollectionView Ports { get; private set; }        
        public string IpAddress { get => _ipAddress; set => SetField(ref _ipAddress, value); }
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
