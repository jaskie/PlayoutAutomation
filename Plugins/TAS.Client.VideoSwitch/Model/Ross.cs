using jNet.RPC;
using TAS.Common.Interfaces;
using TAS.Server.VideoSwitch.Model.Interfaces;

namespace TAS.Server.VideoSwitch.Model
{
    [DtoType(typeof(IVideoSwitcher))]
    public class Ross: VideoSwitcher
    {
        protected override IRouterCommunicator CreateCommunicator()
        {
            return new Communicators.RossCommunicator();
        }
    }
}
