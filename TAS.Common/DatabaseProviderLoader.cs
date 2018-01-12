using System.ComponentModel.Composition.Hosting;
using System.IO;
using TAS.Common.Interfaces;

namespace TAS.Common
{
    public static class DatabaseProviderLoader
    {
        public static IDatabase LoadDatabaseProvider()
        {
            using (DirectoryCatalog catalog = new DirectoryCatalog(Directory.GetCurrentDirectory(), "TAS.Database.*.dll"))
            {
                var container = new CompositionContainer(catalog);
                return container.GetExportedValue<IDatabase>();
            }
        }
    }
}
