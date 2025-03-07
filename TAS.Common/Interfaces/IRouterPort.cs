using System.ComponentModel;

namespace TAS.Common.Interfaces
{
    public interface IRouterPort : INotifyPropertyChanged
    {
        int PortId { get; }
        string PortName { get; }
        bool? IsSignalPresent { get; }
    }
}
