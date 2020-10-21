using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;
using TAS.Common.Interfaces.Configurator;
using TAS.Database.Common.Interfaces;

namespace TAS.Client.Config.ViewModels.Plugins
{
    internal class GpisViewModel : PluginTypeViewModelBase
    {
        private IConfigEngine _engine;

        private List<IPluginConfiguratorViewModel> _configurators = new List<IPluginConfiguratorViewModel>();
        private IPluginConfiguratorViewModel _selectedConfigurator;

        private bool? _isEnabled = null;

        public GpisViewModel(IConfigEngine engine)
        {
            _engine = engine;
            Name = "GPI";

            //foreach (var plugin in ConfigurationPluginManager.Current.Gpis)
            //{
            //    var configuratorVm = plugin.GetConfiguratorViewModel();
            //    configuratorVm.PluginChanged += PluginConfigurator_PluginChanged;
            //    configuratorVm.Initialize(_engine.Gpis?.FirstOrDefault(g => g?.GetType() == configuratorVm.GetModel().GetType()));
            //    _configurators.Add(configuratorVm);
            //}

            Configurators = CollectionViewSource.GetDefaultView(_configurators);             
        }

        private void PluginConfigurator_PluginChanged(object sender, EventArgs e)
        {
            RaisePluginChanged();
        }

        public void Save()
        {
            foreach (var configurator in _configurators)
                configurator.Save();
        }

        public ICollectionView Configurators { get; }
        
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
