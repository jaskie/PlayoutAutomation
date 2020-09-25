using System.ComponentModel.Composition;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;
using TAS.Server.Advantech.Configurator;

namespace TAS.Server.Advantech
{
    [Export(typeof(IPluginConfigurationProvider))]
    public class PluginExporter : IPluginConfigurationProvider
    {
        private IPluginTypeBinder _binder = new PluginTypeBinder();
        public IPluginTypeBinder Binder => _binder;

        public IPluginConfigurator GetConfiguratorViewModel(IConfigEngine engine) => new GpiViewModel();        
    }
}
