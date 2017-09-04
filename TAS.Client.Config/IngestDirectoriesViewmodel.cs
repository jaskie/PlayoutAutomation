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
        private bool _added;
        private bool _deleted;
        private bool _moved;
        private IngestDirectoryViewmodel _selectedDirectory;


        public IngestDirectoriesViewmodel(string fileName) : base(Deserialize(fileName), typeof(IngestDirectoriesView),
            $"Ingest directories ({System.IO.Path.GetFullPath(fileName)})") 
        {
            foreach (var item in Model.Select(d => new IngestDirectoryViewmodel(d, Directories)))
            {
                item.Load();
                Directories.Add(item);
            }
            _fileName = fileName;
            _createCommands();
        }

        public ICommand CommandAdd { get; private set; }

        public ICommand CommandDelete { get; private set; }

        public ICommand CommandUp { get; private set; }

        public ICommand CommandDown { get; private set; }

        public ICommand CommandAddSub { get; private set; }

        public ObservableCollection<IngestDirectoryViewmodel> Directories { get; } = new ObservableCollection<IngestDirectoryViewmodel>();
        
        public IngestDirectoryViewmodel SelectedDirectory
        {
            get { return _selectedDirectory; }
            set
            {
                if (_selectedDirectory != value)
                {
                    _selectedDirectory = value;
                    NotifyPropertyChanged();
                }
            }
        }
        
        public override bool IsModified { get { return _added || _deleted || _moved|| Directories.Any(d => d.IsModified); } }

        public override void Update(object parameter = null)
        {
            Directories.Where(d => d.IsModified).ToList().ForEach(d => d.Update());
            XmlSerializer writer = new XmlSerializer(typeof(List<IngestDirectory>), new XmlRootAttribute("IngestDirectories"));
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(_fileName))
            {
                writer.Serialize(file, Directories.Select(d => d.Model).ToList());
                file.Close();
            }
        }

        protected override void OnDispose()
        {

        }

        private void _createCommands()
        {
            CommandAdd = new UICommand() { ExecuteDelegate = _add };
            CommandAddSub = new UICommand() { ExecuteDelegate = _addSub, CanExecuteDelegate = _canAddSub };
            CommandDelete = new UICommand() { ExecuteDelegate = _delete, CanExecuteDelegate = _canDelete };
            CommandUp = new UICommand() { ExecuteDelegate = _up, CanExecuteDelegate = _canUp };
            CommandDown = new UICommand() { ExecuteDelegate = _down, CanExecuteDelegate = _canDown };
        }

        private bool _canAddSub(object obj)
        {
            return SelectedDirectory != null;
        }

        private void _addSub(object obj)
        {
            SelectedDirectory = SelectedDirectory.AddSubdirectory();
        }

        private void _delete(object obj)
        {
            if (_deleteDirectory(Directories, _selectedDirectory))
            {
                _deleted = true;
                SelectedDirectory = null;
            }
        }

        private bool _deleteDirectory(ObservableCollection<IngestDirectoryViewmodel> collection, IngestDirectoryViewmodel item)
        {
            if (collection.Contains(item))
            {
                collection.Remove(item);
                return true;
            }
            foreach (var d in collection)
            {
                if (_deleteDirectory(d.SubDirectoriesVM, item))
                    return true;
            }
            return false;
        }

        private bool _canDelete(object obj)
        {
            return SelectedDirectory != null;
        }

        private void _add(object obj)
        {
            var newDir = new IngestDirectoryViewmodel(new IngestDirectory(), Directories) { DirectoryName = Common.Properties.Resources._title_NewDirectory };
            Directories.Add(newDir);
            _added = true;
            SelectedDirectory = newDir;
        }

        private void _up(object o)
        {
            var collection = SelectedDirectory?.OwnerCollection;
            if (collection != null)
            {
                int oldIndex = collection.IndexOf(_selectedDirectory);
                if (oldIndex > 0)
                {
                    collection.Move(oldIndex, oldIndex - 1);
                    _moved = true;
                }
            }
        }

        private bool _canDown(object o)
        {
            var collection = SelectedDirectory?.OwnerCollection;
            if (collection != null)
            {
                int index = collection.IndexOf(_selectedDirectory);
                return index >= 0 && index < collection.Count - 1;
            }
            return false;
        }

        private void _down(object o)
        {
            var collection = SelectedDirectory?.OwnerCollection;
            if (collection != null)
            {
                int oldIndex = collection.IndexOf(_selectedDirectory);
                if (oldIndex < collection.Count - 1)
                {
                    collection.Move(oldIndex, oldIndex + 1);
                    _moved = true;
                }
            }
        }

        private bool _canUp(object o)
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
