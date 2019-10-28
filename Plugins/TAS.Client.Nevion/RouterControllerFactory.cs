using NLog;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Server.Router.Helpers;
using TAS.Server.Router.Model;

namespace TAS.Server
{
    [Export(typeof(IEnginePluginFactory))]
    public class RouterControllerFactory: IEnginePluginFactory
    {
        private readonly RouterDevice[] _routerDevices;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public RouterControllerFactory()
        {
            _routerDevices = DataStore.Load<RouterDevice[]>(Path.Combine(FileUtils.ConfigurationPath, "RouterDevice"), new System.Xml.Serialization.XmlRootAttribute("RouterDevices"));

            if (_routerDevices == null)
            {
                Debug.WriteLine("Błąd deserializacji XML krosownic");
                Logger.Warn("Router config read error");
                return;
            }
        }

        public object CreateEnginePlugin(IEngine engine)
        {
            if (_routerDevices == null)
                return null;
            
            var routerDevice = _routerDevices.FirstOrDefault(c => c.EngineName == engine.EngineName);
            if (routerDevice == null)
                return null;

            return new RouterController(routerDevice);
        }

        public Type Type { get; } = typeof(RouterController);
    }
}
