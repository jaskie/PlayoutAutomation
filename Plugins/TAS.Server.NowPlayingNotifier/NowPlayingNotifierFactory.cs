using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    [Export(typeof(IEnginePluginFactory))]
    public class NowPlayingNotifierFactory : IEnginePluginFactory
    {
        private readonly NowPlayingNotifier[] _plugins;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public NowPlayingNotifierFactory()
        {
            var configPath = Path.Combine(FileUtils.ConfigurationPath, "NowPlayingNotifier.xml");
            if (!File.Exists(configPath))
            {
                Logger.Warn("Configuration file ({0}) missing", configPath);
                return;
            }
            using (var reader = new StreamReader(configPath))
            {
                var serializer = new XmlSerializer(typeof(NowPlayingNotifier[]), new XmlRootAttribute("NowPlayingNotifiers"));
                _plugins = (NowPlayingNotifier[]) serializer.Deserialize(reader);
            }
        }

        public T CreateEnginePlugin<T>(EnginePluginContext enginePluginContext) where T : class
        {
            var plugin = _plugins.FirstOrDefault(p => p.Engine == enginePluginContext.Engine);
            if (plugin != null)
                return plugin as T;
            plugin = _plugins.FirstOrDefault(p => p.EngineName == enginePluginContext.Engine.EngineName);
            plugin?.Initialize(enginePluginContext.Engine);
            return plugin as T;
        }

        public Type Type { get; } = typeof(NowPlayingNotifier);
    }
}