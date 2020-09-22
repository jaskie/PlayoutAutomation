using System;
using System.ComponentModel.Composition;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;
using TAS.Server.VideoSwitch.Configurator;

namespace TAS.Server.VideoSwitch
{
    [Export(typeof(IPluginExport))]
    public class PluginExporter : IPluginExport
    {
        private PluginTypeBinder _binder = new PluginTypeBinder();
        public IPluginTypeBinder Binder => _binder;

        public IPluginConfigurator GetConfiguratorViewModel(IConfigEngine engine) => new RouterConfiguratorViewModel(engine);        
    }
}
