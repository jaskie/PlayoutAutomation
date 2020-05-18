using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
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
                PluginTypeBinders = container.GetExportedValues<IPluginTypeBinder>();
            }
        }

        [ImportMany]
        public IEnumerable<IPluginTypeBinder> PluginTypeBinders { get; }


        public static ConfigurationPluginManager Current { get; } = new ConfigurationPluginManager();
    }
}
