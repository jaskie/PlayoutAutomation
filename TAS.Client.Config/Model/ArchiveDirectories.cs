using System.Collections.Generic;
using System.Linq;
using TAS.Database;

namespace TAS.Client.Config.Model
{
    public class ArchiveDirectories
    {
        public readonly List<ArchiveDirectory> Directories;

        public ArchiveDirectories(string connectionStringPrimary = null, string connectionStringSecondary = null)
        {
            try
            {
                Db.Open(connectionStringPrimary, connectionStringSecondary);
                Directories = Db.DbLoadArchiveDirectories<ArchiveDirectory>();
                Directories.ForEach(d => d.IsModified = false);
            }
            finally
            {
                Db.Close();
            }
        }

        public void Save()
        {
            Db.Open();
            try
            {
                foreach (var dir in Directories.ToList())
                {
                    if (dir.IsDeleted)
                    {
                        dir.DbDeleteArchiveDirectory();
                        Directories.Remove(dir);
                    }
                    else
                    if (dir.IsNew)
                        dir.DbInsertArchiveDirectory();
                    else
                    if (dir.IsModified)
                        dir.DbUpdateArchiveDirectory();
                }
            }
            finally
            {
                Db.Close();
            }
        }

    }
}
