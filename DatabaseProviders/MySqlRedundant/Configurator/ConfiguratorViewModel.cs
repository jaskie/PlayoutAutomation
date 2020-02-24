using System;
using System.Configuration;
using System.Windows;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Client.Config;

namespace TAS.Database.MySqlRedundant.Configurator
{
    public class ConfiguratorViewModel : ViewModelBase
    {
        private string _connectionStringPrimary;
        private string _connectionStringSecondary;
        private bool _isConnectionStringSecondary;
        private MySqlRedundant.DatabaseMySqlRedundant _db = new DatabaseMySqlRedundant();

        public ICommand CommandEditConnectionString { get; }
        public ICommand CommandEditConnectionStringSecondary { get; }
        public ICommand CommandTestConnectivity { get; }
        public ICommand CommandCreateDatabase { get; }
        public ICommand CommandCloneDatabase { get; }
        public ICommand CommandTestConnectivitySecodary { get; }

        internal Window Window;
        private Configuration _configuration;

        public ConfiguratorViewModel()
        {
            CommandEditConnectionString = new UiCommand(EditConnectionString);
            CommandEditConnectionStringSecondary = new UiCommand(EditConnectionStringSecondary);
            CommandTestConnectivity = new UiCommand(TestConnectivity, o => !string.IsNullOrWhiteSpace(ConnectionStringPrimary));
            CommandTestConnectivitySecodary = new UiCommand(TestConnectivitySecondary, o => !string.IsNullOrWhiteSpace(ConnectionStringSecondary) && _isConnectionStringSecondary);
            CommandCreateDatabase = new UiCommand(CreateDatabase, o => !string.IsNullOrWhiteSpace(ConnectionStringPrimary));
            CommandCloneDatabase = new UiCommand(ClonePrimaryDatabase, o => !(string.IsNullOrWhiteSpace(ConnectionStringPrimary) || string.IsNullOrWhiteSpace(ConnectionStringSecondary)));
        }

        public Configuration Configuration
        {
            get => _configuration; 
            set
            {
                if (_configuration == value) return;
                _configuration = value;
                ConnectionStringPrimary = Configuration.ConnectionStrings.ConnectionStrings[ConnectionStringsNames.Primary]?.ConnectionString;
                ConnectionStringSecondary = Configuration.ConnectionStrings.ConnectionStrings[ConnectionStringsNames.Secondary]?.ConnectionString;
                IsSConnectionStringSecondary = !string.IsNullOrWhiteSpace(ConnectionStringSecondary);
            }
        }

        public void Save()
        {
            var section = Configuration.ConnectionStrings;
            SaveConnectionString(section, ConnectionStringsNames.Primary, ConnectionStringPrimary);
            if (IsSConnectionStringSecondary)
                SaveConnectionString(section, ConnectionStringsNames.Secondary, ConnectionStringSecondary);
            else
            {
                var cs = section.ConnectionStrings[ConnectionStringsNames.Secondary];
                if (cs != null)
                    section.ConnectionStrings.Remove(cs);
            }
        }

        public bool IsSConnectionStringSecondary
        {
            get => _isConnectionStringSecondary;
            set => SetField(ref _isConnectionStringSecondary, value);
        }

        public string ConnectionStringPrimary
        {
            get => _connectionStringPrimary;
            set => SetField(ref _connectionStringPrimary, value);
        }

        public string ConnectionStringSecondary
        {
            get => _connectionStringSecondary;
            set => SetField(ref _connectionStringSecondary, value);
        }

        private void TestConnectivity(object obj)
        {
            try
            {
                _db.TestConnect(ConnectionStringPrimary);
                _db.Open(ConnectionStringPrimary, null);
                try
                {
                    if (_db.UpdateRequired())
                    {
                        if (MessageBox.Show(Window, "Connection successful, but database should be updated. \nUpdate now?", "Connection test", MessageBoxButton.YesNo, MessageBoxImage.Question) ==
                            MessageBoxResult.Yes)
                            try
                            {
                                _db.UpdateDb();
                                MessageBox.Show(Window, "Database is now up-to-date.", "Connection test", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            catch (Exception e)
                            {
                                MessageBox.Show(Window, e.ToString(), "Update failed", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                    }
                    else
                        MessageBox.Show(Window, "Connection successful and database is up-to-date.", "Connection test",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                }
                finally
                {
                    _db.Close();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(Window, $"Connection failed:\n{e.Message}", "Connection test", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TestConnectivitySecondary(object obj)
        {
            try
            {
                _db.TestConnect(ConnectionStringSecondary);
                MessageBox.Show(Window, "Connection successful", "Connection test", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception e)
            {
                MessageBox.Show(Window, $"Connection failed:\n{e.Message}", "Connection test", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClonePrimaryDatabase(object obj)
        {
            try
            {
                var databaseExists = false;
                try
                {
                    _db.TestConnect(ConnectionStringSecondary);
                    databaseExists = true;
                }
                catch { }

                if (databaseExists)
                {
                    if (MessageBox.Show(Window, "Secondary database already exists. Delete it first?", "Warning - database exists",
                            MessageBoxButton.YesNo, MessageBoxImage.Hand) != MessageBoxResult.Yes)
                        return;
                    try
                    {
                        _db.DropDatabase(ConnectionStringSecondary);
                    }
                    catch
                    {
                        MessageBox.Show(Window, "Database delete failed, cannot proceed.", "Database clone", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                _db.CloneDatabase(ConnectionStringPrimary, ConnectionStringSecondary);
                MessageBox.Show(Window, "Database clone successful", "Database clone", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception e)
            {
                MessageBox.Show(Window, $"Database clonning failed:\n{e.Message}", "Database clone", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateDatabase(object obj)
        {
            using (var vm = new CreateDatabaseViewmodel(_db) { ConnectionString = ConnectionStringPrimary })
            {
                if (vm.ShowDialog() != true)
                    return;
                if (vm.ConnectionString == ConnectionStringPrimary)
                    MessageBox.Show(Window, "Database created successfully", "Create database", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                else if (MessageBox.Show(Window, "Database created successfully. Use the new database?", "Create database",
                             MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    ConnectionStringPrimary = vm.ConnectionString;
            }
        }


        private void EditConnectionString(object obj)
        {
            using (var vm = new ConnectionStringViewmodel(_connectionStringPrimary))
            {
                if (vm.ShowDialog() == true)
                    ConnectionStringPrimary = vm.Model.ConnectionString;
            }
        }

        private void EditConnectionStringSecondary(object obj)
        {
            using (var vm = new ConnectionStringViewmodel(_connectionStringSecondary))
            {
                if (vm.ShowDialog() == true)
                    ConnectionStringSecondary = vm.Model.ConnectionString;
            }
        }

        private void SaveConnectionString(ConnectionStringsSection section, string name, string value)
        {
            var cs = section.ConnectionStrings[name];
            if (cs == null)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    section.ConnectionStrings.Add(new ConnectionStringSettings(name, value));
            }
            else
            {
                if (string.IsNullOrWhiteSpace(value))
                    section.ConnectionStrings.Remove(cs);
                else
                    cs.ConnectionString = value;
            }
        }

        protected override void OnDispose()
        {

        }
    }
}
