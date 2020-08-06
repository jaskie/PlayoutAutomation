using System;

namespace TAS.Common.Interfaces
{
    //obsolete?
    public interface IEnginePluginFactory
    {
        object CreateEnginePlugin(IEngine engine);
        Type Type { get; }
    }
}
