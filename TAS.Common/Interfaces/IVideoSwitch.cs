using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace TAS.Common.Interfaces
{
    public interface IVideoSwitch : IDisposable, INotifyPropertyChanged, IPlugin, IStartGpi
    {        
        void Connect(CancellationToken cancellationToken);
        void Disconnect();
        List<IVideoSwitchPort> Sources { get; }        
        void SetSource(int inputId);       
        IVideoSwitchPort SelectedSource { get; }
        bool IsConnected { get; }           
        bool Preload { get; }                
    }
}
