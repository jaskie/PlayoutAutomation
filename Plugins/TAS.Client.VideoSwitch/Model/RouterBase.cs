using System.ComponentModel;
using System.Linq;
using TAS.Common;
using TAS.Database.Common;

namespace TAS.Server.VideoSwitch.Model
{
    public abstract class RouterBase : VideoSwitchBase
    {
        private PortInfo[] _allOutputPorts;

        protected RouterBase(int defaultPort) : base(defaultPort)
        { }

        public PortInfo[] AllOutputs { get => _allOutputPorts; protected set => SetField(ref _allOutputPorts, value); }

        [Hibernate]
        public short[] Outputs { get; set; }
    }
}
