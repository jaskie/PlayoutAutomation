using System;

namespace TAS.Common.Interfaces
{
    public interface IEnginePluginFactory
    {
        T CreateEnginePlugin<T>(IEngine engine) where T : class;
        Type Type { get; }
    }
}
