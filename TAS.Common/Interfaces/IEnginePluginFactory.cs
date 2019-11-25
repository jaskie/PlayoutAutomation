using System;

namespace TAS.Common.Interfaces
{
    public interface IEnginePluginFactory
    {
        object CreateEnginePlugin(IEngine engine);
        Type Type { get; }
    }
}
