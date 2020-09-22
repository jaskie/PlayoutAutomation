using System.ComponentModel.Composition;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;
using TAS.Server.Advantech.Configurator;

namespace TAS.Server.Advantech
{
    [Export(typeof(IPluginExport))]
    public class PluginExporter : IPluginExport
    {
        private IPluginTypeBinder _binder = new PluginTypeBinder();
        public IPluginTypeBinder Binder => _binder;

        public IPluginConfigurator GetConfiguratorViewModel(IConfigEngine engine) => new GpiViewModel();        
    }
}
