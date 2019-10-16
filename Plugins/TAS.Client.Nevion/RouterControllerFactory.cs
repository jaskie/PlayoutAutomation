using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Common.Interfaces;

namespace TAS.Server.Router
{
    [Export(typeof(IEnginePluginFactory))]
    public class RouterControllerFactory: IEnginePluginFactory
    {       
        public object CreateEnginePlugin(IEngine engine)
        {
            return new RouterController();
        }

        public Type Type { get; } = typeof(RouterController);
    }
}
