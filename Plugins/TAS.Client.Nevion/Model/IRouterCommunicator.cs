using System;
using System.Threading.Tasks;
using TAS.Common;

namespace TAS.Server.Model
{
    interface IRouterCommunicator : IDisposable
    {
        Task<bool> Connect();       
        void SelectInput(int inPort);
        
        void RequestInputPorts();
        void RequestOutputPorts();
        void RequestRouterState();
        void RequestCurrentInputPort();       

        event EventHandler<EventArgs<PortInfo[]>> OnInputPortsReceived;
        event EventHandler<EventArgs<CrosspointInfo[]>> OnInputPortChangeReceived;
        event EventHandler<EventArgs<PortState[]>> OnRouterStateReceived;
        event EventHandler<EventArgs<bool>> OnRouterConnectionStateChanged;
    }
}
