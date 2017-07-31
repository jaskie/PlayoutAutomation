using System.IO;
using System.Windows.Input;
using TAS.Client.Common;

namespace TAS.Client.Config
{
    public class MainWindowViewmodel: ViewmodelBase
    {
        private Model.ConfigFile _configFile;

        public MainWindowViewmodel()
        {
            CommandIngestFoldersSetup = new UICommand { ExecuteDelegate = _ingestFoldersSetup };
            CommandConfigFileEdit = new UICommand { ExecuteDelegate = _configFileEdit };
            CommandConfigFileSelect = new UICommand { ExecuteDelegate = _configFileSelect };
            CommandPlayoutServersSetup = new UICommand { ExecuteDelegate = _serversSetup };
            CommandEnginesSetup = new UICommand { ExecuteDelegate = _enginesSetup };
            if (File.Exists("TVPlay.exe"))
                ConfigFile = new Model.ConfigFile("TVPlay.exe");
        }

        public ICommand CommandIngestFoldersSetup { get; }
        public ICommand CommandConfigFileEdit { get; }
        public ICommand CommandConfigFileSelect { get; }
        public ICommand CommandPlayoutServersSetup { get; }
        public ICommand CommandEnginesSetup { get; }

        public Model.ConfigFile ConfigFile
        {
            get { return _configFile; }
            set { SetField(ref _configFile, value); }
        }

        protected override void OnDispose() { }
        
        private void _enginesSetup(object obj)
        {
            using (var vm = new EnginesViewmodel(_configFile.connectionStrings.tasConnectionString,
                _configFile.connectionStrings.tasConnectionStringSecondary))
            {
                vm.Load();
                vm.ShowDialog();
            }
        }
        
        private void _serversSetup(object obj)
        {
            using (var vm = new PlayoutServersViewmodel(_configFile.connectionStrings.tasConnectionString,
                _configFile.connectionStrings.tasConnectionStringSecondary))
            {
                vm.Load();
                vm.ShowDialog();
            }
        }
                
        private void _configFileSelect(object obj)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog() { Filter = "Executables (*.exe)|*.exe" };
            if (dlg.ShowDialog() == true)
                ConfigFile = new Model.ConfigFile(dlg.FileName);
        }

        private void _configFileEdit(object obj)
        {
            ConfigFileViewmodel vm = new ConfigFileViewmodel(_configFile);
            vm.Load();
            vm.ShowDialog();
        }

        private void _ingestFoldersSetup(object obj)
        {
            IngestDirectoriesViewmodel vm = new IngestDirectoriesViewmodel(_configFile.appSettings.IngestFolders);
            vm.Load();
            vm.ShowDialog();
        }

    }
}
