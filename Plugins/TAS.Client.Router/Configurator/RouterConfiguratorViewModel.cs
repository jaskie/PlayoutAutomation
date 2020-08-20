using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Data;
using TAS.Client.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;
using TAS.Server.VideoSwitch.Model;

namespace TAS.Server.VideoSwitch.Configurator
{
    [Export(typeof(IPluginConfigurator))]
    public class RouterConfiguratorViewModel : ModifyableViewModelBase, IPluginConfigurator
    {
        private IConfigEngine _engine;
        private VideoSwitch _router = new VideoSwitch();
        private VideoSwitch _testRouter;

        private bool _isEnabled;        
        private string _ipAddress;
        private string _login;
        private string _password;
        private int _level;        
        private VideoSwitch.VideoSwitchType? _selectedRouterType;
        private List<PortInfo> _ports;
        private bool _requiresAuthentication;
        private bool _requiresLevel;
        private bool _requiresPorts;

        [ImportingConstructor]
        public RouterConfiguratorViewModel([Import("Engine")]IConfigEngine engine)
        {
            _engine = engine;            

            CommandAddOutputPort = new UiCommand(AddOutputPort, CanAddOutputPort);
            CommandConnect = new UiCommand(Connect, CanConnect);
            CommandDisconnect = new UiCommand(Disconnect, CanDisconnect);
            CommandSave = new UiCommand(Save, CanSave);
            CommandUndo = new UiCommand(Undo, CanUndo);
            CommandDeleteOutputPort = new UiCommand(Delete);            
        }

        private bool CheckRequirements()
        {
            if (_requiresAuthentication && (_login == null || _password == null || _login.Length<1 || _password.Length<1))
                return false;

            if (_requiresLevel && _level < 0)
                return false;

            if (_requiresPorts && _ports?.Count < 1)
                return false;

            return true;
        }

        private bool CanAddOutputPort(object obj)
        {
            return _isEnabled;
        }

        private void Delete(object obj)
        {
            if (!(obj is PortInfo port))
                return;
            _ports.Remove(port);
            Ports.Refresh();
        }

        private void Save(object obj)
        {
            _router = new VideoSwitch
            {
                Type = _selectedRouterType ?? VideoSwitch.VideoSwitchType.Unknown,
                IpAddress = _ipAddress,
                Login = _login,
                Password = _password,
                Level = _level,                
                IsEnabled = _isEnabled
            };

            if (_selectedRouterType == VideoSwitch.VideoSwitchType.Ross)
                foreach (var port in _ports)
                    _router.InputPorts.Add(new RouterPort(port.Id, port.Name));
            else
                _router.OutputPorts = _ports.Select(p => p.Id).ToArray();

            PluginChanged?.Invoke(this, EventArgs.Empty);
            IsModified = false;
        }

        private bool CanSave(object obj)
        {
            if (!IsModified || _ipAddress?.Length < 1)
                return false;

            return CheckRequirements();
        }        

        private bool CanUndo(object obj)
        {
            return IsModified;
        }

        private void Undo(object obj)
        {
            Init();
            IsModified = false;
        }

        private bool CanDisconnect(object obj)
        {
            if (_testRouter != null)
                return true;

            return false;
        }

        private void Disconnect(object obj)
        {
            _testRouter.PropertyChanged -= TestRouter_PropertyChanged;
            _testRouter.Dispose();
            _testRouter = null;

            NotifyPropertyChanged(nameof(IsConnected));
        }

        private bool CanConnect(object obj)
        {
            if (_testRouter != null || _ipAddress?.Length<1)
                return false;            

            return CheckRequirements();            
        }

        private void Connect(object obj)
        {
            _testRouter = new VideoSwitch(_selectedRouterType ?? VideoSwitch.VideoSwitchType.Unknown)
            {
                Type = _selectedRouterType ?? VideoSwitch.VideoSwitchType.Unknown,
                IpAddress = _ipAddress,
                Login = _login,
                Password = _password,
                Level = _level,
                
            };
            if (_selectedRouterType == VideoSwitch.VideoSwitchType.Ross)
                foreach (var port in _ports)
                    _testRouter.InputPorts.Add(new RouterPort(port.Id, port.Name));
            else                
                _testRouter.OutputPorts = _ports.Select(p => p.Id).ToArray();

            _testRouter.PropertyChanged += TestRouter_PropertyChanged;
            _testRouter.Connect();          
        }

        private void TestRouter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IVideoSwitch.SelectedInputPort))
                NotifyPropertyChanged(nameof(SelectedInputPort));
            else if (e.PropertyName == nameof(IVideoSwitch.InputPorts))                          
                NotifyPropertyChanged(nameof(InputPorts));                            
            else if (e.PropertyName == nameof(IVideoSwitch.IsConnected))
                NotifyPropertyChanged(nameof(IsConnected));
        }

        private void AddOutputPort(object obj)
        {
            var lastItem = _ports.LastOrDefault();           
            _ports.Add(new PortInfo((short)(lastItem == null ? 0 : lastItem.Id+1), String.Empty));
            IsModified = true;
            Ports.Refresh();
        }

        private void Init()
        {
            _ports = new List<PortInfo>();
            Ports = CollectionViewSource.GetDefaultView(_ports);
            NotifyPropertyChanged(nameof(Ports));

            _level = 0;
            _ipAddress = null;
            _login = null;
            _password = null;
            _selectedRouterType = null;
           
            if (_router == null)
                return;

            IpAddress = _router.IpAddress;
            SelectedRouterType = RouterTypes.FirstOrDefault(r => r == ((VideoSwitch)_router).Type);
            Login = _router.Login;
            Password = _router.Password;
            Level = _router.Level;
            IsEnabled = _router.IsEnabled;
           
            if(_selectedRouterType == VideoSwitch.VideoSwitchType.Ross)
            {
                foreach (var port in _router.InputPorts)
                    _ports.Add(new PortInfo(port.PortId, port.PortName));
            }
            else
            {
                foreach (var port in _router.OutputPorts)
                    _ports.Add(new PortInfo(port, null));
            }
            
            IsModified = false;            
        }

        public string PluginName => "VideoSwitch";

        public bool IsEnabled 
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value)
                    return;
                _isEnabled = value;
                
                if (_router != null)
                    _router.IsEnabled = value;

                NotifyPropertyChanged();
                PluginChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool IsConnected => _testRouter?.IsConnected ?? false;
        public IVideoSwitchPort SelectedInputPort
        {
            get => _testRouter?.SelectedInputPort;
            set
            {
                if (_testRouter?.InputPorts == value)
                    return;

                if (value == null)
                    return;

                _testRouter?.SelectInput(value.PortId);
            }
        }
        public IList<IVideoSwitchPort> InputPorts => _testRouter?.InputPorts;
        public event EventHandler PluginChanged;
        public ICollectionView Ports { get; private set; }        
        public List<VideoSwitch.VideoSwitchType> RouterTypes { get; set; } = Enum.GetValues(typeof(VideoSwitch.VideoSwitchType)).Cast<VideoSwitch.VideoSwitchType>().ToList();
        public string Login { get => _login; set => SetField(ref _login, value); }
        public string Password { get => _password; set => SetField(ref _password, value); }
        public int Level { get => _level; set => SetField(ref _level, value); }
        public VideoSwitch.VideoSwitchType? SelectedRouterType 
        { 
            get => _selectedRouterType;
            set
            {
                if (!SetField(ref _selectedRouterType, value))
                    return;

                switch(value)
                {
                    case VideoSwitch.VideoSwitchType.Nevion:
                        RequiresAuthentication = true;
                        RequiresLevel = true;
                        RequiresPorts = true;
                        break;
                    case VideoSwitch.VideoSwitchType.BlackmagicSmartVideoHub:
                    case VideoSwitch.VideoSwitchType.Unknown:
                        RequiresAuthentication = false;
                        RequiresLevel = false;
                        RequiresPorts = true;
                        break;
                    case VideoSwitch.VideoSwitchType.Atem:                    
                        RequiresAuthentication = false;
                        RequiresLevel = true;
                        RequiresPorts = false;
                        break;
                    case VideoSwitch.VideoSwitchType.Ross:
                        RequiresAuthentication = false;
                        RequiresLevel = false;
                        RequiresPorts = true;
                        break;
                }
            }
        }
        public UiCommand CommandAddOutputPort { get; }
        public UiCommand CommandConnect { get; }
        public UiCommand CommandDisconnect { get; }
        public UiCommand CommandSave { get; }
        public UiCommand CommandUndo { get; }
        public UiCommand CommandDeleteOutputPort { get; }        
        public string IpAddress { get => _ipAddress; set => SetField(ref _ipAddress, value); }
        
        public bool RequiresAuthentication { get => _requiresAuthentication; set => SetField(ref _requiresAuthentication, value); }
        public bool RequiresLevel { get => _requiresLevel; set => SetField(ref _requiresLevel, value); }
        public bool RequiresPorts { get => _requiresPorts; set => SetField(ref _requiresPorts, value); }

        public object GetModel()
        {
            return _router;
        }

        public void Initialize(object parameter)
        {
            UiServices.AddDataTemplate(typeof(RouterConfiguratorViewModel), typeof(RouterConfiguratorView));                     
            _router = parameter is VideoSwitch router ? router : null;
            Init();
        }

        public void Save()
        {
            
        }

        protected override void OnDispose()
        {            
        }
    }
}
