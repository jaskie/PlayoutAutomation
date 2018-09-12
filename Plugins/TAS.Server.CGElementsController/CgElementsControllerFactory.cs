using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    [Export(typeof(IEnginePluginFactory))]
    public class CgElementsControllerFactory : IEnginePluginFactory
    {
        private readonly Dictionary<IEngine, CgElementsController> _plugins = new Dictionary<IEngine, CgElementsController>();
        private readonly object _pluginLock = new object();
        public object CreateEnginePlugin(IEngine engine)
        {
            CgElementsController plugin;
            lock (_pluginLock)
            {
                if (_plugins.TryGetValue(engine, out plugin))
                    return plugin;
                plugin = new CgElementsController(engine);
                plugin.Initialize();
                _plugins.Add(engine, plugin);
            }
            return plugin;
        }

        public Type Type { get; } = typeof(CgElementsController);
        
    }
}