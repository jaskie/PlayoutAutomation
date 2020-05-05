using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using TAS.Common;
using TAS.Database.Common.Interfaces;

namespace TAS.Database.Common
{
    public static class DatabaseLoader
    {
        private const string FileNameSearchPattern = "TAS.Database.*.dll";
     
        public static IEnumerable<IDatabase> LoadDatabaseProviders()
        {
            using (var catalog = new DirectoryCatalog(Directory.GetCurrentDirectory(), FileNameSearchPattern))
            using (var container = new CompositionContainer(catalog))
            {
                return container.GetExportedValues<IDatabase>();
            }
        }

        public static IDatabaseConfigurator LoadDatabaseConfigurator(DatabaseType databaseType)
        {
            using (var catalog = new DirectoryCatalog(Directory.GetCurrentDirectory(), FileNameSearchPattern))
            using (var container = new CompositionContainer(catalog))
            {
                return container.GetExportedValues<IDatabaseConfigurator>().FirstOrDefault(v => v.DatabaseType == databaseType);
            }
        }

    }
}
