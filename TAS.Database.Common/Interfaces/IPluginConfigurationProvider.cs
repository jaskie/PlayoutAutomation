using System;
using TAS.Common.Interfaces.Configurator;

namespace TAS.Database.Common.Interfaces
{
    public interface IPluginConfigurationProvider
    {
        IPluginConfiguratorViewModel GetConfiguratorViewModel(IConfigEngine engine = null);
        HibernationBinder Binder { get; }
        Type GetPluginModelType();
    }
}
