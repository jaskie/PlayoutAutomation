using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;

namespace TAS.Client.Config.ViewModels.Plugins
{
    internal class GpisViewModel : PluginTypeViewModelBase
    {
        private IEnginePersistent _engine;

        private IPluginConfiguratorViewModel _selectedConfigurator;

        private bool? _isEnabled = null;

        public GpisViewModel(IEnginePersistent engine) : base("GPI")
        {
            _engine = engine;

            Configurators = new List<IPluginConfiguratorViewModel>(ConfigurationPluginManager.Current.ConfigurationProviders
                .Where(p => typeof(IStartGpi).IsAssignableFrom(p.GetPluginInterfaceType()))
                .Select(p =>
                {
                    var vm = p.GetConfiguratorViewModel(engine);
                    vm.ModifiedChanged += PluginConfigurator_ModifiedChanged;
                    return vm;
                }));
            SelectedConfigurator = Configurators.FirstOrDefault();
        }
        public List<IPluginConfiguratorViewModel> Configurators { get; }

        private void PluginConfigurator_ModifiedChanged(object sender, EventArgs e)
        {
            IsModified = true;
        }

        public override void Save()
        {
            foreach (var configurator in Configurators)
                configurator.Save();
        }

        
        public IPluginConfiguratorViewModel SelectedConfigurator
        {
            get => _selectedConfigurator;
            set
            {
                if (!SetField(ref _selectedConfigurator, value))
                    return;
            }
        }

        public bool? IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (!SetField(ref _isEnabled, value))
                    return;
            }
        }


        protected override void OnDispose()
        {
            foreach (var configurator in Configurators)
            {
                configurator.ModifiedChanged -= PluginConfigurator_ModifiedChanged;
                configurator.Dispose();                
            }
        }
    }
}
