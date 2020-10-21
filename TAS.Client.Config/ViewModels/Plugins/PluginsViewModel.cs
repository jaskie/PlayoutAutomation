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

            if (ConfigurationPluginManager.Current.ConfigurationProviders.Any(c => typeof(ICGElementsController).IsAssignableFrom(c.GetPluginModelType())))
            {
                var pluginTypeViewModel = new CgElementsControllersViewModel(engine);
                PluginTypes.Add(pluginTypeViewModel);
                pluginTypeViewModel.PluginChanged += PluginTypeViewModel_PluginChanged;                
            }
            //TODO
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
