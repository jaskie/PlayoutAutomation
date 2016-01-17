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

        private readonly string _connectionStringPrimary;
        private readonly string _connectionStringSecondary;
        public ArchiveDirectories(string connectionStringPrimary, string connectionStringSecondary)
        {
            _connectionStringPrimary = connectionStringPrimary;
            _connectionStringSecondary = connectionStringSecondary;
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
            try
            {
            }
            finally
            {
                Database.Close();
            }
        }

    }
}
