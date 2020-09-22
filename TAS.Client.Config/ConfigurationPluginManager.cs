using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using TAS.Common.Interfaces;

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
                foreach(var plugin in container.GetExportedValues<IPluginExport>())
                {
                    if (Binders == null)
                        Binders = new List<IPluginTypeBinder>();

                    Binders.Add(plugin.Binder);

                    var configuratorVm = plugin.GetConfiguratorViewModel();
                    if (configuratorVm.GetModel() is ICGElementsController)
                    {
                        if (CgElementsControllers == null)
                            CgElementsControllers = new List<IPluginExport>();

                        CgElementsControllers.Add(plugin);
                    }

                    else if (configuratorVm.GetModel() is IVideoSwitch)
                    {
                        if (VideoSwitchers == null)
                            VideoSwitchers = new List<IPluginExport>();

                        VideoSwitchers.Add(plugin);
                    }

                    else if (configuratorVm.GetModel() is IGpi)
                    {
                        if (Gpis == null)
                            Gpis = new List<IPluginExport>();

                        Gpis.Add(plugin);
                    }
                }                                
            }
        }

        public IList<IPluginExport> CgElementsControllers { get; private set; }
        public IList<IPluginExport> Gpis { get; private set; }
        public IList<IPluginExport> VideoSwitchers { get; private set; }
        public IList<IPluginTypeBinder> Binders { get; }

        public static ConfigurationPluginManager Current { get; } = new ConfigurationPluginManager();
    }
}
