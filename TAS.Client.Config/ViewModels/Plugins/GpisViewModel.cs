using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;

namespace TAS.Client.Config.ViewModels.Plugins
{
    internal class GpisViewModel : PluginTypeViewModelBase
    {
        private IEnginePersistent _engine;

        private List<IPluginConfiguratorViewModel> _configurators = new List<IPluginConfiguratorViewModel>();
        private IPluginConfiguratorViewModel _selectedConfigurator;

        private bool? _isEnabled = null;

        public GpisViewModel(IEnginePersistent engine) : base("GPI")
        {
            _engine = engine;

            //foreach (var plugin in ConfigurationPluginManager.Current.Gpis)
            //{
            //    var configuratorVm = plugin.GetConfiguratorViewModel();
            //    configuratorVm.PluginChanged += PluginConfigurator_PluginChanged;
            //    configuratorVm.Initialize(_engine.Gpis?.FirstOrDefault(g => g?.GetType() == configuratorVm.GetModel().GetType()));
            //    _configurators.Add(configuratorVm);
            //}

            Configurators = CollectionViewSource.GetDefaultView(_configurators);             
        }

        private void PluginConfigurator_ModifiedChanged(object sender, EventArgs e)
        {
            IsModified = true;
        }

        public override void Save()
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
                configurator.ModifiedChanged -= PluginConfigurator_ModifiedChanged;
                configurator.Dispose();                
            }
        }
    }
}
