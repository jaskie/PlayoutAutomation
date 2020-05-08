using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using NLog;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    [Export(typeof(IEnginePluginFactory))]
    public class CgElementsControllerFactory : IEnginePluginFactory
    {
        private readonly CgElementsController[] _cgElementsControllers;
        private const string ElementsFileName = "CgElementsControllers.xml";

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public CgElementsControllerFactory()
        {
            var file = Path.Combine(FileUtils.ConfigurationPath, ElementsFileName);
            if (!File.Exists(file))
            {
                Logger.Warn("Configuration file ({0}) missing", file);
                return;
            }
            using (var streamReader = new FileStream(file, FileMode.Open))
            {
                var serializer = new XmlSerializer(typeof(CgElementsController[]), new XmlRootAttribute("CgElementsControllers"));
                _cgElementsControllers = (CgElementsController[]) serializer.Deserialize(streamReader);
            }
        }

        public object CreateEnginePlugin(IEngine engine)
        {
            if (_cgElementsControllers == null || engine == null)
                return null;

            var controller = _cgElementsControllers.FirstOrDefault(c => c.Engine == engine);            
            if (controller != null)
                return controller;

            controller = _cgElementsControllers.FirstOrDefault(c => c.EngineName == engine.EngineName);            
            if (controller == null)
                return null;
            if (controller.Engine != null)
                throw new ApplicationException($"Unable to re-use CgElementsController. Duplicated engine name {engine.EngineName}?");

            if (controller.IsEnabled)
            {
                controller.Engine = engine;
                return controller;
            }
            else
                return null;            
        }

        public Type Type { get; } = typeof(CgElementsController);
        
    }
}