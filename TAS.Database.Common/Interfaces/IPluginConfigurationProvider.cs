using System;
using TAS.Common.Interfaces.Configurator;

namespace TAS.Database.Common.Interfaces
{
    public interface IPluginConfigurationProvider
    {
        IPluginConfiguratorViewModel GetConfiguratorViewModel(IConfigEngine engine);
        HibernationBinder Binder { get; }
        Type GetPluginModelType();
    }
}
