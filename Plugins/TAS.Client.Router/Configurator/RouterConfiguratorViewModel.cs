using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Data;
using TAS.Client.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;
using TAS.Server.Router.Model;

namespace TAS.Server.Router.Configurator
{
    [Export(typeof(IPluginConfigurator))]
    public class RouterConfiguratorViewModel : ModifyableViewModelBase, IPluginConfigurator
    {
        private IConfigEngine _engine;
        private IRouter _router = new Router();
        private IRouter _testRouter;

        private bool _isEnabled;
        private bool _isExtendedType;
        private string _ipAddress;
        private string _login;
        private string _password;
        private int _level;        
        private Router.RouterType? _selectedRouterType;
        private List<PortInfo> _outputPorts;        
        
        [ImportingConstructor]
        public RouterConfiguratorViewModel([Import("Engine")]IConfigEngine engine)
        {
            _engine = engine;            

            AddOutputPortCommand = new UiCommand(AddOutputPort, CanAddOutputPort);
            ConnectCommand = new UiCommand(Connect, CanConnect);
            DisconnectCommand = new UiCommand(Disconnect, CanDisconnect);
            SaveCommand = new UiCommand(Save, CanSave);
            UndoCommand = new UiCommand(Undo, CanUndo);
            DeleteOutputPortCommand = new UiCommand(Delete);
        }

        private bool CanAddOutputPort(object obj)
        {
            return _isEnabled;
        }

        private void Delete(object obj)
        {
            if (!(obj is PortInfo port))
                return;
            _outputPorts.Remove(port);
            OutputPorts.Refresh();
        }

        private void Save(object obj)
        {
            _router = new Router
            {
                Type = _selectedRouterType ?? Router.RouterType.Unknown,
                IpAddress = _ipAddress,
                Login = _login,
                Password = _password,
                Level = _level,
                OutputPorts = _outputPorts.Select(p => p.Id).ToArray(),
                IsEnabled = _isEnabled
            };

            PluginChanged?.Invoke(this, EventArgs.Empty);
            IsModified = false;
        }

        private bool CanSave(object obj)
        {
            if (_selectedRouterType == Router.RouterType.Nevion)
            {
                if (_ipAddress.Length > 0 && _login.Length > 0 && _password.Length > 0 && IsModified)
                    return true;
            }
            else if (_selectedRouterType == Router.RouterType.BlackmagicSmartVideoHub)
            {
                if (_ipAddress.Length > 0 && IsModified)
                    return true;
            }
            return false;
        }

        private bool CanUndo(object obj)
        {
            return IsModified;
        }

        private void Undo(object obj)
        {
            Init();
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
            if (_selectedRouterType == Router.RouterType.Nevion)
            {
                if (_testRouter == null && _ipAddress.Length > 0 && _login.Length > 0 && _password.Length > 0 && _outputPorts.Count > 0)
                    return true;
            }
            else if (_testRouter == null && _outputPorts?.Count > 0 && _selectedRouterType == Router.RouterType.BlackmagicSmartVideoHub)
            {
                if (_ipAddress.Length > 0)
                    return true;
            }
            
            return false;
        }

        private void Connect(object obj)
        {
            _testRouter = new Router(_selectedRouterType ?? Router.RouterType.Unknown)
            {
                Type = _selectedRouterType ?? Router.RouterType.Unknown,
                IpAddress = _ipAddress,
                Login = _login,
                Password = _password,
                Level = _level,
                OutputPorts = _outputPorts.Select(p => p.Id).ToArray()
            };
            _testRouter.PropertyChanged += TestRouter_PropertyChanged;
            _testRouter.Connect();          
        }

        private void TestRouter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IRouter.SelectedInputPort))
                NotifyPropertyChanged(nameof(SelectedInputPort));
            else if (e.PropertyName == nameof(IRouter.InputPorts))                          
                NotifyPropertyChanged(nameof(InputPorts));                            
            else if (e.PropertyName == nameof(IRouter.IsConnected))
                NotifyPropertyChanged(nameof(IsConnected));
        }

        private void AddOutputPort(object obj)
        {
            var lastItem = _outputPorts.LastOrDefault();           
            _outputPorts.Add(new PortInfo((short)(lastItem == null ? 0 : lastItem.Id+1), String.Empty));
            IsModified = true;
            OutputPorts.Refresh();
        }

        private void Init()
        {
            _outputPorts = new List<PortInfo>();
            OutputPorts = CollectionViewSource.GetDefaultView(_outputPorts);
            _level = 0;
            _ipAddress = null;
            _login = null;
            _password = null;
            _selectedRouterType = null;

            if (_router == null)
                return;

            IpAddress = _router.IpAddress;
            SelectedRouterType = RouterTypes.FirstOrDefault(r => r == ((Router)_router).Type);
            Login = _router.Login;
            Password = _router.Password;
            Level = _router.Level;
            IsEnabled = _router.IsEnabled;

            if (_router.OutputPorts != null)
                foreach (var outputPort in _router.OutputPorts)
                    _outputPorts.Add(new PortInfo(outputPort, null));

            IsModified = false;            
        }

        public string PluginName => "Router";

        public bool IsEnabled 
        {
            get => _isEnabled;
            set
            {
                if (!SetField(ref _isEnabled, value))
                    return;
                
                if (_router != null)
                    _router.IsEnabled = value;
                PluginChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool IsConnected => _testRouter?.IsConnected ?? false;
        public IRouterPort SelectedInputPort
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
        public IList<IRouterPort> InputPorts => _testRouter?.InputPorts;
        public event EventHandler PluginChanged;
        public ICollectionView OutputPorts { get; private set; }        
        public List<Router.RouterType> RouterTypes { get; set; } = Enum.GetValues(typeof(Router.RouterType)).Cast<Router.RouterType>().ToList();
        public string Login { get => _login; set => SetField(ref _login, value); }
        public string Password { get => _password; set => SetField(ref _password, value); }
        public int Level { get => _level; set => SetField(ref _level, value); }
        public Router.RouterType? SelectedRouterType 
        { 
            get => _selectedRouterType;
            set
            {
                if (!SetField(ref _selectedRouterType, value))
                    return;

                if (value == Router.RouterType.Nevion)
                    IsExtendedType = true;
                else
                    IsExtendedType = false;
            }
        }
        public UiCommand AddOutputPortCommand { get; }
        public UiCommand ConnectCommand { get; }
        public UiCommand DisconnectCommand { get; }
        public UiCommand SaveCommand { get; }
        public UiCommand UndoCommand { get; }
        public UiCommand DeleteOutputPortCommand { get; }
        public string IpAddress { get => _ipAddress; set => SetField(ref _ipAddress, value); }
        public bool IsExtendedType { get => _isExtendedType; set => SetField(ref _isExtendedType, value); }

        public object GetModel()
        {
            return _router;
        }

        public void Initialize(object parameter)
        {
            UiServices.AddDataTemplate(typeof(RouterConfiguratorViewModel), typeof(RouterConfiguratorView));                     
            _router = parameter is IRouter router ? router : null;
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
