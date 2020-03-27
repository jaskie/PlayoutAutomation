using System;
using System.Configuration;
using System.Linq;
using TAS.Common;
using TAS.Common.Database;
using TAS.Common.Database.Interfaces;

namespace TAS.Server
{
    public static class DatabaseProvider
    {
        static DatabaseProvider()
        {
            if (!Enum.TryParse<DatabaseType>(ConfigurationManager.AppSettings["DatabaseType"], out var databaseType))
                throw new ApplicationException("Database type not configured");
            Database = DatabaseProviderLoader.LoadDatabaseProviders().FirstOrDefault(db => db.DatabaseType == databaseType) ??
                throw new ApplicationException($"Database provider {databaseType} not available");
            Database.Open(ConfigurationManager.ConnectionStrings);
            Database.InitializeFieldLengths();
        }

        public static IDatabase Database { get; }
    }
}
