using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TAS.Common.Interfaces
{
    public interface IVideoSwitch : IDisposable, INotifyPropertyChanged, IPlugin
    {
        void Connect();
        IList<IVideoSwitchPort> InputPorts { get; }        
        void SelectInput(int inputId);       
        IVideoSwitchPort SelectedInputPort { get; }
        bool IsConnected { get; }               
    }
}
