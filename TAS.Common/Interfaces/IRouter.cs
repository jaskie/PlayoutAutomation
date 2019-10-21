using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace TAS.Common.Interfaces
{
    public interface IRouter : IDisposable, INotifyPropertyChanged
    {
        IList<IRouterPort> InputPorts { get; }
        void SelectInput(int inputId);       
        IRouterPort SelectedInputPort { get; }
    }
}
