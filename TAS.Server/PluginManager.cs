using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Server.Common
{
    public static class PluginManager
    {

        static NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(PluginManager));
        private static readonly CompositionContainer ServerContainer;
        static IEnumerable<IEnginePluginFactory> _enginePlugins = null;


        static PluginManager()
        {
            Logger.Debug("Creating");
            DirectoryCatalog catalog = new DirectoryCatalog(Path.Combine(Directory.GetCurrentDirectory(),  "Plugins"), "TAS.Server.*.dll");
            ServerContainer = new CompositionContainer(catalog);
            ServerContainer.ComposeExportedValue("AppSettings", ConfigurationManager.AppSettings);
            try
            {
                _enginePlugins = ServerContainer.GetExportedValues<IEnginePluginFactory>();
            }
            catch (ReflectionTypeLoadException e)
            {
                Logger.Error(e, "Plugin load failed: {0}", e.LoaderExceptions);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Plugin load failed: {0}", e);
            }
        }

        public static T ComposePart<T>(this IEngine engine) 
        {
            var factory = _enginePlugins?.FirstOrDefault(f => f.Types().Any(t => typeof(T).IsAssignableFrom(t)));
            if (factory != null)
                return (T)factory.CreateEnginePlugin(engine, typeof(T));
            return default(T);
        }

        public static IEnumerable<T> ComposeParts<T>(this IEngine engine)
        {
            var factories = _enginePlugins?.Where(f => f.Types().Any(t => typeof(T).IsAssignableFrom(t)));
            if (factories != null)
                return factories.Select(f => (T)f.CreateEnginePlugin(engine, typeof(T))).Where(f => f != null);
            else
                return null;

        }

    }
}
