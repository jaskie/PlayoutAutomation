using System;
using System.ComponentModel;
using System.Threading.Tasks;
using TAS.Common;

namespace TAS.Server.VideoSwitch.Model.Interfaces
{
    public interface IRouterCommunicator : INotifyPropertyChanged, IDisposable
    {
        PortInfo[] Sources { get; set; }
        Task<bool> ConnectAsync();       
        void SetSource(int inPort);                             
        Task<CrosspointInfo> GetSelectedSource();

        event EventHandler<EventArgs<CrosspointInfo>> SourceChanged;        
        event EventHandler<EventArgs<bool>> ConnectionChanged;
    }
}
