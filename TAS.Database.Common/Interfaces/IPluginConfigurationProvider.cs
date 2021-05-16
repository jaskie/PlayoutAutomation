using System;
using TAS.Common.Interfaces;

namespace TAS.Database.Common.Interfaces
{
    public interface IPluginConfigurationProvider
    {
        IPluginConfiguratorViewModel GetConfiguratorViewModel(IEngineProperties engine);
        HibernationBinder Binder { get; }
        Type GetPluginModelType();
    }
}
