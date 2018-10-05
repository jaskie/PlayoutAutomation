using System.Collections.Generic;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Client.Config.Model
{
    public class ArchiveDirectory: IArchiveDirectoryProperties
    {
        ulong _idArchive;
        string _folder;
        private bool _deleted;

        public ulong idArchive { get { return _idArchive; }  set { SetField(ref _idArchive, value); } }
        public string Folder { get { return _folder; } set { SetField(ref _folder, value); } }
        public bool IsModified { get; internal set; }

        public string DirectoryName { get; set; }
        

        public void Delete()
        {
            _deleted = true;
        }

        public bool IsDeleted => _deleted;

        public bool IsNew => _idArchive == 0;

        protected virtual bool SetField<T>(ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            IsModified = true;
            return true;
        }

        public override string ToString()
        {
            return Folder;
        }
    }
}
