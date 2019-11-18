using System;
using System.Threading.Tasks;
using TAS.Common;

namespace TAS.Server.Model
{
    internal interface IRouterCommunicator : IDisposable
    {
        Task<bool> Connect();       
        void SelectInput(int inPort);
        
        Task<PortInfo[]> GetInputPorts();               
        Task<CrosspointInfo> GetCurrentInputPort();

        event EventHandler<EventArgs<CrosspointInfo>> OnInputPortChangeReceived;
        event EventHandler<EventArgs<PortState[]>> OnRouterPortsStatesReceived;
        event EventHandler<EventArgs<bool>> OnRouterConnectionStateChanged;
    }
}
