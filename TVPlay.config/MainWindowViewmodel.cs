using System.Configuration;
using System.IO;
using System.Windows.Input;
using TAS.Client.Common;

namespace TAS.Client.Config
{
    public class MainWindowViewmodel: ViewModelBase
    {
        private Model.ConfigFile _configFile;

        public MainWindowViewmodel()
        {
            if (File.Exists("TVPlay.exe"))
                ConfigFile = new Model.ConfigFile(ConfigurationManager.OpenExeConfiguration("TVPlay.exe"));
            CommandConfigFileSelect = new UiCommand(CommandName(nameof(ConfigFileSelect)), ConfigFileSelect);
            CommandIngestFoldersSetup = new UiCommand(CommandName(nameof(IngestFoldersSetup)), IngestFoldersSetup, CanEditWithConfigFile);
            CommandConfigFileEdit = new UiCommand(CommandName(nameof(ConfigFileEdit)), ConfigFileEdit, CanEditWithConfigFile);
            CommandPlayoutServersSetup = new UiCommand(CommandName(nameof(ServersSetup)), ServersSetup, CanEditWithDatabase);
            CommandEnginesSetup = new UiCommand(CommandName(nameof(EnginesSetup)), EnginesSetup, CanEditWithDatabase);
        }

        public ICommand CommandIngestFoldersSetup { get; }
        public ICommand CommandConfigFileEdit { get; }
        public ICommand CommandConfigFileSelect { get; }
        public ICommand CommandPlayoutServersSetup { get; }
        public ICommand CommandEnginesSetup { get; }

        public Model.ConfigFile ConfigFile
        {
            get => _configFile;
            set => SetField(ref _configFile, value);
        }

        protected override void OnDispose() { }

        private bool CanEditWithConfigFile(object _)
        {
            return _configFile != null;
        }

        private void EnginesSetup(object _)
        {
            if (ConfigFile.AppSettings.DatabaseType == null)
                return;
            using (var vm = new EnginesViewmodel(ConfigFile.AppSettings.DatabaseType.Value, ConfigFile.Configuration.ConnectionStrings.ConnectionStrings))
            {
                vm.ShowDialog();
            }
        }

        private bool CanEditWithDatabase(object _)
        {
            return ConfigFile?.AppSettings.DatabaseType != null;
        }

        private void ServersSetup(object _)
        {
            if (ConfigFile.AppSettings.DatabaseType == null)
                return;
            using (var vm = new PlayoutServersViewmodel(ConfigFile.AppSettings.DatabaseType.Value, ConfigFile.Configuration.ConnectionStrings.ConnectionStrings))
            {
                vm.ShowDialog();
            }
        }

        private void ConfigFileSelect(object _)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog() { Filter = "Executables (*.exe)|*.exe" };
            if (dlg.ShowDialog() == true)
                ConfigFile = new Model.ConfigFile(ConfigurationManager.OpenExeConfiguration(dlg.FileName));
        }

        private void ConfigFileEdit(object _)
        {
            ConfigFileViewmodel vm = new ConfigFileViewmodel(_configFile);
            vm.ShowDialog();
        }

        private void IngestFoldersSetup(object _)
        {
            IngestDirectoriesViewmodel vm = new IngestDirectoriesViewmodel(_configFile.AppSettings.IngestFolders);
            vm.ShowDialog();
        }

    }
}
