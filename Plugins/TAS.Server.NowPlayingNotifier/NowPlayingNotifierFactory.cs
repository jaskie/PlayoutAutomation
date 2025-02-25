using System;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using NLog;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    [Export(typeof(IEnginePluginFactory))]
    public class NowPlayingNotifierFactory : IEnginePluginFactory
    {
        private readonly NowPlayingNotifier[] _plugins;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        [ImportingConstructor]
        public NowPlayingNotifierFactory([Import("AppSettings")] NameValueCollection settings)
        {
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), settings["NowPlayingNotifier"] ?? "Configuration\\NowPlayingNotifier.xml");
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

        public T CreateEnginePlugin<T>(IEngine engine) where T : class
        {
            var plugin = _plugins.FirstOrDefault(p => p.Engine == engine);
            if (plugin != null)
                return plugin as T;
            plugin = _plugins.FirstOrDefault(p => p.EngineName == engine.EngineName);
            plugin?.Initialize(engine);
            return plugin as T;
        }

        public Type Type { get; } = typeof(NowPlayingNotifier);
    }
}