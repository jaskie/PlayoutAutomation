using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Client.Common;

namespace TAS.Client.Config.Model.Plugins
{
    public interface IPluginManager
    {
        string PluginName { get; }           
    }
}
