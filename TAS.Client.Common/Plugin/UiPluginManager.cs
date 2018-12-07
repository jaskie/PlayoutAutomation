using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using NLog;

namespace TAS.Client.Common.Plugin
{
    public static class UiPluginManager
    {
        private static readonly Logger Logger = LogManager.GetLogger(nameof(UiPluginManager));
        private static readonly CompositionContainer UiContainer;

        static UiPluginManager()
        {
            var pluginPath = Path.Combine(Directory.GetCurrentDirectory(), "Plugins");
            if (!Directory.Exists(pluginPath))
                return;
            var catalog = new DirectoryCatalog(pluginPath);
            UiContainer = new CompositionContainer(catalog);
        }


        public static void ComposeUiPlugins(this object component, Func<PluginExecuteContext> executionContext = null)
        {
            try
            {
                if (executionContext != null)
                    UiContainer.ComposeExportedValue(executionContext);
                UiContainer.SatisfyImportsOnce(component);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}
