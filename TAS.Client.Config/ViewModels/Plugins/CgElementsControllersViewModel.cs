using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using TAS.Client.Common;
using TAS.Client.Config.Model;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;

namespace TAS.Client.Config.ViewModels.Plugins
{
    public class CgElementsControllersViewModel : ViewModelBase, IPluginManager
    {
        private IConfigEngine _engine;
        public event EventHandler PluginChanged;

        private IList<IPluginConfigurator> _configurators = new List<IPluginConfigurator>();
        private IPluginConfigurator _selectedConfigurator;
                
        private bool? _isEnabled;

        public CgElementsControllersViewModel(IConfigEngine engine)
        {
            _engine = engine;
                                    
            foreach (var plugin in ConfigurationPluginManager.Current.CgElementsControllers)
            {
                var configuratorVm = plugin.GetConfiguratorViewModel();
                configuratorVm.PluginChanged += PluginConfigurator_PluginChanged;
                configuratorVm.Initialize(_engine.CGElementsController);
                _configurators.Add(configuratorVm);
            }
            Configurators = CollectionViewSource.GetDefaultView(_configurators);
            SelectedConfigurator = _configurators.FirstOrDefault(p => p.GetModel()?.GetType() == _engine.CGElementsController?.GetType());
        }

        private void PluginConfigurator_PluginChanged(object sender, EventArgs e)
        {
            PluginChanged?.Invoke(this, EventArgs.Empty);
        }
        
        public bool? IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (!SetField(ref _isEnabled, value))
                    return;

                if (value == null)
                    return;

                if (_selectedConfigurator != null)
                    _selectedConfigurator.IsEnabled = (bool)value;
            }
        }

        public string Name => _selectedConfigurator?.PluginName ?? String.Empty;

        public ICGElementsController CgElementsController => (ICGElementsController)_selectedConfigurator?.GetModel();

        public IPluginConfigurator SelectedConfigurator
        {
            get => _selectedConfigurator;
            set
            {
                if (!SetField(ref _selectedConfigurator, value))
                    return;

                _isEnabled = _selectedConfigurator?.IsEnabled ?? false;
                NotifyPropertyChanged(nameof(IsEnabled));
                NotifyPropertyChanged(nameof(Name));
            }
        }
        public ICollectionView Configurators { get; }

        protected override void OnDispose()
        {
            foreach (var cgConfigurator in _configurators)           
                cgConfigurator.PluginChanged -= PluginConfigurator_PluginChanged;            
        }

        public void Save()
        {
            _selectedConfigurator.Save();
        }       
    }
}
