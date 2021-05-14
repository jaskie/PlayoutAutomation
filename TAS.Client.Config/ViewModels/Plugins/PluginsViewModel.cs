using System;
using System.Collections.Generic;
using System.Linq;
using TAS.Client.Common;
using TAS.Client.Config.Model;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;

namespace TAS.Client.Config.ViewModels.Plugins
{
    public class PluginsViewModel : ViewModelBase
    {
        public static string FileNameSearchPattern { get; } = "TAS.Server.*.dll";
        private IConfigEngine _engine;
        public event EventHandler PluginChanged;

        private PluginTypeViewModelBase _selectedPluginType;

        public PluginsViewModel(Engine engine)
        {
            _engine = engine;
            foreach (var configurationProvider in ConfigurationPluginManager.Current.ConfigurationProviders)
                switch (configurationProvider.GetPluginModelType())
                {
                    case Type type when typeof(ICGElementsController).IsAssignableFrom(type):
                        if (!PluginTypes.Any(pt => pt is CgElementsControllersViewModel))
                        {
                            var pluginTypeViewModel = new CgElementsControllersViewModel(engine);
                            PluginTypes.Add(pluginTypeViewModel);
                            pluginTypeViewModel.PluginChanged += PluginTypeViewModel_PluginChanged;
                        }
                        break;
                    case Type type when typeof(IVideoSwitch).IsAssignableFrom(type):
                        if (!PluginTypes.Any(pt => pt is VideoSwitchersViewModel))
                        {
                            var videoSwitchersViewModel = new VideoSwitchersViewModel(engine);
                            PluginTypes.Add(videoSwitchersViewModel);
                            videoSwitchersViewModel.PluginChanged += PluginTypeViewModel_PluginChanged;
                        }
                        break;
                }
            _selectedPluginType = PluginTypes.FirstOrDefault();
        }

   

        private void PluginTypeViewModel_PluginChanged(object sender, EventArgs e)
        {
            PluginChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool HasPlugins => PluginTypes.Any();
        
        public List<PluginTypeViewModelBase> PluginTypes { get; } = new List<PluginTypeViewModelBase>();
        
        public PluginTypeViewModelBase SelectedPluginType
        {
            get => _selectedPluginType;
            set
            {
                if (!SetField(ref _selectedPluginType, value))
                    return;                                
            }
        }
                                                           
        public void Save()
        {         
        }

        protected override void OnDispose()
        {
            foreach (var pluginType in PluginTypes)
                pluginType.PluginChanged -= PluginChanged;
        }             
    }
}
