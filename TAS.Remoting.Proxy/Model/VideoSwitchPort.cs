using jNet.RPC;
using jNet.RPC.Client;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    class VideoSwitchPort : ProxyObjectBase, IVideoSwitchPort
    {
        #pragma warning disable CS0649
        [DtoMember(nameof(IVideoSwitchPort.Id))]
        private short _portId;
        [DtoMember(nameof(IVideoSwitchPort.Name))]
        private string _portName;
        [DtoMember(nameof(IVideoSwitchPort.IsSignalPresent))]
        private bool? _portIsSignalPresent;
        #pragma warning restore

        public short Id => _portId;
        public string Name => _portName;
        public bool? IsSignalPresent => _portIsSignalPresent;

        protected override void OnEventNotification(SocketMessage message) { }
    }
}
