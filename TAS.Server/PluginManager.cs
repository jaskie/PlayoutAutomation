using System;
using System.Collections.Generic;
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

        public static T ComposePart<T>(this IEngine engine) where T: class
        {
            try
            {
                var factory = EnginePlugins?.FirstOrDefault(f => typeof(T).IsAssignableFrom(f.Type));
                if (factory != null)
                    return factory.CreateEnginePlugin<T>(new EnginePluginContext
                    {
                        Engine = engine,
                        AppSettings = ConfigurationManager.AppSettings
                    });
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            return null;
        }

        public static T[] ComposeParts<T>(this IEngine engine) where T : class
        {
            try
            {
                if (EnginePlugins != null)
                {
                    var factories = EnginePlugins.Where(f => typeof(T).IsAssignableFrom(f.Type));
                    return factories.Select(f => f.CreateEnginePlugin<T>(new EnginePluginContext
                    {
                        Engine = engine,
                        AppSettings = ConfigurationManager.AppSettings
                    }
                    )).Where(f => f != null).ToArray();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            return Array.Empty<T>();
        }

    }
}
