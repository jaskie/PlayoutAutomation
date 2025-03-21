using jNet.RPC;
using jNet.RPC.Server;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    [DtoType(typeof(IRouterPort))]
    public class RouterPort : ServerObjectBase, IRouterPort
    {
        private int _portId;
        private string _portName;
        private bool? _isSignalPresent;

        public RouterPort(int id, string portName)
        {
            _portId = id;
            PortName = portName;
        }

        [DtoMember]
        public int PortId
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
