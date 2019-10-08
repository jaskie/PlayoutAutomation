using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Client.Common.Plugin;

namespace TAS.Client.Nevion
{
    [Export(typeof(IUiPluginFactory))]
    public class NevionViewModelFactory: IUiPluginFactory
    {
        public object CreateNew(IUiPluginContext context)
        {
            return new NevionViewModel();
        }
        
        public Type Type { get; } = typeof(NevionViewModel);
    }
}
