using System;
using System.ComponentModel;
using System.Threading;
using TAS.Common;

namespace TAS.Server.VideoSwitch.Model.Interfaces
{
    public interface IRouterCommunicator : INotifyPropertyChanged, IDisposable
    {
        PortInfo[] Sources { get; set; }
        void Connect(string address, CancellationToken cancellationToken);
        void Disconnect();
        void SetSource(int inPort);                             
        CrosspointInfo GetSelectedSource();

        bool IsConnected { get; }


        event EventHandler<EventArgs<CrosspointInfo>> SourceChanged;        
    }
}
