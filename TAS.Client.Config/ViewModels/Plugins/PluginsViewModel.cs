using System;
using System.Collections.Generic;
using System.Linq;
using TAS.Client.Common;
using TAS.Client.Config.Model;
using TAS.Common.Interfaces;
using TAS.Database.Common.Interfaces;

namespace TAS.Client.Config.ViewModels.Plugins
{
    public class PluginsViewModel : ViewModelBase
    {
        private PluginTypeViewModelBase _selectedPluginType;

        public PluginsViewModel(Engine engine)
        {
            var configurationProviders = ConfigurationPluginManager.Current.ConfigurationProviders;
            AddSingleSelectionPluginType(engine, configurationProviders, typeof(IVideoSwitch), engine.VideoSwitch, "Video switchers and routers");
            AddSingleSelectionPluginType(engine, configurationProviders, typeof(ICGElementsController), engine.CGElementsController, "Channel branding controllers");
            _selectedPluginType = PluginTypes.FirstOrDefault();
        }

        private void AddSingleSelectionPluginType(IEngineProperties engine, IEnumerable<IPluginConfigurationProvider> configurationProviders, Type interfaceType, IPlugin activePlugin, string pluginTypeName)
        {
            if (!configurationProviders.Any(p => p.GetPluginInterfaceType() == interfaceType))
                return;
            var pluginTypeVm = new SingleSelectionPluginsViewModel(engine, configurationProviders, interfaceType, activePlugin, pluginTypeName);
            pluginTypeVm.ModifiedChanged += PluginTypeVm_ModifiedChanged;
            PluginTypes.Add(pluginTypeVm);
        }

        public event EventHandler PluginChanged;

        private void PluginTypeVm_ModifiedChanged(object sender, EventArgs e)
        {
            PluginChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool HasPlugins => PluginTypes.Any();
        
        public List<PluginTypeViewModelBase> PluginTypes { get; } = new List<PluginTypeViewModelBase>();
        
        public PluginTypeViewModelBase SelectedPluginType
        {
            get => _selectedPluginType;
            set => SetField(ref _selectedPluginType, value);
        }

        public void Save()
        {
            foreach (var pluginType in PluginTypes)
                pluginType.Save();
        }

        protected override void OnDispose()
        {
            foreach (var pluginType in PluginTypes)
            {
                pluginType.ModifiedChanged -= PluginTypeVm_ModifiedChanged;
                pluginType.Dispose();
            }
        }             
    }
}
