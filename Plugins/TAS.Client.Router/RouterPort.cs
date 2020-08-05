using jNet.RPC;
using jNet.RPC.Server;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    public class RouterPort : ServerObjectBase, IVideoSwitchPort
    {
        private short _portId;
        private string _portName;
        private bool? _isSignalPresent;

        public RouterPort(short id, string portName)
        {
            _portId = id;
            PortName = portName;
        }

        [DtoMember]
        public short PortId
        { 
            get => _portId; 
            set => SetField(ref _portId, value);
        }
        
        [DtoMember]
        public string PortName
        {
            get => _portName;
            set => SetField(ref _portName, value);
        }

        [DtoMember]
        public bool? IsSignalPresent
        {
            get => _isSignalPresent;
            set => SetField(ref _isSignalPresent, value);
        }      
    }
}
