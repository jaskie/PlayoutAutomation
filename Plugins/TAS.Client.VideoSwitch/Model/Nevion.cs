using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Server.VideoSwitch.Communicators;
using TAS.Server.VideoSwitch.Model.Interfaces;

namespace TAS.Server.VideoSwitch.Model
{
    public class Nevion : Router
    {
        protected override IRouterCommunicator CreateCommunicator()
        {
            return new NevionCommunicator();
        }
    }
}
