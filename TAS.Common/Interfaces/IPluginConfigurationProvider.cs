using TAS.Common.Interfaces.Configurator;

namespace TAS.Common.Interfaces
{
    public interface IPluginConfigurationProvider
    {
        IPluginConfigurator GetConfiguratorViewModel(IConfigEngine engine = null);
        IPluginTypeBinder Binder { get; }
    }
}
