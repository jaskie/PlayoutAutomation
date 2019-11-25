using System;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using NLog;

namespace TAS.Client.Common.Plugin
{
    public static class UiPluginManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly IUiPluginFactory[] Factories;

        static UiPluginManager()
        {
            var directory = Path.Combine(Directory.GetCurrentDirectory(), "Plugins");
            if (!Directory.Exists(directory))
                return;
            using (var catalog = new DirectoryCatalog(directory, "TAS.Client.*.dll"))
            using (var container = new CompositionContainer(catalog))
            {
                try
                {
                    Factories = container.GetExportedValues<IUiPluginFactory>().ToArray();
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

        public static T[] ComposeParts<T>(IUiPluginContext context) where T : IUiPlugin
        {
            try
            {
                if (Factories != null)
                    return Factories
                        .Where(f => typeof(T).IsAssignableFrom(f.Type))
                        .Select(f => (T) f.CreateNew(context))
                        .Where(p => p != null)
                        .ToArray();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            return new T[0];
        }

        public static T ComposePart<T>(IUiPluginContext context)
        {
            try
            {
                if (Factories != null)
                    return Factories
                        .Where(f => typeof(T).IsAssignableFrom(f.Type))
                        .Select(f => (T) f.CreateNew(context))
                        .FirstOrDefault(p => p != null);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            return default(T);
        }
    }
}
