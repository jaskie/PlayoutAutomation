using System;
using System.Linq;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;
using TAS.Database.Common.Interfaces;

namespace TAS.Client.Config.ViewModels.Plugins
{
    internal class CgElementsControllersViewModel : PluginTypeViewModelBase
    {
        private IPluginConfiguratorViewModel _selectedConfigurator;
                
        public CgElementsControllersViewModel(IConfigEngine engine)
        {
            Name = "Channel branding controllers";

            Configurators = ConfigurationPluginManager.Current.ConfigurationProviders
                .Where(p => typeof(ICGElementsController).IsAssignableFrom(p.GetPluginModelType()))
                .Select(p =>
                {
                    var configuratorVm = p.GetConfiguratorViewModel(engine);
                    configuratorVm.PluginChanged += PluginConfigurator_PluginChanged;
                    configuratorVm.Initialize(engine.CGElementsController);
                    return configuratorVm;
                })
                .ToArray();
            SelectedConfigurator = Configurators.FirstOrDefault();
        }

        private void PluginConfigurator_PluginChanged(object sender, EventArgs e)
        {
            RaisePluginChanged();
        }
        
        public IPluginConfiguratorViewModel SelectedConfigurator
        {
            get => _selectedConfigurator;
            set => SetField(ref _selectedConfigurator, value);
        }
        public IPluginConfiguratorViewModel[] Configurators { get; }

        protected override void OnDispose()
        {
            foreach (var cgConfigurator in Configurators)           
                cgConfigurator.PluginChanged -= PluginConfigurator_PluginChanged;            
        }

        public void Save()
        {
            _selectedConfigurator.Save();
        }       
    }
}
