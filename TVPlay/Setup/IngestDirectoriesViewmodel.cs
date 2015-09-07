using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Client.Setup
{
    public class IngestDirectoriesViewmodel: OkCancelViewmodelBase<IEnumerable<IIngestDirectory>>
    {
        private readonly ObservableCollection<IngestDirectoryViewmodel> _directories;
        public IngestDirectoriesViewmodel(IEnumerable<IIngestDirectory> directories): base(directories, new IngestFoldersView(), "Ingest directories", 500, 300)
        {
            _directories = new ObservableCollection<IngestDirectoryViewmodel>(directories.Select(d => new IngestDirectoryViewmodel(d)));
        }

        public ObservableCollection<IngestDirectoryViewmodel> Directories { get { return _directories; } }
        
        protected override void OnDispose()
        {
            throw new NotImplementedException();
        }
    }
}
