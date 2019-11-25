using System.Collections.Generic;
using System.Linq;
using TAS.Common.Database.Interfaces;

namespace TAS.Client.Config.Model
{
    public class ArchiveDirectories
    {
        public readonly List<ArchiveDirectory> Directories;
        private readonly IDatabase _db;

        public ArchiveDirectories(IDatabase db)
        {
            _db = db;
            Directories = db.LoadArchiveDirectories<ArchiveDirectory>();
            Directories.ForEach(d => d.IsModified = false);
        }

        public void Save()
        {
            foreach (var dir in Directories.ToList())
            {
                if (dir.IsDeleted)
                {
                    _db.DeleteArchiveDirectory(dir);
                    Directories.Remove(dir);
                }
                else
                if (dir.IsNew)
                    _db.InsertArchiveDirectory(dir);
                else
                if (dir.IsModified)
                    _db.UpdateArchiveDirectory(dir);
            }
        }
    }
}
