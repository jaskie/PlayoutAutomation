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
        public IngestDirectoriesViewmodel(IEnumerable<IIngestDirectory> directories): base(directories, new IngestFoldersView(), "Ingest directories", 500, 300)
        {
            _directories = new ObservableCollection<IngestDirectoryViewmodel>(directories.Select(d => new IngestDirectoryViewmodel(d)));
            _createCommands();
        }

        private void _createCommands()
        {
            CommandAdd = new SimpleCommand() { ExecuteDelegate = _add, CanExecuteDelegate = _canAdd };
            CommandDelete = new SimpleCommand() { ExecuteDelegate = _delete, CanExecuteDelegate = _canDelete };
        }

        private void _delete(object obj)
        {
            throw new NotImplementedException();
        }

        private bool _canDelete(object obj)
        {
            throw new NotImplementedException();
        }

        private bool _canAdd(object obj)
        {
            throw new NotImplementedException();
        }

        private void _add(object obj)
        {
            throw new NotImplementedException();
        }

        public ObservableCollection<IngestDirectoryViewmodel> Directories { get { return _directories; } }
        public IngestDirectoryViewmodel SelectedDirectory { get; set; }

        
        protected override void OnDispose()
        {
            
        }
    }
}
