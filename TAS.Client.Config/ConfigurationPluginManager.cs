using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using TAS.Database.Common;
using TAS.Database.Common.Interfaces;

namespace TAS.Client.Config
{
    public class ConfigurationPluginManager
    {
        private const string FileNameSearchPattern = "TAS.Server.*.dll";
        
        private ConfigurationPluginManager()
        {
            using (var catalog = new DirectoryCatalog(Path.Combine(Directory.GetCurrentDirectory(), "Plugins"), FileNameSearchPattern))
            using (var container = new CompositionContainer(catalog))
            {
                ConfigurationProviders = container.GetExportedValues<IPluginConfigurationProvider>().ToArray();
            }
        }

        public static ConfigurationPluginManager Current { get; } = new ConfigurationPluginManager();

        public IPluginConfigurationProvider[] ConfigurationProviders { get; }
        public bool IsPluginAvailable => ConfigurationProviders.Length > 0;
        public IEnumerable<HibernationBinder> Binders => ConfigurationProviders.Select(cp => cp.Binder);
    }
}
