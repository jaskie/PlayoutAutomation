using System.Collections.Generic;
using System.ComponentModel;
namespace TAS.Common.Interfaces
{
    public interface IRouter : INotifyPropertyChanged
    {
        IList<IRouterPort> InputPorts { get; }
        void SelectInputPort(int inputId);
        IRouterPort SelectedInputPort { get; }
        bool SwitchOnLoad { get; }
        bool IsConnected { get; }
    }
}
