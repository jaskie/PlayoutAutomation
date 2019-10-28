using ComponentModelRPC.Server;
using Newtonsoft.Json;
using TAS.Common.Interfaces;

namespace TAS.Server.Model
{
    public class RouterPort : DtoBase, IRouterPort
    {
        private short _portId;
        private string _portName;
        private bool? _portIsSignalPresent;

        public RouterPort() {}

        public RouterPort(short id)
        {
            _portId = id;          
        }

        public RouterPort(short id, string name)
        {
            _portId = id;
            _portName = name;
        }

        public RouterPort(short id, bool isSignalPresent)
        {
            _portId = id;            
            _portIsSignalPresent = isSignalPresent;
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
        public bool? PortIsSignalPresent
        {
            get => _portIsSignalPresent;
            set => SetField(ref _portIsSignalPresent, value);
        }      
    }
}
