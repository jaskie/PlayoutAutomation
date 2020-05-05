using System;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Windows.Controls;
using TAS.Common;
using TAS.Database.Common.Interfaces;
using TAS.Database.MySqlRedundant.Configurator;

namespace TAS.Database.MySqlRedundant
{
    [Export(typeof(IDatabaseConfigurator))]
    public class MySqlRedundantConfigurator : IDatabaseConfigurator
    {
        private ConfiguratorViewModel _configuratorViewModel = new ConfiguratorViewModel();

        public MySqlRedundantConfigurator()
        {
            View = new Configurator.ConfiguratorView() { DataContext = _configuratorViewModel };
            _configuratorViewModel.PropertyChanged += ConfiguratorViewModel_PropertyChanged;
        }

        public DatabaseType DatabaseType => DatabaseType.MySQL;

        public UserControl View { get; } 

        public void Open(Configuration configuration)
        {
            _configuratorViewModel.Configuration = configuration;
        }

        public void Save()
        {
            _configuratorViewModel.Save();
        }

        public event EventHandler Modified;

        private void ConfiguratorViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_configuratorViewModel.Configuration == null)
                return;
            if (e.PropertyName == nameof(MySqlRedundant.Configurator.ConfiguratorViewModel.ConnectionStringPrimary)
                || e.PropertyName == nameof(ConfiguratorViewModel.ConnectionStringSecondary)
                || e.PropertyName == nameof(ConfiguratorViewModel.IsSConnectionStringSecondary))
                Modified?.Invoke(this, EventArgs.Empty);
        }

    }
}
