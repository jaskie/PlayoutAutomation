using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAS.Common.Interfaces.Configurator
{
    public interface IConfigCasparChannel : IPlayoutServerChannelProperties
    {
        int Id { get; set; }
        object Owner { get; set; }
    }
}
