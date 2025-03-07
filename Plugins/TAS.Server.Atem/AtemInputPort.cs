using jNet.RPC;
using LibAtem.Common;
using TAS.Common.Interfaces;

namespace TAS.Server
{
    [DtoType(typeof(IRouterPort))]
    class AtemInputPort : jNet.RPC.Server.ServerObjectBase, IRouterPort
    {
        private string _portName;
        private VideoSource _videoSource;

        public AtemInputPort(LibAtem.Commands.Settings.InputPropertiesGetCommand command)
        {
            _videoSource = command.Id;
            _portName = $"{command.LongName} ({command.ExternalPortType})";
        }

        internal void UpdateName(LibAtem.Commands.Settings.InputPropertiesGetCommand command)
        {
            _portName = $"{command.LongName} ({command.ExternalPortType})";
            NotifyPropertyChanged(nameof(PortName));
        }

        internal VideoSource VideoSource => _videoSource;

        [DtoMember]
        public int PortId => (int)VideoSource;

        [DtoMember]
        public string PortName => _portName;

        [DtoMember]
        public bool? IsSignalPresent => null;

    }
}
