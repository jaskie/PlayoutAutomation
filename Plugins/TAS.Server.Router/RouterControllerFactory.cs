using NLog;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using TAS.Common;
using TAS.Common.Helpers;
using TAS.Common.Interfaces;
using TAS.Server.Model;

namespace TAS.Server
{
    [Export(typeof(IEnginePluginFactory))]
    public class RouterControllerFactory: IEnginePluginFactory
    {
        private readonly RouterDevice[] _routerDevices;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public RouterControllerFactory()
        {
            _routerDevices = XmlDataStore.Load<RouterDevice[]>(Path.Combine(FileUtils.ConfigurationPath, "RouterDevices"), new System.Xml.Serialization.XmlRootAttribute("RouterDevices"));
            if (_routerDevices == null) 
                Logger.Error("Router config read error");
        }

        public T CreateEnginePlugin<T>(EnginePluginContext enginePluginContext) where T: class
        {
            var routerDevice = _routerDevices?.FirstOrDefault(c => c.EngineName == enginePluginContext.Engine.EngineName);
            if (routerDevice == null)
                return null;
            if (routerDevice.Engine != null && routerDevice.Engine != enginePluginContext.Engine)
                throw new InvalidOperationException("Router reused");
            routerDevice.Engine = enginePluginContext.Engine;
            return new RouterController(routerDevice) as T;
        }

        public Type Type { get; } = typeof(RouterController);
    }
}
