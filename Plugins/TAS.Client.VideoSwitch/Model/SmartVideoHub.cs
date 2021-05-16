using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Common.Interfaces;

namespace TAS.Server.VideoSwitch.Model
{
    public class SmartVideoHub: RouterBase
    {
        public SmartVideoHub() : base(CommunicatorType.BlackmagicSmartVideoHub) { }
    }
}
