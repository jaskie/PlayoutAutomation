using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Database.Common;
using TAS.Server.VideoSwitch.Communicators;
using TAS.Server.VideoSwitch.Model.Interfaces;

namespace TAS.Server.VideoSwitch.Model
{
    public class Nevion : RouterBase
    {

        [Hibernate]
        public int Level { get; set; }

        protected override IRouterCommunicatorBase CreateCommunicator()
        {
            return new NevionCommunicator { OutputPorts = this.OutputPorts };
        }
    }
}
