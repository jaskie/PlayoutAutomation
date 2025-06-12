using System;

namespace TAS.Common.Interfaces
{
    public interface IEnginePluginFactory
    {
        T CreateEnginePlugin<T>(EnginePluginContext enginePluginContext) where T : class;
        Type Type { get; }
    }
}
