using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAS.Server.VideoSwitch.Model
{
    public class Ross: VideoSwitcher
    {
        public Ross() : base(CommunicatorType.Ross) { }
    }
}
