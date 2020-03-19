using jNet.RPC.Server;
using Newtonsoft.Json;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    public class RouterPort : ServerObjectBase, IRouterPort
    {
        private short _portId;
        private string _portName;
        private bool? _isSignalPresent;

        public RouterPort(short id, string portName)
        {
            _portId = id;
            PortName = portName;
        }

        [JsonProperty]
        public short PortId
        { 
            get => _portId; 
            set => SetField(ref _portId, value);
        }
        
        [JsonProperty]
        public string PortName
        {
            get => _portName;
            set => SetField(ref _portName, value);
        }

        [JsonProperty]
        public bool? IsSignalPresent
        {
            get => _isSignalPresent;
            set => SetField(ref _isSignalPresent, value);
        }      
    }
}
