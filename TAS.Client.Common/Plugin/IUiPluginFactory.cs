using System;

namespace TAS.Client.Common.Plugin
{
    public interface IUiPluginFactory
    {
        object CreateNew(IUiPluginContext context);
        Type Type { get; }
    }
}
