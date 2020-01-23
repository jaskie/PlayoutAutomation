using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using TAS.Common.Database.Interfaces;

namespace TAS.Common.Database
{
    public static class DatabaseConfiguratorLoader
    {
        public static IDatabaseConfigurator LoadDatabaseConfigurator(DatabaseType databaseType)
        {
            using (DirectoryCatalog catalog = new DirectoryCatalog(Directory.GetCurrentDirectory(), "TAS.Database.*.dll"))
            {
                var container = new CompositionContainer(catalog);
                return container.GetExportedValues<IDatabaseConfigurator>().FirstOrDefault(v => v.DatabaseType == databaseType);
            }
        }
   }
}
