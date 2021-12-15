using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Server.VideoSwitch.Model.Interfaces;

namespace TAS.Server.VideoSwitch.Model
{
    public class Atem: VideoSwitcher
    {
        public Atem() : base() { }

        protected override IRouterCommunicatorBase CreateCommunicator()
        {
            return new Communicators.AtemCommunicator();
        }
    }
}
