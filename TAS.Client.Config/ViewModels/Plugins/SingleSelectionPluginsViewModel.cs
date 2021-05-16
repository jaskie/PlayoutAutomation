using System;
using System.Collections.Generic;
using System.Linq;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;
using TAS.Database.Common.Interfaces;

namespace TAS.Client.Config.ViewModels.Plugins
{
    internal class SingleSelectionPluginsViewModel : PluginTypeViewModelBase
    {

        private IPluginConfiguratorViewModel _selectedConfigurator;

        public SingleSelectionPluginsViewModel(IEngineProperties engine, IEnumerable<IPluginConfigurationProvider> pluginConfigurationProviders, Type interfaceType, IPlugin selectedPlugin, string pluginTypeName): base(pluginTypeName)
        {
            Configurators = new List<IPluginConfiguratorViewModel> { new EmptyPluginConfiguratorViewModel(engine, interfaceType) };
            Configurators.AddRange(pluginConfigurationProviders.Where(p => p.GetPluginInterfaceType() == interfaceType).Select(p => p.GetConfiguratorViewModel(engine)));
            foreach (var configurator in Configurators)
                configurator.PluginChanged += Configurator_PluginChanged;
            _selectedConfigurator = Configurators.FirstOrDefault(p => p.Model == selectedPlugin) ?? Configurators.First();
        }

        private void Configurator_PluginChanged(object sender, EventArgs e)
        {
            IsModified = true;
        }

        public List<IPluginConfiguratorViewModel> Configurators { get; }

        public IPluginConfiguratorViewModel SelectedConfigurator
        {
            get => _selectedConfigurator;
            set => SetField(ref _selectedConfigurator, value);
        }

        protected override void OnDispose()
        {
            foreach (var configurator in Configurators)
                configurator.PluginChanged -= Configurator_PluginChanged;                
        }

        public override void Save()
        {
            _selectedConfigurator.Save();
        }
    }

    internal class EmptyPluginConfiguratorViewModel : IPluginConfiguratorViewModel
    {
        private readonly IEngineProperties _engine;
        private readonly Type _interfaceType;

        public EmptyPluginConfiguratorViewModel(IEngineProperties engine, Type interfaceType)
        {
            _engine = engine;
            _interfaceType = interfaceType;
        }

        public string PluginName { get; } = Common.Properties.Resources._none_;

        public event EventHandler PluginChanged;

        public void Dispose() { }

        public void Save() 
        {
            if (_interfaceType == typeof(ICGElementsController))
                _engine.CGElementsController = null;
            else if (_interfaceType == typeof(IVideoSwitch))
                _engine.VideoSwitch = null;
        }

        public void Load() { }

        public IPlugin Model => null;

        public override string ToString()
        {
            return "You can select a plugin from list above";
        }
                
    }
}
