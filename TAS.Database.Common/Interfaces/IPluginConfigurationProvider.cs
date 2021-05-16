using System;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Configurator;

namespace TAS.Database.Common.Interfaces
{
    public interface IPluginConfigurationProvider
    {
        IPluginConfiguratorViewModel GetConfiguratorViewModel(IEngineProperties engine);
        HibernationBinder Binder { get; }
        Type GetPluginInterfaceType();
    }
}
