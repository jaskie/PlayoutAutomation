using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    public static class PluginManager
    {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(PluginManager));
        private static readonly IEnumerable<IEnginePluginFactory> EnginePlugins;
        
        static PluginManager()
        {
            Logger.Debug("Creating");
            using (var catalog = new DirectoryCatalog(Path.Combine(Directory.GetCurrentDirectory(), "Plugins"), "TAS.Server.*.dll"))
            {
                var container = new CompositionContainer(catalog);
                container.ComposeExportedValue("AppSettings", ConfigurationManager.AppSettings);
                try
                {
                    EnginePlugins = container.GetExportedValues<IEnginePluginFactory>();
                }
                catch (ReflectionTypeLoadException e)
                {
                    foreach (var loaderException in e.LoaderExceptions)
                        Logger.Error(e, "Plugin load exception: {0}", loaderException);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Plugin load failed: {0}", e);
                }
            }
        }

        public static T ComposePart<T>(this IEngine engine) 
        {
            var factory = EnginePlugins?.FirstOrDefault(f => typeof(T).IsAssignableFrom(f.Type));
            if (factory != null)
                return (T)factory.CreateEnginePlugin(engine);
            return default(T);
        }

        public static IEnumerable<T> ComposeParts<T>(this IEngine engine)
        {
            var factories = EnginePlugins?.Where(f => typeof(T).IsAssignableFrom(f.Type));
            return factories?.Select(f => (T)f.CreateEnginePlugin(engine)).Where(f => f != null);
        }

    }
}
