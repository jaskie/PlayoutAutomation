using System;
using System.Collections.Generic;
using System.Linq;
using TAS.Client.Common;
using TAS.Database.Common.Interfaces;
using TAS.Server.VideoSwitch.Model;

namespace TAS.Server.VideoSwitch.Configurator
{
    public class RouterConfiguratorViewModel : ViewModelBase, IPluginConfiguratorViewModel
    {
        private bool _isEnabled;
        ConfiguratorViewModelBase _communicatorConfigurator;    
        private CommunicatorType? _selectedCommunicatorType;
        private Router _router;
        private VideoSwitcher _videoSwitcher;
               
        public RouterConfiguratorViewModel()
        {                        
            CommandSave = new UiCommand(UpdateModel, CanUpdateModel);
            CommandUndo = new UiCommand(Undo, CanUndo);            
        }

        private bool CanUpdateModel(object obj)
        {
            return CommunicatorConfigurator.CanSave();
        }

        private void UpdateModel(object obj = null)
        {
            CommunicatorConfigurator.Save();

            if (CommunicatorConfigurator.GetModel() is VideoSwitcher videoSwitcher)
                _videoSwitcher = videoSwitcher;
            else if (CommunicatorConfigurator.GetModel() is Router router)
                _router = router;

            PluginChanged?.Invoke(this, EventArgs.Empty);
        }         

        private bool CanUndo(object obj)
        {
            return CommunicatorConfigurator.CanUndo();
        }

        private void Undo(object obj)
        {
            CommunicatorConfigurator.Undo();            
        }                                     

        public string PluginName => "VideoSwitch";

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
        
        public List<CommunicatorType> CommunicatorTypes { get; set; } = Enum.GetValues(typeof(CommunicatorType)).Cast<CommunicatorType>().ToList();
        
        
        public CommunicatorType? SelectedCommunicatorType 
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
                    case CommunicatorType.Unknown:
                        CommunicatorConfigurator = new BlackmagicConfiguratorViewModel(_router);
                        break;
                    case CommunicatorType.Atem:
                        CommunicatorConfigurator = new AtemConfiguratorViewModel(_videoSwitcher);
                        break;
                    case CommunicatorType.Ross:
                        CommunicatorConfigurator = new RossConfiguratorViewModel(_videoSwitcher);
                        break;
                }

                if (_router == null || _videoSwitcher == null)
                    CommunicatorConfigurator.IsEnabled = _isEnabled;
            }
        }                                
                    
        
        public UiCommand CommandSave { get; }
        public UiCommand CommandUndo { get; }
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

        public void Initialize(object parameter)
        {
            if (!(parameter is RouterBase routerBase))
            {
                if (parameter == null)
                    SelectedCommunicatorType = CommunicatorTypes.First();
                return;
            }

            if (parameter is VideoSwitcher videoSwitcher)
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
            else if (parameter is Router router)
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

        }

        protected override void OnDispose()
        {
            
        }
    }
}
