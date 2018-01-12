using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.Config
{
    public class ConfigFileViewmodel : OkCancelViewmodelBase<Model.ConfigFile>
    {
        private string _ingestFolders;
        private string _localDevices;
        private string _tempDirectory;
        private int _instance;
        private string _tasConnectionString;
        private string _tasConnectionStringSecondary;
        private bool _isBackupInstance;
        private bool _isConnectionStringSecondary;
        private string _uiLanguage;
        private readonly IDatabase _db;

        protected override void OnDispose() { }
        public ConfigFileViewmodel(Model.ConfigFile configFile)
            : base(configFile, typeof(ConfigFileView), $"Config file ({configFile.FileName})")
        {
            _db = DatabaseProviderLoader.LoadDatabaseProvider();
            CommandEditConnectionString = new UICommand { ExecuteDelegate = _editConnectionString };
            CommandEditConnectionStringSecondary = new UICommand { ExecuteDelegate = _editConnectionStringSecondary };
            CommandTestConnectivity = new UICommand { ExecuteDelegate = _testConnectivity, CanExecuteDelegate = o => !string.IsNullOrWhiteSpace(tasConnectionString) };
            CommandTestConnectivitySecodary = new UICommand { ExecuteDelegate = _testConnectivitySecondary, CanExecuteDelegate = o => !string.IsNullOrWhiteSpace(tasConnectionStringSecondary) && _isConnectionStringSecondary };
            CommandCreateDatabase = new UICommand { ExecuteDelegate = _createDatabase, CanExecuteDelegate = o => !string.IsNullOrWhiteSpace(tasConnectionString) };
            CommandCloneDatabase = new UICommand { ExecuteDelegate = _clonePrimaryDatabase, CanExecuteDelegate = o => !(string.IsNullOrWhiteSpace(tasConnectionString) || string.IsNullOrWhiteSpace(tasConnectionStringSecondary)) };
        }

        public string IngestFolders { get { return _ingestFolders; } set { SetField(ref _ingestFolders, value); } }

        public string LocalDevices { get { return _localDevices; } set { SetField(ref _localDevices, value); } }

        public string TempDirectory { get { return _tempDirectory; } set { SetField(ref _tempDirectory, value); } }

        public int Instance { get { return _instance; } set { SetField(ref _instance, value); } }

        public string tasConnectionString { get { return _tasConnectionString; } set { SetField(ref _tasConnectionString, value); } }

        public string tasConnectionStringSecondary { get { return _tasConnectionStringSecondary; } set { SetField(ref _tasConnectionStringSecondary, value); } }

        public bool IsBackupInstance { get { return _isBackupInstance; } set { SetField(ref _isBackupInstance, value); } }

        public bool IsSConnectionStringSecondary { get { return _isConnectionStringSecondary; } set { SetField(ref _isConnectionStringSecondary, value); } }

        public string UiLanguage { get { return _uiLanguage; } set { SetField(ref _uiLanguage, value); } }

        public string ExeDirectory => Path.GetDirectoryName(Model.FileName);


        private void _createDatabase(object obj)
        {
            using (var vm = new CreateDatabaseViewmodel {ConnectionString = tasConnectionString})
            {
                vm.Load();
                if (vm.ShowDialog() != true)
                    return;
                if (vm.ConnectionString == tasConnectionString)
                    vm.ShowMessage("Database created successfully", "Create database", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                else if (vm.ShowMessage("Database created successfully. Use the new database?", "Create database",
                             MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    tasConnectionString = vm.ConnectionString;
            }
        }

        public override void Load(object source = null)
        {
            base.Load(Model.appSettings);
            base.Load(Model.connectionStrings);
            _isConnectionStringSecondary = !string.IsNullOrWhiteSpace(tasConnectionStringSecondary);
        }

        public override void Update(object destObject = null)
        {
            base.Update(Model.appSettings);
            if (!_isConnectionStringSecondary)
                tasConnectionStringSecondary = string.Empty;
            base.Update(Model.connectionStrings);
            Model.Save();
        }

        public ICommand CommandEditConnectionString { get; }
        public ICommand CommandEditConnectionStringSecondary { get; }
        public ICommand CommandTestConnectivity { get; }
        public ICommand CommandCreateDatabase { get; }
        public ICommand CommandCloneDatabase { get; }
        public ICommand CommandTestConnectivitySecodary { get; }
        public List<CultureInfo> SupportedLanguages { get; } = new List<CultureInfo> { CultureInfo.InvariantCulture, new CultureInfo("en"), new CultureInfo("pl") };

        private void _editConnectionString(object obj)
        {
            using (var vm = new ConnectionStringViewmodel(_tasConnectionString))
            {
                vm.Load();
                if (vm.ShowDialog() == true)
                    tasConnectionString = vm.Model.ConnectionString;
            }
        }
    
        private void _editConnectionStringSecondary(object obj)
        {
            using (var vm = new ConnectionStringViewmodel(_tasConnectionStringSecondary))
            {
                vm.Load();
                if (vm.ShowDialog() == true)
                    tasConnectionStringSecondary = vm.Model.ConnectionString;
            }
        }

        private void _testConnectivity(object obj)
        {
            if (_db.TestConnect(tasConnectionString))
            {
                _db.Open(tasConnectionString, tasConnectionStringSecondary);
                if (_db.UpdateRequired())
                {
                    if (ShowMessage("Connection successful, but database should be updated. \nUpdate now?", "Connection test", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        if (_db.UpdateDb())
                            ShowMessage("Database is now up-to-date.", "Connection test", MessageBoxButton.OK, MessageBoxImage.Information);
                        else 
                            ShowMessage("Database update failed.", "Connection test", MessageBoxButton.OK, MessageBoxImage.Error);

                }
                else
                    ShowMessage("Connection successful and database is up-to-date.", "Connection test", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
                ShowMessage("Connection failed", "Connection test", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void _testConnectivitySecondary(object obj)
        {
            if (_db.TestConnect(tasConnectionStringSecondary))
                ShowMessage("Connection successful", "Connection test", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                ShowMessage("Connection failed", "Connection test", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void _clonePrimaryDatabase(object obj)
        {
            if (_db.TestConnect(tasConnectionStringSecondary))
            {
                if (ShowMessage("Secondary database already exists. Delete it first?", "Warning - database exists", MessageBoxButton.YesNo, MessageBoxImage.Hand) != MessageBoxResult.Yes)
                    return;
                if (!_db.DropDatabase(tasConnectionStringSecondary))
                {
                    ShowMessage("Database delete failed, cannot proceed.", "Database clone", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            if (_db.CloneDatabase(tasConnectionString, tasConnectionStringSecondary))
                ShowMessage("Database clone successful", "Database clone", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                ShowMessage("Database clonning failed", "Database clone", MessageBoxButton.OK, MessageBoxImage.Error);
        }

    }
}
