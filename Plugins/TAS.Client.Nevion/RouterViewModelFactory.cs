using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Client.Common.Plugin;

namespace TAS.Client.Router
{
    [Export(typeof(IUiPluginFactory))]
    public class RouterViewModelFactory: IUiPluginFactory
    {
        public object CreateNew(IUiPluginContext context)
        {
            return new RouterViewModel();
        }
        
        public Type Type { get; } = typeof(RouterViewModel);
    }
}
