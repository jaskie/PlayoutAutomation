using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server.Interfaces
{
    public interface IEnginePluginFactory
    {
        object CreateEnginePlugin(IEngine engine, Type type);
        IEnumerable<Type> Types();
    }
}
