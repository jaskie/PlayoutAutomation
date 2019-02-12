using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common;
using TAS.Common.Database;
using TAS.Common.Database.Interfaces;

namespace TAS.Client.Config
{
    public class ConfigFileViewmodel : OkCancelViewmodelBase<Model.ConfigFile>
    {
        private string _ingestFolders;
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
            CommandEditConnectionString = new UiCommand(_editConnectionString);
            CommandEditConnectionStringSecondary = new UiCommand(_editConnectionStringSecondary);
            CommandTestConnectivity = new UiCommand(_testConnectivity, o => !string.IsNullOrWhiteSpace(tasConnectionString));
            CommandTestConnectivitySecodary = new UiCommand(_testConnectivitySecondary, o => !string.IsNullOrWhiteSpace(tasConnectionStringSecondary) && _isConnectionStringSecondary);
            CommandCreateDatabase = new UiCommand(_createDatabase, o => !string.IsNullOrWhiteSpace(tasConnectionString));
            CommandCloneDatabase = new UiCommand(_clonePrimaryDatabase, o => !(string.IsNullOrWhiteSpace(tasConnectionString) || string.IsNullOrWhiteSpace(tasConnectionStringSecondary)));
            Load(Model.AppSettings);
            Load(Model.ConnectionStrings);
            _isConnectionStringSecondary = !string.IsNullOrWhiteSpace(tasConnectionStringSecondary);
        }

        public string IngestFolders
        {
            get => _ingestFolders;
            set => SetField(ref _ingestFolders, value);
        }

        public string TempDirectory
        {
            get => _tempDirectory;
            set => SetField(ref _tempDirectory, value);
        }

        public int Instance
        {
            get => _instance;
            set => SetField(ref _instance, value);
        }

        public string tasConnectionString
        {
            get => _tasConnectionString;
            set => SetField(ref _tasConnectionString, value);
        }

        public string tasConnectionStringSecondary
        {
            get => _tasConnectionStringSecondary;
            set => SetField(ref _tasConnectionStringSecondary, value);
        }

        public bool IsBackupInstance
        {
            get => _isBackupInstance;
            set => SetField(ref _isBackupInstance, value);
        }

        public bool IsSConnectionStringSecondary
        {
            get => _isConnectionStringSecondary;
            set => SetField(ref _isConnectionStringSecondary, value);
        }

        public string UiLanguage
        {
            get => _uiLanguage;
            set => SetField(ref _uiLanguage, value);
        }

        public string ExeDirectory => Path.GetDirectoryName(Model.FileName);


        private void _createDatabase(object obj)
        {
            using (var vm = new CreateDatabaseViewmodel {ConnectionString = tasConnectionString})
            {
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

        protected override void Update(object destObject = null)
        {
            base.Update(Model.AppSettings);
            if (!_isConnectionStringSecondary)
                tasConnectionStringSecondary = string.Empty;
            base.Update(Model.ConnectionStrings);
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
                if (vm.ShowDialog() == true)
                    tasConnectionString = vm.Model.ConnectionString;
            }
        }
    
        private void _editConnectionStringSecondary(object obj)
        {
            using (var vm = new ConnectionStringViewmodel(_tasConnectionStringSecondary))
            {
                if (vm.ShowDialog() == true)
                    tasConnectionStringSecondary = vm.Model.ConnectionString;
            }
        }

        private void _testConnectivity(object obj)
        {
            try
            {
                _db.TestConnect(tasConnectionString);
                _db.Open(tasConnectionString, tasConnectionStringSecondary);
                if (_db.UpdateRequired())
                {
                    if (ShowMessage("Connection successful, but database should be updated. \nUpdate now?",
                            "Connection test", MessageBoxButton.YesNo, MessageBoxImage.Question) ==
                        MessageBoxResult.Yes)
                        if (_db.UpdateDb())
                            ShowMessage("Database is now up-to-date.", "Connection test", MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        else
                            ShowMessage("Database update failed.", "Connection test", MessageBoxButton.OK,
                                MessageBoxImage.Error);

                }
                else
                    ShowMessage("Connection successful and database is up-to-date.", "Connection test",
                        MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception e)
            {
                ShowMessage($"Connection failed:\n{e.Message}", "Connection test", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void _testConnectivitySecondary(object obj)
        {
            try
            {
                _db.TestConnect(tasConnectionStringSecondary);
                ShowMessage("Connection successful", "Connection test", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception e)
            {
                ShowMessage($"Connection failed:\n{e.Message}", "Connection test", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void _clonePrimaryDatabase(object obj)
        {
            try
            {
                var databaseExists = false;
                try
                {
                    _db.TestConnect(tasConnectionStringSecondary);
                    databaseExists = true;
                }
                catch { }

                if (databaseExists)
                {
                    if (ShowMessage("Secondary database already exists. Delete it first?", "Warning - database exists",
                            MessageBoxButton.YesNo, MessageBoxImage.Hand) != MessageBoxResult.Yes)
                        return;
                    if (!_db.DropDatabase(tasConnectionStringSecondary))
                    {
                        ShowMessage("Database delete failed, cannot proceed.", "Database clone", MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }
                }
                _db.CloneDatabase(tasConnectionString, tasConnectionStringSecondary);
                ShowMessage("Database clone successful", "Database clone", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception e)
            {
                ShowMessage($"Database clonning failed:\n{e.Message}", "Database clone", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
