using System;
using System.Collections.Generic;
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
        }

        private void _configFileEdit(object obj)
        {
            throw new NotImplementedException();
        }
        protected override void OnDispose() { }
        
        private void _ingestFoldersSetup(object obj)
        {
            IngestDirectoriesViewmodel vm = new IngestDirectoriesViewmodel(null, null);
            vm.Show();
        }

        readonly UICommand _commandIngestFoldersSetup;
        readonly UICommand _commandConfigFileEdit;
        public ICommand CommandIngestFoldersSetup { get { return _commandIngestFoldersSetup; } }
        public ICommand CommandConfigFileEdit { get { return _commandConfigFileEdit; } }
        string _configFile;
        public string ConfigFile
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
