using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAS.Client.XKeys
{
    public class DeviceEventArgs: EventArgs
    {
        public Device Device { get; }

        public DeviceEventArgs(Device device)
        {
            Device = device;
        }
    }
}
