using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Xml.Serialization;
using TAS.Client.Common;
using TAS.Client.Config.Model;

namespace TAS.Client.Config
{
    public class ArchiveDirectoriesViewmodel: OkCancelViewmodelBase<ArchiveDirectories>
    {
        public ICommand CommandAdd { get; private set; }
        public ICommand CommandDelete { get; private set; }
        public ICommand CommandUp { get; private set; }
        public ICommand CommandDown { get; private set; }

        public ArchiveDirectoriesViewmodel() : base(new Model.ArchiveDirectories(), new ArchiveDirectoriesView(), "Archive directories") 
        {
            _directories = new ObservableCollection<ArchiveDirectory>(Model.Directories);
            _createCommands();
        }

        public ArchiveDirectoriesViewmodel(Model.ArchiveDirectories directories) : base(directories, new ArchiveDirectoriesView(), "Archive directories")
        {
            _directories = new ObservableCollection<ArchiveDirectory>(Model.Directories);
            _createCommands();
        }


        private readonly ObservableCollection<ArchiveDirectory> _directories;

        private void _createCommands()
        {
            CommandAdd = new UICommand() { ExecuteDelegate = _add };
            CommandDelete = new UICommand() { ExecuteDelegate = _delete, CanExecuteDelegate = _canDelete };
        }

        private void _delete(object obj)
        {
            SelectedDirectory.Delete();
            Directories.Remove(SelectedDirectory);
            SelectedDirectory = null;
        }

        private bool _canDelete(object obj)
        {
            return SelectedDirectory != null;
        }

        private void _add(object obj)
        {
            var newDir = new ArchiveDirectory();
            Directories.Add(newDir);
            Model.Directories.Add(newDir);
            SelectedDirectory = newDir;
        }

        public ObservableCollection<ArchiveDirectory> Directories { get { return _directories; } }
        ArchiveDirectory _selectedDirectory;
        
        public ArchiveDirectory SelectedDirectory { get { return _selectedDirectory; } set { SetField(ref _selectedDirectory, value); } }
        
        protected override void OnDispose()
        {
        }
        
        public override bool IsModified { get { return Model.Directories.Any(d => d.IsModified || d.IsDeleted | d.IsNew); } }

        public override void ModelUpdate(object parameter)
        {
            Model.Save();
        }

    }
}
