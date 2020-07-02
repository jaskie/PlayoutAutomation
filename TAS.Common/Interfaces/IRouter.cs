using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TAS.Common.Interfaces
{
    public interface IRouter : IDisposable, INotifyPropertyChanged, IPlugin
    {
        void Connect();
        IList<IRouterPort> InputPorts { get; }
        short[] OutputPorts { get; }
        void SelectInput(int inputId);       
        IRouterPort SelectedInputPort { get; }
        bool IsConnected { get; }        
        string IpAddress { get; }
        string Login { get; }
        string Password { get; }
        int Level { get; }
    }
}
