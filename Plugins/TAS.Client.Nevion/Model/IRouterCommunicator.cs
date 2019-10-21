using System;
using System.Collections.Generic;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Server.Router.Model
{
    public interface IRouterCommunicator : IDisposable
    {
        bool Connect(string ip, int port);       
        void SelectInput(int inPort, IEnumerable<IRouterPort> outPorts);
        
        void RequestInputPorts();
        void RequestOutputPorts();
        void RequestSignalPresence();
        void RequestCurrentInputPort();

        event EventHandler<EventArgs<IEnumerable<IRouterPort>>> OnInputPortsListReceived;
        event EventHandler<EventArgs<IEnumerable<IRouterPort>>> OnInputPortChangeReceived;
        event EventHandler<EventArgs<IEnumerable<IRouterPort>>> OnInputSignalPresenceListReceived;

        event EventHandler<EventArgs<bool>> OnRouterConnectionStateChanged;

        event EventHandler<EventArgs<IEnumerable<IRouterPort>>> OnOutputPortsListReceived;      
    }
}
