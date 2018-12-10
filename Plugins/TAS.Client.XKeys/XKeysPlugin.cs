using System.IO;
using System.Linq;
using System.Xml.Serialization;
using NLog;
using TAS.Client.Common.Plugin;
using TAS.Common;

namespace TAS.Client.XKeys
{
    public class XKeysPlugin : IUiPlugin
    {

        private static readonly Logger Logger = LogManager.GetLogger(nameof(XKeysPlugin));
        private static readonly XKey[] XKeys;
        private const string ConfigurationFileName = "XKeys.xml";

        static XKeysPlugin()
        {
            var file = Path.Combine(FileUtils.ConfigurationPath, ConfigurationFileName);
            if (!File.Exists(file))
            {
                Logger.Warn("Configuration file ({0}) missing", file);
                return;
            }
            using (var streamReader = new FileStream(file, FileMode.Open))
            {
                var serializer = new XmlSerializer(typeof(XKey[]), new XmlRootAttribute("XKeys"));
                XKeys = (XKey[])serializer.Deserialize(streamReader);
            }
        }

        public XKeysPlugin(IUiPluginContext context)
        {
            Context = context;
            var engine = context.Engine;
            if (engine == null)
                return;
            var xKey = XKeys.FirstOrDefault(k => k.EngineName == engine.EngineName);
            if (xKey != null)
            {
                xKey.Engine = engine;
            }
        }
        
        public void NotifyExecuteChanged()
        {
        }

        public IUiMenuItem Menu { get; } = null;

        public IUiPluginContext Context { get;  }
    }
}
