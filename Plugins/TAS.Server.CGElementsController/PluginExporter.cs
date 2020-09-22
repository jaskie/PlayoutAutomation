using System.ComponentModel.Composition;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;
using TAS.Server.CgElementsController.Configurator;

namespace TAS.Server.CgElementsController
{
    [Export(typeof(IPluginExport))]
    public class PluginExporter : IPluginExport
    {
        private IPluginTypeBinder _binder = new PluginTypeBinder();
        public IPluginTypeBinder Binder => _binder;

        public IPluginConfigurator GetConfiguratorViewModel(IConfigEngine engine) => new CgElementsControllerViewModel(engine);        
    }
}
