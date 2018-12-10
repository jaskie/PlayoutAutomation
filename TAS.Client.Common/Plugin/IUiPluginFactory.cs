using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAS.Client.Common.Plugin
{
    public interface IUiPluginFactory
    {
        object CreateNew(IUiPluginContext context);
        Type Type { get; }
    }
}
