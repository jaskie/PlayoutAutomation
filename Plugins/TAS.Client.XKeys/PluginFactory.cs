using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using NLog;
using TAS.Client.Common.Plugin;
using TAS.Common;

namespace TAS.Client.XKeys
{
    [Export(typeof(IUiPluginFactory))]
    public class PluginFactory: IUiPluginFactory
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private const string ConfigurationFileName = "XKeys.xml";

        private readonly Plugin[] _plugins;

        public PluginFactory()
        {
            var file = Path.Combine(FileUtils.ConfigurationPath, ConfigurationFileName);
            if (!File.Exists(file))
            {
                Logger.Warn("Configuration file ({0}) missing", file);
                return;
            }
            using (var streamReader = new FileStream(file, FileMode.Open))
            {
                var serializer = new XmlSerializer(typeof(Plugin[]), new XmlRootAttribute("XKeys"));
                _plugins = (Plugin[])serializer.Deserialize(streamReader);
                XKeysDeviceEnumerator.KeyNotified += KeyNotified;
            }
        }

        public object[] Create(IUiPluginContext context)
        {
            var result = _plugins?.Where(xk => string.Equals(xk.EngineName, context.Engine.EngineName, StringComparison.OrdinalIgnoreCase)).ToArray();
            foreach (var plugin in result)
                if (!plugin.SetContext(context))
                    throw new ApplicationException($"The {Type.FullName} plugin cannot be re-used");
            return result;
        }

        public Type Type { get; } = typeof(Plugin);

        private void KeyNotified(object sender, KeyNotifyEventArgs keyNotifyEventArgs)
        {
            Array.ForEach(_plugins, plugin => plugin.Notify(keyNotifyEventArgs));
        }

    }
}
