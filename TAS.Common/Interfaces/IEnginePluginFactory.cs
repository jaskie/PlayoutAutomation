using System;
using System.Collections.Generic;

namespace TAS.Common.Interfaces
{
    public interface IEnginePluginFactory
    {
        object CreateEnginePlugin(IEngine engine, Type type);
        IEnumerable<Type> Types();
    }
}
