using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Client.ViewModels;

namespace TAS.Client.Setup
{
    public class MainWindowViewmodel: ViewmodelBase
    {
        public MainWindowViewmodel()
        {
            _commandIngestFoldersSetup = new UICommand() { ExecuteDelegate = _ingestFoldersSetup };
            _commandConfigFileEdit = new UICommand() { ExecuteDelegate = _configFileEdit };
            _commandConfigFileSelect = new UICommand() { ExecuteDelegate = _configFileSelect };

            if (File.Exists("TVPlay.exe"))
                ConfigFile = new Model.ConfigFile("TVPlay.exe");
        }

        protected override void OnDispose() { }

        private void _configFileSelect(object obj)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog() { Filter = "Executables (*.exe)|*.exe" };
            if (dlg.ShowDialog() == true)
                ConfigFile = new Model.ConfigFile(dlg.FileName);
        }

        private void _configFileEdit(object obj)
        {
            ConfigFileViewmodel vm = new ConfigFileViewmodel(_configFile);
            vm.Show();
        }

        private void _ingestFoldersSetup(object obj)
        {
            IngestDirectoriesViewmodel vm = new IngestDirectoriesViewmodel(_configFile.appSettings.IngestFolders);
            vm.Show();
        }

        readonly UICommand _commandIngestFoldersSetup;
        readonly UICommand _commandConfigFileEdit;
        readonly UICommand _commandConfigFileSelect;
        public ICommand CommandIngestFoldersSetup { get { return _commandIngestFoldersSetup; } }
        public ICommand CommandConfigFileEdit { get { return _commandConfigFileEdit; } }
        public ICommand CommandConfigFileSelect { get { return _commandConfigFileSelect; } }

        Model.ConfigFile _configFile;
        public Model.ConfigFile ConfigFile
        {
            get { return _configFile; }
            set
            {
                if (SetField(ref _configFile, value, "ConfigFile"))
                {

                }
            }
        }
    }
}
