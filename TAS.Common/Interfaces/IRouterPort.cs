using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAS.Common.Interfaces
{
    public interface IRouterPort : INotifyPropertyChanged
    {
        int PortID { get; set; }
        string PortName { get; set; }
        bool? PortIsSignalPresent { get; set; }
    }
}
