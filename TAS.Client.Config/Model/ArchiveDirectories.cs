using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Database;

namespace TAS.Client.Config.Model
{
    public class ArchiveDirectories
    {
        public readonly List<ArchiveDirectory> Directories;

        public ArchiveDirectories(string connectionStringPrimary = null, string connectionStringSecondary = null)
        {
            try
            {
                Database.Open(connectionStringPrimary, connectionStringSecondary);
                Directories = Database.DbLoadArchiveDirectories<ArchiveDirectory>();
                Directories.ForEach(d => d.Modified = false);
            }
            finally
            {
                Database.Close();
            }
        }

        public void Save()
        {
            Database.Open();
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
                    if (dir.Modified)
                        dir.DbUpdateArchiveDirectory();
                }
            }
            finally
            {
                Database.Close();
            }
        }

    }
}
