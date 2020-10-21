using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;
using TAS.Common.Interfaces.Configurator;
using TAS.Database.Common.Interfaces;

namespace TAS.Client.Config.ViewModels.Plugins
{
    internal class VideoSwitchersViewModel : PluginTypeViewModelBase
    {
        private IConfigEngine _engine;

        private IList<IPluginConfiguratorViewModel> _configurators = new List<IPluginConfiguratorViewModel>();
        private IPluginConfiguratorViewModel _selectedConfigurator;

        public VideoSwitchersViewModel(IConfigEngine engine)
        {
            _engine = engine;

            Configurators = CollectionViewSource.GetDefaultView(_configurators);
            Name = "Video switchers";
            //SelectedConfigurator = _configurators.FirstOrDefault(p => p.GetModel()?.GetType() == _engine.Router?.GetType()) ?? _configurators.First();              
        }

        private void PluginConfigurator_PluginChanged(object sender, EventArgs e)
        {
            RaisePluginChanged();
        }

        public ICollectionView Configurators { get; }

        public IPluginConfiguratorViewModel SelectedConfigurator
        {
            get => _selectedConfigurator;
            set => SetField(ref _selectedConfigurator, value);
        }

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
