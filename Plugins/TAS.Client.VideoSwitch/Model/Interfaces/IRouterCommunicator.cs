using System;
using System.ComponentModel;
using TAS.Common;

namespace TAS.Server.VideoSwitch.Model.Interfaces
{
    public interface IRouterCommunicator : INotifyPropertyChanged, IDisposable
    {
        PortInfo[] Sources { get; set; }
        bool Connect();       
        void SetSource(int inPort);                             
        CrosspointInfo GetSelectedSource();

        event EventHandler<EventArgs<CrosspointInfo>> SourceChanged;        
        event EventHandler<EventArgs<bool>> ConnectionChanged;
    }
}
