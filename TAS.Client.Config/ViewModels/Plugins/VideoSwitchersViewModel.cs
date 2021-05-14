using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using TAS.Common.Interfaces;
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


            Configurators = new List<IPluginConfiguratorViewModel> { new EmptyPluginConfiguratorViewModel() };
            Name = "Video switchers";
            Configurators.AddRange(ConfigurationPluginManager.Current.ConfigurationProviders.Select(p => p.GetConfiguratorViewModel(engine)));
            //_selectedConfigurator = _configurators.FirstOrDefault(p => p.GetModel()?.GetType() == _engine.Router?.GetType()) ?? _configurators.First();
        }

        private void PluginConfigurator_PluginChanged(object sender, EventArgs e)
        {
            RaisePluginChanged();
        }

        public List<IPluginConfiguratorViewModel> Configurators { get; }

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

    internal class EmptyPluginConfiguratorViewModel : IPluginConfiguratorViewModel
    {
        public string PluginName { get; } = Common.Properties.Resources._none_;

        public event EventHandler PluginChanged;

        public void Dispose() { }

        public void Initialize(IPlugin model)
        {
            throw new NotImplementedException();
        }

        public void Save() { }

        public void Undo() { }


        public override string ToString()
        {
            return "You can select a plugin from list above";
        }
                
    }
}
