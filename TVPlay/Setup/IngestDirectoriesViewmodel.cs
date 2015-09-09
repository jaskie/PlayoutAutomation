using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
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
            View.ShowDialog();
        }

        private void _createCommands()
        {
            CommandAdd = new SimpleCommand() { ExecuteDelegate = _add };
            CommandDelete = new SimpleCommand() { ExecuteDelegate = _delete, CanExecuteDelegate = _canDelete };
        }

        private void _delete(object obj)
        {
            throw new NotImplementedException();
        }

        private bool _canDelete(object obj)
        {
            return SelectedDirectory != null;
        }

        private void _add(object obj)
        {
            throw new NotImplementedException();
        }

        public ObservableCollection<IngestDirectoryViewmodel> Directories { get { return _directories; } }
        IngestDirectoryViewmodel _selectedDirectory;
        public IngestDirectoryViewmodel SelectedDirectory { get { return _selectedDirectory; } set { SetField(ref _selectedDirectory, value, "SelectedDirectory"); } }

        
        protected override void OnDispose()
        {
            
        }
    }
}
