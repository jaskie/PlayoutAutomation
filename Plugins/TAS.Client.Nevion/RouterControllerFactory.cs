using NLog;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Server.Model;
using TAS.Server.Router.Helpers;

namespace TAS.Server
{
    [Export(typeof(IEnginePluginFactory))]
    public class RouterControllerFactory: IEnginePluginFactory
    {
        private readonly RouterDevice[] _routerDevices;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public RouterControllerFactory()
        {
            _routerDevices = DataStore.Load<RouterDevice[]>(Path.Combine(FileUtils.ConfigurationPath, "RouterDevices"), new System.Xml.Serialization.XmlRootAttribute("RouterDevices"));
            if (_routerDevices != null) return;
            Debug.WriteLine("Router configuration readError");
            Logger.Error("Router config read error");
        }

        public object CreateEnginePlugin(IEngine engine)
        {
            var routerDevice = _routerDevices?.FirstOrDefault(c => c.EngineName == engine.EngineName);
            if (routerDevice == null)
                return null;
            if (routerDevice.Engine != null && routerDevice.Engine != engine)
                throw new InvalidOperationException("Router reused");
            routerDevice.Engine = engine;
            return new RouterController(routerDevice);
        }

        public Type Type { get; } = typeof(RouterController);
    }
}
