using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Server.VideoSwitch.Communicators;
using TAS.Server.VideoSwitch.Model.Interfaces;

namespace TAS.Server.VideoSwitch.Model
{
    public class SmartVideoHub: RouterBase
    {
        protected override void Communicator_SourceChanged(object sender, EventArgs<CrosspointInfo> e)
        {
            
        }

        protected override IRouterCommunicator CreateCommunicator()
        {
            return new SmartVideoHubCommunicator();
        }



    }
}
