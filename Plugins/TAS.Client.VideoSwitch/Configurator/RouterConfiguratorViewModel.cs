using System;
using System.Collections.Generic;
using System.Linq;
using TAS.Client.Common;
using TAS.Common.Interfaces;
using TAS.Database.Common.Interfaces;
using TAS.Server.VideoSwitch.Model;

namespace TAS.Server.VideoSwitch.Configurator
{
    public class RouterConfiguratorViewModel : ViewModelBase, IPluginConfiguratorViewModel
    {
        private bool _isEnabled;
        ConfiguratorViewModelBase _communicatorConfigurator;    
        private CommunicatorType _selectedCommunicatorType;
        private Router _router;
        private VideoSwitcher _videoSwitcher;
        IEngineProperties _engine;
               
        public RouterConfiguratorViewModel(IEngineProperties engine)
        {
            _engine = engine;
            Load();
        }

        
        public string PluginName => "Video switch";

        public bool IsEnabled 
        {
            get => CommunicatorConfigurator.IsEnabled;
            set
            {
                _isEnabled = value;
                CommunicatorConfigurator.IsEnabled = value;
                PluginChanged?.Invoke(this, EventArgs.Empty);                
                NotifyPropertyChanged();                
            }
        }                
        
        public event EventHandler PluginChanged;
        
        public CommunicatorType[] CommunicatorTypes { get; set; } = Enum.GetValues(typeof(CommunicatorType)).Cast<CommunicatorType>().ToArray();
        
        
        public CommunicatorType SelectedCommunicatorType 
        { 
            get => _selectedCommunicatorType;
            set
            {
                if (!SetField(ref _selectedCommunicatorType, value))
                    return;                
                
                switch(value)
                {
                    case CommunicatorType.Nevion:
                        CommunicatorConfigurator = new NevionConfiguratorViewModel(_router);                       
                        break;                    
                    case CommunicatorType.BlackmagicSmartVideoHub:
                        CommunicatorConfigurator = new BlackmagicConfiguratorViewModel(_router);
                        break;
                    case CommunicatorType.Atem:
                        CommunicatorConfigurator = new AtemConfiguratorViewModel(_videoSwitcher);
                        break;
                    case CommunicatorType.Ross:
                        CommunicatorConfigurator = new RossConfiguratorViewModel(_videoSwitcher);
                        break;
                    case CommunicatorType.None:
                        CommunicatorConfigurator = null;
                        break;
                }

                if (_router == null || _videoSwitcher == null)
                    CommunicatorConfigurator.IsEnabled = _isEnabled;
            }
        }                                
                   
        public ConfiguratorViewModelBase CommunicatorConfigurator 
        { 
            get => _communicatorConfigurator;
            set
            {
                if (!SetField(ref _communicatorConfigurator, value))
                    return;                
            }
        }

        public object GetModel()
        {
            return _communicatorConfigurator?.GetModel() ?? new Router();
        }

        public void Initialize(IPlugin plugin)
        {
            if (!(plugin is RouterBase routerBase))
            {
                if (plugin == null)
                    SelectedCommunicatorType = CommunicatorTypes.First();
                return;
            }

            if (plugin is VideoSwitcher videoSwitcher)
            {
                _videoSwitcher = videoSwitcher;
                _router = new Router
                {
                    IpAddress = videoSwitcher.IpAddress,
                    IsEnabled = videoSwitcher.IsEnabled,
                    Level = videoSwitcher.Level,
                    Login = videoSwitcher.Login,
                    Password = videoSwitcher.Password,
                    OutputPorts = videoSwitcher.OutputPorts,
                    Preload = videoSwitcher.Preload,
                    Sources = videoSwitcher.Sources,
                    Type = videoSwitcher.Type
                };
            }
            else if (plugin is Router router)
            {
                _router = router;
                _videoSwitcher = new VideoSwitcher
                {
                    IpAddress = router.IpAddress,
                    IsEnabled = router.IsEnabled,
                    Level = router.Level,
                    Login = router.Login,
                    Password = router.Password,
                    OutputPorts = router.OutputPorts,
                    Preload = router.Preload,
                    Sources = router.Sources,
                    Type = router.Type
                };
            }
                    
            SelectedCommunicatorType = CommunicatorTypes.FirstOrDefault(t => t == routerBase.Type);
            IsEnabled = _router.IsEnabled;
        }

        public void Save()
        {
            CommunicatorConfigurator.Save();
            if (CommunicatorConfigurator.GetModel() is VideoSwitcher videoSwitcher)
                _videoSwitcher = videoSwitcher;
            else if (CommunicatorConfigurator.GetModel() is Router router)
                _router = router;
        }

        public void Load()
        {

            //CommunicatorConfigurator.Undo();
        }

        protected override void OnDispose()
        {
            
        }
    }
}
