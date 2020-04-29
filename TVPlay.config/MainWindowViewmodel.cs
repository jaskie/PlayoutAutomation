using System.Configuration;
using System.IO;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Client.Config.ViewModels.ConfigFile;
using TAS.Client.Config.ViewModels.Engines;
using TAS.Client.Config.ViewModels.IngestDirectories;
using TAS.Client.Config.ViewModels.Playout;
using TAS.Client.Config.ViewModels.Plugins;

namespace TAS.Client.Config
{
    public class MainWindowViewmodel: ViewModelBase
    {
        private Model.ConfigFile _configFile;
        
        public MainWindowViewmodel()
        {            
            if (File.Exists("TVPlay.exe"))
                ConfigFile = new Model.ConfigFile(ConfigurationManager.OpenExeConfiguration("TVPlay.exe"));
            CommandIngestFoldersSetup = new UiCommand(_ingestFoldersSetup, _canShowDialog);
            CommandConfigFileEdit = new UiCommand(_configFileEdit, _canShowDialog);
            CommandConfigFileSelect = new UiCommand(_configFileSelect, _canShowDialog);
            CommandPlayoutServersSetup = new UiCommand(_serversSetup, _canShowDialog);
            CommandEnginesSetup = new UiCommand(_enginesSetup, _canShowDialog);
            CommandPluginsSetup = new UiCommand(_pluginsSetup, _canShowDialog);
        }       

        public ICommand CommandIngestFoldersSetup { get; }
        public ICommand CommandConfigFileEdit { get; }
        public ICommand CommandConfigFileSelect { get; }
        public ICommand CommandPlayoutServersSetup { get; }
        public ICommand CommandEnginesSetup { get; }
        public ICommand CommandPluginsSetup { get; }
        public Model.ConfigFile ConfigFile
        {
            get => _configFile;
            set => SetField(ref _configFile, value);
        }

        protected override void OnDispose() { }

        private bool _canShowDialog(object o)
        {
            return _configFile != null;
        }
        
        private void _enginesSetup(object obj)
        {
            using (var vm = new EnginesViewmodel(ConfigFile.AppSettings.DatabaseType, ConfigFile.Configuration.ConnectionStrings.ConnectionStrings))
            {
                UiServices.WindowManager.ShowWindow(vm, "Engines");
            }
        }
        
        private void _serversSetup(object obj)
        {
            using (var vm = new PlayoutServersViewmodel(ConfigFile.AppSettings.DatabaseType, ConfigFile.Configuration.ConnectionStrings.ConnectionStrings))
            {
                UiServices.WindowManager.ShowWindow(vm, "Playout Servers");
            }
        }
                
        private void _configFileSelect(object obj)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog() { Filter = "Executables (*.exe)|*.exe" };
            if (dlg.ShowDialog() == true)
                ConfigFile = new Model.ConfigFile(ConfigurationManager.OpenExeConfiguration(dlg.FileName));
        }

        private void _configFileEdit(object obj)
        {
            ConfigFileViewmodel vm = new ConfigFileViewmodel(_configFile);
            UiServices.WindowManager.ShowWindow(vm, $"Config file ({_configFile.FileName})");
        }

        private void _ingestFoldersSetup(object obj)
        {
            IngestDirectoriesViewmodel vm = new IngestDirectoriesViewmodel(_configFile.AppSettings.IngestFolders);
            UiServices.WindowManager.ShowWindow(vm, $"Ingest directories ({System.IO.Path.GetFullPath(_configFile.AppSettings.IngestFolders)})");
        }

        private void _pluginsSetup(object obj)
        {
            PluginsViewModel vm = new PluginsViewModel(ConfigFile.AppSettings.DatabaseType, ConfigFile.Configuration.ConnectionStrings.ConnectionStrings);
            UiServices.WindowManager.ShowWindow(vm, "Plugins");
        }
    }
}
