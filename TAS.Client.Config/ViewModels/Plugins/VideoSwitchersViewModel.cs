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
    public class VideoSwitchersViewModel : ViewModelBase, IPluginManager
    {
        private IConfigEngine _engine;
        public event EventHandler PluginChanged;

        private IList<IPluginConfiguratorViewModel> _configurators = new List<IPluginConfiguratorViewModel>();
        private IPluginConfiguratorViewModel _selectedConfigurator;

        private bool? _isEnabled;

        public VideoSwitchersViewModel(IConfigEngine engine)
        {
            _engine = engine;

            foreach (var plugin in ConfigurationPluginManager.Current.VideoSwitchers)
            {
                var configuratorVm = plugin.GetConfiguratorViewModel();
                configuratorVm.PluginChanged += PluginConfigurator_PluginChanged;
                configuratorVm.Initialize(_engine.Router);
                _configurators.Add(configuratorVm);
            }

            Configurators = CollectionViewSource.GetDefaultView(_configurators);
            SelectedConfigurator = _configurators.FirstOrDefault(p => p.GetModel()?.GetType() == _engine.Router?.GetType()) ?? _configurators.First();              
        }

        private void PluginConfigurator_PluginChanged(object sender, EventArgs e)
        {
            PluginChanged?.Invoke(this, EventArgs.Empty);
        }

        public ICollectionView Configurators { get; }

        public IPluginConfiguratorViewModel SelectedConfigurator
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

        public string Name => _selectedConfigurator?.PluginName;

        public IRouter Router => (IRouter)_selectedConfigurator.GetModel();

        protected override void OnDispose()
        {
            foreach (var routerConfigurator in _configurators)
            {
                routerConfigurator.PluginChanged -= PluginConfigurator_PluginChanged;                
            }
        }

        public void Save()
        {
            _selectedConfigurator.Save();
        }
    }
}
