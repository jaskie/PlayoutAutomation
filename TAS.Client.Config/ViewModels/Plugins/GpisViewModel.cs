using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using TAS.Client.Common;
using TAS.Client.Config.Model;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;
using TAS.Database.Common.Interfaces;

namespace TAS.Client.Config.ViewModels.Plugins
{
    public class GpisViewModel : ViewModelBase, IPluginManager
    {
        private IConfigEngine _engine;
        public event EventHandler PluginChanged;

        private List<IPluginConfiguratorViewModel> _configurators = new List<IPluginConfiguratorViewModel>();
        private IPluginConfiguratorViewModel _selectedConfigurator;

        private bool? _isEnabled = null;

        public GpisViewModel(IConfigEngine engine)
        {
            _engine = engine;

            foreach (var plugin in ConfigurationPluginManager.Current.Gpis)
            {
                var configuratorVm = plugin.GetConfiguratorViewModel();
                configuratorVm.PluginChanged += PluginConfigurator_PluginChanged;
                configuratorVm.Initialize(_engine.Gpis?.FirstOrDefault(g => g?.GetType() == configuratorVm.GetModel().GetType()));
                _configurators.Add(configuratorVm);
            }

            Configurators = CollectionViewSource.GetDefaultView(_configurators);             
        }

        private void PluginConfigurator_PluginChanged(object sender, EventArgs e)
        {            
            PluginChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Save()
        {
            foreach (var configurator in _configurators)
                configurator.Save();
        }

        public ICollectionView Configurators { get; }
        public string Name => "GPI";
        public List<IGpi> Gpis => _configurators.Select(c => c.GetModel()).Cast<IGpi>().ToList();

        public IPluginConfiguratorViewModel SelectedConfigurator
        {
            get => _selectedConfigurator;
            set
            {
                if (!SetField(ref _selectedConfigurator, value))
                    return;

                //_isEnabled = _selectedConfigurator?.IsEnabled ?? false;
                //NotifyPropertyChanged(nameof(IsEnabled));                
            }
        }

        public bool? IsEnabled
        {
            get => _isEnabled;
            set
            {
                //if (!SetField(ref _isEnabled, value))
                //    return;

                //if (value == null)
                //    return;

                //foreach (var configurator in _configurators)
                //    configurator.IsEnabled = (bool)value;
            }
        }


        protected override void OnDispose()
        {
            foreach (var configurator in _configurators)
            {
                configurator.PluginChanged -= PluginConfigurator_PluginChanged;
                configurator.Dispose();                
            }
        }
    }
}
