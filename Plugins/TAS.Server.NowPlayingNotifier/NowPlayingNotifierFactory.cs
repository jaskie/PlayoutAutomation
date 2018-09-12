using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    [Export(typeof(IEnginePluginFactory))]
    public class NowPlayingNotifierFactory : IEnginePluginFactory
    {
        private List<NowPlayingNotifier> _plugins;

        [ImportingConstructor]
        public NowPlayingNotifierFactory([Import("AppSettings")] NameValueCollection settings)
        {
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), settings["NowPlayingNotifier"] ?? "Configuration\\NowPlayingNotifier.xml");
            if (!File.Exists(configPath))
                return;
            using (var reader = new StreamReader(configPath))
            {
                var serializer = new XmlSerializer(typeof(List<NowPlayingNotifier>), new XmlRootAttribute("Engines"));
                _plugins = (List<NowPlayingNotifier>) serializer.Deserialize(reader);
            }
        }

        public object CreateEnginePlugin(IEngine engine)
        {
            var plugin = _plugins.FirstOrDefault(p => p.Engine == engine.EngineName);
            plugin?.Initialize(engine);
            return plugin;
        }

        public Type Type { get; } = typeof(NowPlayingNotifier);
    }
}