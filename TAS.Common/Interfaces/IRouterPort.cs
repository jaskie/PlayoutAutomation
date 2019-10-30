using System.ComponentModel;

namespace TAS.Common.Interfaces
{
    public interface IRouterPort : INotifyPropertyChanged
    {
        short PortId { get; }
        string PortName { get; }
        bool? IsSignalPresent { get; }
    }
}
