using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace TAS.Common.Interfaces
{
    public interface IVideoSwitch : IDisposable, INotifyPropertyChanged, IPlugin, IStartGpi
    {        
        Task<bool> ConnectAsync();
        IList<IVideoSwitchPort> Sources { get; }        
        void SetSource(int inputId);       
        IVideoSwitchPort SelectedSource { get; }
        bool IsConnected { get; }           
        bool Preload { get; }                
    }
}
