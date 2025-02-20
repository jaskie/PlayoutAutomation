using NLog;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using TAS.Common;
using TAS.Common.Helpers;
using TAS.Common.Interfaces;


namespace TAS.Server
{
    [Export(typeof(IEnginePluginFactory))]
    public class AtemControllerFactory: IEnginePluginFactory
    {
        private readonly AtemDevice[] _atemDevices;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public AtemControllerFactory()
        {
            _atemDevices = XmlDataStore.Load<AtemDevice[]>(Path.Combine(FileUtils.ConfigurationPath, "AtemDevices"), new System.Xml.Serialization.XmlRootAttribute("AtemDevices"));
            if (_atemDevices != null)
                return;
            Logger.Error("Router config read error");
        }

        public object CreateEnginePlugin(IEngine engine)
        {
            var atemDevice = _atemDevices?.FirstOrDefault(c => c.EngineName == engine.EngineName);
            if (atemDevice == null)
                return null;
            if (atemDevice.Engine != null && atemDevice.Engine != engine)
                throw new InvalidOperationException("Atem plugin reused");
            atemDevice.Engine = engine;
            return new AtemController(atemDevice);
        }

        public Type Type { get; } = typeof(AtemController);
    }
}
