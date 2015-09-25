using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Xml.Serialization;
using TAS.Client.Common;
using TAS.Client.Setup.Model;
using TAS.Server.Interfaces;

namespace TAS.Client.Setup
{
    public class IngestDirectoriesViewmodel: OkCancelViewmodelBase<IEnumerable<IIngestDirectory>>
    {
        public ICommand CommandAdd { get; private set; }
        public ICommand CommandDelete { get; private set; }
        private readonly ObservableCollection<IngestDirectoryViewmodel> _directories;
        private readonly string _fileName;
        public IngestDirectoriesViewmodel(IEnumerable<IIngestDirectory> directories, string fileName): base(directories, new IngestFoldersView(), string.Format("Ingest directories ({0})", fileName))
        {
            _directories = new ObservableCollection<IngestDirectoryViewmodel>(directories.Select(d => new IngestDirectoryViewmodel(d)));
            _fileName = fileName;
            _createCommands();
        }

        public IngestDirectoriesViewmodel(string fileName) : base(Deserialize(fileName), new IngestFoldersView(), "Ingest directories") 
        {
            _directories = new ObservableCollection<IngestDirectoryViewmodel>(Model.Select(d => new IngestDirectoryViewmodel(d)));
            _fileName = fileName;
            _createCommands();
        }

        private static IEnumerable<IIngestDirectory> Deserialize(string fileName)
        {
            try
            {
                XmlSerializer reader = new XmlSerializer(typeof(List<IngestDirectory>), new XmlRootAttribute("IngestDirectories"));
                System.IO.StreamReader file = null;
                try
                {
                    file = new System.IO.StreamReader(fileName);
                    return (IEnumerable<IIngestDirectory>)reader.Deserialize(file);
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
            var newDir = new IngestDirectoryViewmodel() { DirectoryName = Common.Properties.Resources._title_NewDirectory };
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
            System.IO.StreamWriter file = new System.IO.StreamWriter(_fileName);
            writer.Serialize(file, _directories.ToList());
            file.Close();
            base.Apply(parameter);
        }

    }
}
