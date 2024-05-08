using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using TAS.Client.Common;
using TAS.Client.Config.Model;

namespace TAS.Client.Config
{
    public class ArchiveDirectoriesViewmodel: OkCancelViewmodelBase<ArchiveDirectories>
    {
        ArchiveDirectory _selectedDirectory;

        public ArchiveDirectoriesViewmodel(ArchiveDirectories directories) : base(directories, typeof(ArchiveDirectoriesView), "Archive directories")
        {
            Directories = new ObservableCollection<ArchiveDirectory>(Model.Directories);
            CommandAdd = new UiCommand(CommandName(nameof(Add)), Add);
            CommandDelete = new UiCommand(CommandName(nameof(Delete)), Delete, CanDelete);
        }

        public ICommand CommandAdd { get; }
        public ICommand CommandDelete { get; }

        private void Delete(object _)
        {
            SelectedDirectory.Delete();
            Directories.Remove(SelectedDirectory);
            SelectedDirectory = null;
        }

        private bool CanDelete(object _)
        {
            return SelectedDirectory != null;
        }

        private void Add(object _)
        {
            var newDir = new ArchiveDirectory();
            Directories.Add(newDir);
            Model.Directories.Add(newDir);
            SelectedDirectory = newDir;
        }

        public ObservableCollection<ArchiveDirectory> Directories { get; }

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

        protected override void OnDispose() { }

        public override bool IsModified { get { return Model.Directories.Any(d => d.IsModified || d.IsDeleted | d.IsNew); } }

        protected override void Update(object parameter = null)
        {
            Model.Save();
        }

    }
}
