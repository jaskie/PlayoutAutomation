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
using TAS.Server.Interfaces;

namespace TAS.Client.Config
{
    public class IngestDirectoriesViewmodel: OkCancelViewmodelBase<IEnumerable<IngestDirectory>>
    {
        public ICommand CommandAdd { get; private set; }
        public ICommand CommandDelete { get; private set; }
        public ICommand CommandUp { get; private set; }
        public ICommand CommandDown { get; private set; }
        public ICommand CommandAddSub { get; private set; }
        private readonly ObservableCollection<IngestDirectoryViewmodel> _directories;
        private readonly string _fileName;

        public IngestDirectoriesViewmodel(string fileName) : base(Deserialize(fileName), new IngestDirectoriesView(), string.Format("Ingest directories ({0})", System.IO.Path.GetFullPath(fileName))) 
        {
            _directories = new ObservableCollection<IngestDirectoryViewmodel>();
            foreach (var item in Model.Select(d => new IngestDirectoryViewmodel(d, _directories)))
                _directories.Add(item);
            _fileName = fileName;
            _createCommands();
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
            var newDir = new IngestDirectoryViewmodel( new IngestDirectory(), _directories) { DirectoryName = Common.Properties.Resources._title_NewDirectory };
            _directories.Add(newDir);
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
            else
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
            if (collection != null)
                return collection.IndexOf(_selectedDirectory) > 0;
            else
                return false;
        }

        public ObservableCollection<IngestDirectoryViewmodel> Directories { get { return _directories; } }
        IngestDirectoryViewmodel _selectedDirectory;
        
        private bool _added;
        private bool _deleted;
        private bool _moved;

        public IngestDirectoryViewmodel SelectedDirectory
        {
            get { return _selectedDirectory; }
            set
            {
                if (_selectedDirectory != value)
                {
                    _selectedDirectory = value;
                    NotifyPropertyChanged(nameof(SelectedDirectory));
                }
            }
        }
        
        protected override void OnDispose()
        {
            
        }
        
        public override bool IsModified { get { return _added || _deleted || _moved|| _directories.Any(d => d.IsModified); } }

        public override void ModelUpdate(object parameter)
        {
            _directories.Where(d => d.IsModified).All(d =>
            {
                d.ModelUpdate();
                return true;
            });
            XmlSerializer writer = new XmlSerializer(typeof(List<IngestDirectory>), new XmlRootAttribute("IngestDirectories"));
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(_fileName))
            {
                writer.Serialize(file, _directories.Select(d => d.Model).ToList());
                file.Close();
            }
        }

    }
}
