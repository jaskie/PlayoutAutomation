using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Client.Config.Model;

namespace TAS.Client.Config.ViewModels.ArchiveDirectories
{
    public class ArchiveDirectoriesViewmodel : OkCancelViewModelBase
    {
        private Model.ArchiveDirectories _archiveDirectories;
        public ArchiveDirectoriesViewmodel(Model.ArchiveDirectories directories)
        {
            _archiveDirectories = directories;
            Directories = new ObservableCollection<ArchiveDirectory>(_archiveDirectories.Directories);
            _createCommands();
        }

        public ICommand CommandAdd { get; private set; }
        public ICommand CommandDelete { get; private set; }

        private void _createCommands()
        {
            CommandAdd = new UiCommand(_add);
            CommandDelete = new UiCommand(_delete, _canDelete);
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
            _archiveDirectories.Directories.Add(newDir);
            SelectedDirectory = newDir;
        }

        public ObservableCollection<ArchiveDirectory> Directories { get; }

        ArchiveDirectory _selectedDirectory;

        public ArchiveDirectory SelectedDirectory
        {
            get => _selectedDirectory;
            set
            {
                if (_selectedDirectory == value)
                    return;
                _selectedDirectory = value;
                NotifyPropertyChanged();
            }
        }                        
        
        public override bool IsModified { get { return _archiveDirectories.Directories.Any(d => d.IsModified || d.IsDeleted | d.IsNew); } }                       
    }
}
