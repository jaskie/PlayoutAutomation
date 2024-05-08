using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Xml.Serialization;
using TAS.Client.Common;
using TAS.Client.Config.Model;

namespace TAS.Client.Config
{
    public class IngestDirectoriesViewmodel: OkCancelViewmodelBase<IEnumerable<IngestDirectory>>
    {
        private readonly string _fileName;
        private IngestDirectoryViewmodel _selectedDirectory;


        public IngestDirectoriesViewmodel(string fileName) : base(Deserialize(fileName), typeof(IngestDirectoriesView),
            $"Ingest directories ({System.IO.Path.GetFullPath(fileName)})") 
        {
            foreach (var item in Model.Select(d => new IngestDirectoryViewmodel(d, this)))
            {
                Directories.Add(item);
            }
            _fileName = fileName;

            CommandAdd = new UiCommand(CommandName(nameof(Add)), Add);
            CommandAddSub = new UiCommand(CommandName(nameof(AddSub)), AddSub, CanAddSub);
            CommandDelete = new UiCommand(CommandName(nameof(Delete)), Delete, CanDelete);
            CommandUp = new UiCommand(CommandName(nameof(Up)), Up, CanUp);
            CommandDown = new UiCommand(CommandName(nameof(Down)), Down, CanDown);
        }

        public ICommand CommandAdd { get; }

        public ICommand CommandDelete { get; }

        public ICommand CommandUp { get; }

        public ICommand CommandDown { get; }

        public ICommand CommandAddSub { get; }

        public ObservableCollection<IngestDirectoryViewmodel> Directories { get; } = new ObservableCollection<IngestDirectoryViewmodel>();
        
        public IngestDirectoryViewmodel SelectedDirectory
        {
            get => _selectedDirectory;
            set
            {
                if (_selectedDirectory == value)
                    return;
                _selectedDirectory = value;
                NotifyPropertyChanged();
                InvalidateRequerySuggested();
            }
        }
        
        public override bool IsModified { get { return base.IsModified || Directories.Any(d => d.IsModified); } }

        protected override void Update(object parameter = null)
        {
            Directories.ToList().ForEach(d => d.SaveToModel());
            var writer = new XmlSerializer(typeof(List<IngestDirectory>), new XmlRootAttribute("IngestDirectories"));
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(_fileName))
            {
                writer.Serialize(file, Directories.Select(d => d.Model).ToList());
            }
        }

        protected override void OnDispose() { }

        private bool CanAddSub(object obj)
        {
            return SelectedDirectory != null;
        }

        private void AddSub(object obj)
        {
            SelectedDirectory = SelectedDirectory.AddSubdirectory();
        }

        private void Delete(object obj)
        {
            if (!DeleteDirectory(_selectedDirectory))
                return;
            IsModified = true;
            SelectedDirectory = Directories.FirstOrDefault();
        }

        private bool DeleteDirectory(IngestDirectoryViewmodel item)
        {
            var collection = item.OwnerCollection;
            if (!collection.Contains(item))
                return false;
            collection.Remove(item);
            return true;
        }

        private bool CanDelete(object obj)
        {
            return SelectedDirectory != null;
        }

        private void Add(object obj)
        {
            var newDir = new IngestDirectoryViewmodel(new IngestDirectory(), this) { DirectoryName = Common.Properties.Resources._title_NewDirectory };
            Directories.Add(newDir);
            IsModified = true;
            SelectedDirectory = newDir;
        }

        private void Up(object o)
        {
            var collection = SelectedDirectory?.OwnerCollection;
            if (collection != null)
            {
                int oldIndex = collection.IndexOf(_selectedDirectory);
                if (oldIndex > 0)
                {
                    collection.Move(oldIndex, oldIndex - 1);
                    IsModified = true;
                }
            }
        }

        private bool CanDown(object o)
        {
            var collection = SelectedDirectory?.OwnerCollection;
            if (collection != null)
            {
                int index = collection.IndexOf(_selectedDirectory);
                return index >= 0 && index < collection.Count - 1;
            }
            return false;
        }

        private void Down(object o)
        {
            var collection = SelectedDirectory?.OwnerCollection;
            if (collection != null)
            {
                int oldIndex = collection.IndexOf(_selectedDirectory);
                if (oldIndex < collection.Count - 1)
                {
                    collection.Move(oldIndex, oldIndex + 1);
                    IsModified = true;
                }
            }
        }

        private bool CanUp(object o)
        {
            var collection = SelectedDirectory?.OwnerCollection;
            return collection?.IndexOf(_selectedDirectory) > 0;
        }

        private static IEnumerable<IngestDirectory> Deserialize(string fileName)
        {
            try
            {
                XmlSerializer reader = new XmlSerializer(typeof(List<IngestDirectory>), new XmlRootAttribute("IngestDirectories"));
                System.IO.StreamReader file = new System.IO.StreamReader(fileName);
                try
                {
                    return (IEnumerable<IngestDirectory>)reader.Deserialize(file);
                }
                finally
                {
                    file.Close();
                }
            }
            catch (NullReferenceException)
            {
                return new List<IngestDirectory>();
            }
        }

    }
}
