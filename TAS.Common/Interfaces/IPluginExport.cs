using TAS.Common.Interfaces.Configurator;

namespace TAS.Common.Interfaces
{
    public interface IPluginExport
    {
        IPluginConfigurator GetConfiguratorViewModel(IConfigEngine engine = null);
        IPluginTypeBinder Binder { get; }
    }
}
