using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAS.Common.Interfaces
{
    public interface INevionRouter
    {
        bool Connect();
        bool Disconnect();
        bool SwitchInput();
        IEnumerable<INevionPort> InputPorts { get; set; }
        IEnumerable<INevionPort> OutputPorts { get; set; }
    }
}
