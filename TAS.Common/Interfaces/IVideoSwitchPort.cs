using System.ComponentModel;

namespace TAS.Common.Interfaces
{
    public interface IVideoSwitchPort : INotifyPropertyChanged
    {
        short PortId { get; }
        string PortName { get; }
        bool? IsSignalPresent { get; }
    }
}
