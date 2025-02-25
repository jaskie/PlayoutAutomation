using System.Collections.Generic;
using System.ComponentModel;
namespace TAS.Common.Interfaces
{
    public interface IRouter : INotifyPropertyChanged
    {
        IList<IRouterPort> InputPorts { get; }
        void SelectInput(int inputId);
        IRouterPort SelectedInputPort { get; }
        bool IsConnected { get; }
    }
}
