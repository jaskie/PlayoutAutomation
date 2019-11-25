using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    public static class PluginManager
    {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly IEnumerable<IEnginePluginFactory> EnginePlugins;
        
        static PluginManager()
        {
            Logger.Debug("Creating");
            var pluginPath = Path.Combine(Directory.GetCurrentDirectory(), "Plugins");
            if (!Directory.Exists(pluginPath))
                return;
            using (var catalog = new DirectoryCatalog(pluginPath, "TAS.Server.*.dll"))
            using (var container = new CompositionContainer(catalog))
            {
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
            try
            {
                var factory = EnginePlugins?.FirstOrDefault(f => typeof(T).IsAssignableFrom(f.Type));
                if (factory != null)
                    return (T) factory.CreateEnginePlugin(engine);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            return default(T);
        }

        public static List<T> ComposeParts<T>(this IEngine engine)
        {
            try
            {
                if (EnginePlugins != null)
                {
                    var factories = EnginePlugins.Where(f => typeof(T).IsAssignableFrom(f.Type));
                    return factories.Select(f => (T) f.CreateEnginePlugin(engine)).Where(f => f != null).ToList();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            return new List<T>();
        }

    }
}
