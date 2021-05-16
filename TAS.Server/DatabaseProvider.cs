using System;
using System.Configuration;
using System.Linq;
using TAS.Common;
using TAS.Database.Common;
using TAS.Database.Common.Interfaces;

namespace TAS.Server
{
    public static class DatabaseProvider
    {
        static DatabaseProvider()
        {
            if (!Enum.TryParse<DatabaseType>(ConfigurationManager.AppSettings["DatabaseType"], out var databaseType))
                throw new ApplicationException("Database type not configured");
            Database = DatabaseLoader.LoadDatabaseProviders().FirstOrDefault(db => db.DatabaseType == databaseType) ??
                throw new ApplicationException($"Database provider plugin for {databaseType} not found");
            Database.Open(ConfigurationManager.ConnectionStrings, false, null);
            Database.InitializeFieldLengths();                        
        }

        public static IDatabase Database { get; }
    }
}
