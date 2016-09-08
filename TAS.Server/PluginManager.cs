using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Configuration;
using System.Linq;
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
            DirectoryCatalog catalog = new DirectoryCatalog(".\\Plugins", "TAS.Server.*.dll");
            ServerContainer = new CompositionContainer(catalog);
            ServerContainer.ComposeExportedValue("AppSettings", ConfigurationManager.AppSettings);
            _enginePlugins = ServerContainer.GetExportedValues<IEnginePluginFactory>();
        }

        public static T ComposePart<T>(this IEngine engine) 
        {
            var factory = _enginePlugins.FirstOrDefault(f => f.Types().Any(t => typeof(T).IsAssignableFrom(t)));
            if (factory != null)
                return (T)factory.CreateEnginePlugin(engine, typeof(T));
            return default(T);
        }

        public static IEnumerable<T> ComposeParts<T>(this IEngine engine)
        {
            var factories = _enginePlugins.Where(f => f.Types().Any(t => typeof(T).IsAssignableFrom(t)));
            return factories.Select(f => (T)f.CreateEnginePlugin(engine, typeof(T)));
        }

    }
}
