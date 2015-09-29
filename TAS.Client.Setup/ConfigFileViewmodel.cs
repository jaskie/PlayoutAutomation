using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Server.Common;

namespace TAS.Client.Setup
{
    public class ConfigFileViewmodel:OkCancelViewmodelBase<Model.ConfigFile>
    {

        protected override void OnDispose() { }
        public ConfigFileViewmodel(Model.ConfigFile configFile)
            : base(configFile, new ConfigFileView(), string.Format("Config file ({0})", configFile.FileName))
        {
            _commandEditConnectionString = new UICommand() { ExecuteDelegate = _editConnectionString };
            _commandTestConnectivity = new UICommand() { ExecuteDelegate = _testConnectivity, CanExecuteDelegate = o => !string.IsNullOrWhiteSpace(tasConnectionString) };
            _commandCreateDatabase = new UICommand() { ExecuteDelegate = _createDatabase, CanExecuteDelegate = o => !string.IsNullOrWhiteSpace(tasConnectionString) };
        }

        private void _createDatabase(object obj)
        {
            var vm = new CreateDatabaseViewmodel();
            vm.ConnectionString = this.tasConnectionString;
            if (vm.Show() == true)
                if (vm.ConnectionString == this.tasConnectionString)
                    MessageBox.Show(Window.GetWindow(View), "Database created successfully", "Create database", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    if (MessageBox.Show(Window.GetWindow(View), "Database created successfully. Use the new database?", "Create database", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        this.tasConnectionString = vm.ConnectionString;
        }

        protected override void Load(object source)
        {
            base.Load(Model.appSettings);
            base.Load(Model.connectionStrings);
        }

        protected override void Apply(object parameter)
        {
            base.Apply(Model.appSettings);
            base.Apply(Model.connectionStrings);
            Model.Save();
        }

        readonly UICommand _commandEditConnectionString;
        public ICommand CommandEditConnectionString { get { return _commandEditConnectionString; } }
        readonly UICommand _commandTestConnectivity;
        public ICommand CommandTestConnectivity { get { return _commandTestConnectivity; } }
        readonly UICommand _commandCreateDatabase;
        public ICommand CommandCreateDatabase { get { return _commandCreateDatabase; } }

        private void _editConnectionString(object obj)
        {
            var vm = new ConnectionStringViewmodel(_tasConnectionString);
            if (vm.Show() == true)
                tasConnectionString = vm.ConnectionString;
        }

        private void _testConnectivity(object obj)
        {
            if (Database.TestConnect(tasConnectionString))
                MessageBox.Show(Window.GetWindow(View), "Connection successful", "Connection test", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show(Window.GetWindow(View), "Connection failed", "Connection test", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        string _ingestFolders;
        public string IngestFolders { get { return _ingestFolders; } set { SetField(ref _ingestFolders, value, "IngestFolders"); } }
        string _localDevices;
        public string LocalDevices { get { return _localDevices; } set { SetField(ref _localDevices, value, "LocalDevices"); } }
        string _tempDirectory;
        public string TempDirectory { get { return _tempDirectory; } set { SetField(ref _tempDirectory, value, "TempDirectory"); } }
        double _volumeReferenceLoudness;
        public double VolumeReferenceLoudness { get { return _volumeReferenceLoudness; } set { SetField(ref _volumeReferenceLoudness, value, "VolumeReferenceLoudness"); } }
        int _instance;
        public int Instance { get { return _instance; } set { SetField(ref _instance, value, "Instance"); } }
        string _tasConnectionString;
        public string tasConnectionString { get { return _tasConnectionString; } set { SetField(ref _tasConnectionString, value, "tasConnectionString"); } }

        public string ExeDirectory { get { return Path.GetDirectoryName(Model.FileName); } }

    }
}
