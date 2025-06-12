using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using TAS.Common;
using TAS.Common.Helpers;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    [Export(typeof(IEnginePluginFactory))]
    public class CgElementsControllerFactory : IEnginePluginFactory
    {
        private readonly CgElementsController[] _cgElementsControllers;
        private const string ElementsFileName = "CgElementsControllers.xml";

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public CgElementsControllerFactory()
        {
            var file = Path.Combine(FileUtils.ConfigurationPath, ElementsFileName);
            _cgElementsControllers = XmlDataStore.Load<CgElementsController[]>(file, new XmlRootAttribute("CgElementsControllers"));
            if (_cgElementsControllers != null)
                Logger.Warn($"Configuration file ({file}) empty or missing");
        }

        public T CreateEnginePlugin<T>(EnginePluginContext enginePluginContext) where T : class
        {
            if (_cgElementsControllers == null)
                return null;
            var controller = _cgElementsControllers.FirstOrDefault(c => c.Engine != null && c.Engine == enginePluginContext.Engine);
            if (controller != null)
                return controller as T;
            controller = _cgElementsControllers.FirstOrDefault(c => c.EngineName == enginePluginContext.Engine.EngineName);
            if (controller == null)
                return null;
            if (controller.Engine != null)
                throw new ApplicationException($"Unable to re-use CgElementsController. Duplicated engine name {enginePluginContext.Engine.EngineName}?");
            controller.Engine = enginePluginContext.Engine;
            return controller as T;
        }

        public Type Type { get; } = typeof(CgElementsController);
        
    }
}