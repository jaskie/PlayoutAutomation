using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Xml.Serialization;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.Setup
{
    public class IngestDirectoriesViewmodel: OkCancelViewmodelBase<IEnumerable<IIngestDirectory>>
    {
        public ICommand CommandAdd { get; private set; }
        public ICommand CommandDelete { get; private set; }
        private readonly ObservableCollection<IngestDirectoryViewmodel> _directories;
        public IngestDirectoriesViewmodel(IEnumerable<IIngestDirectory> directories): base(directories, new IngestFoldersView(), "Ingest directories", 600, 500)
        {
            _directories = new ObservableCollection<IngestDirectoryViewmodel>(directories.Select(d => new IngestDirectoryViewmodel(d)));
            _createCommands();
        }

        private void _createCommands()
        {
            CommandAdd = new UICommand() { ExecuteDelegate = _add };
            CommandDelete = new UICommand() { ExecuteDelegate = _delete, CanExecuteDelegate = _canDelete };
        }

        private void _delete(object obj)
        {
            Directories.Remove(SelectedDirectory);
            _deleted = true;
            SelectedDirectory = null;
        }

        private bool _canDelete(object obj)
        {
            return SelectedDirectory != null;
        }

        private void _add(object obj)
        {
            var newDir = new IngestDirectoryViewmodel() { DirectoryName = Properties.Resources._title_NewDirectory };
            _directories.Add(newDir);
            _added = true;
            SelectedDirectory = newDir;
        }

        public ObservableCollection<IngestDirectoryViewmodel> Directories { get { return _directories; } }
        IngestDirectoryViewmodel _selectedDirectory;
        
        private bool _added;
        private bool _deleted;

        public IngestDirectoryViewmodel SelectedDirectory { get { return _selectedDirectory; } set { SetField(ref _selectedDirectory, value, "SelectedDirectory"); } }

        
        protected override void OnDispose()
        {
            
        }
        
        public override bool Modified { get { return _added || _deleted || _directories.Any(d => d.Modified); } }

        protected override void Apply(object parameter)
        {
            XmlSerializer writer = new XmlSerializer(typeof(List<IngestDirectoryViewmodel>), new XmlRootAttribute("IngestDirectories"));
            System.IO.StreamWriter file = new System.IO.StreamWriter(ConfigurationManager.AppSettings["IngestFolders"]);
            writer.Serialize(file, _directories.ToList());
            file.Close();
            base.Apply(parameter);
        }

    }
}
