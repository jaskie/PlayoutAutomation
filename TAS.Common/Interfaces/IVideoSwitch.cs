using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TAS.Common.Interfaces
{
    public interface IVideoSwitch : IDisposable, INotifyPropertyChanged, IPlugin, IStartGpi
    {        
        bool Connect();
        List<IVideoSwitchPort> Sources { get; }        
        void SetSource(int inputId);       
        IVideoSwitchPort SelectedSource { get; }
        bool IsConnected { get; }           
        bool Preload { get; }                
    }
}
