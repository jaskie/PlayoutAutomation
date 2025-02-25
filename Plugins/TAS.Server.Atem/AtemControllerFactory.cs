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

        public T CreateEnginePlugin<T>(IEngine engine) where T : class
        {
            var atemDevice = _atemDevices?.FirstOrDefault(c => c.EngineName == engine.EngineName);
            if (atemDevice == null)
                return null;
            if (atemDevice.AtemController is null)
                atemDevice.AtemController = new AtemController(atemDevice);
            switch (typeof(T))
            {
                case Type t when t == typeof(IGpi) && atemDevice.StartME > 0:
                    return atemDevice.AtemController as T;
                case Type t when t == typeof(IRouter) && atemDevice.InputSelectME > 0:
                    return atemDevice.AtemController as T;
                default:
                    return null;
            }
        }

        public Type Type { get; } = typeof(AtemController);
    }
}
