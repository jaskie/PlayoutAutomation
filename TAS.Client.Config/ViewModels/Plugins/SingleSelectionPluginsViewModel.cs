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
        private bool _allowEnable;

        public SingleSelectionPluginsViewModel(IEngineProperties engine, IEnumerable<IPluginConfigurationProvider> pluginConfigurationProviders, Type interfaceType, IPlugin selectedPlugin, string pluginTypeName) : base(pluginTypeName)
        {
            Configurators = new List<IPluginConfiguratorViewModel> { new EmptyPluginConfiguratorViewModel(engine, interfaceType) };
            Configurators.AddRange(pluginConfigurationProviders.Where(p => p.GetPluginInterfaceType() == interfaceType).Select(p => p.GetConfiguratorViewModel(engine)));
            foreach (var configurator in Configurators)
                configurator.ModifiedChanged += Configurator_ModifiedChanged;
            _selectedConfigurator = Configurators.FirstOrDefault(p => p.Model == selectedPlugin) ?? Configurators.First();
            _allowEnable = _selectedConfigurator is not EmptyPluginConfiguratorViewModel;
        }

        private void Configurator_ModifiedChanged(object sender, EventArgs e)
        {
            IsModified = true;
        }

        public List<IPluginConfiguratorViewModel> Configurators { get; }

        public bool AllowEnable { get => _allowEnable; private set => SetFieldNoModify(ref _allowEnable, value); }

        public IPluginConfiguratorViewModel SelectedConfigurator
        {
            get => _selectedConfigurator;
            set
            {
                if (!SetField(ref _selectedConfigurator, value))
                    return;
                AllowEnable = value is not EmptyPluginConfiguratorViewModel;
            }
        }

        protected override void OnDispose()
        {
            foreach (var configurator in Configurators)
                configurator.ModifiedChanged -= Configurator_ModifiedChanged;
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

        public event EventHandler ModifiedChanged;

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

        public bool IsEnabled { get; set; }

        public override string ToString()
        {
            return "You can select a plugin from list above";
        }
                
    }
}
