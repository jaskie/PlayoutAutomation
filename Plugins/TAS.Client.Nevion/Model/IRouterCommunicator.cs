using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TAS.Common;

namespace TAS.Server.Router.Model
{
    public interface IRouterCommunicator : IDisposable
    {
        bool Connect(string ip, int port);       
        void SwitchInput(RouterPort inPort, IEnumerable<RouterPort> outPorts);
        
        void RequestInputPorts();
        void RequestOutputPorts();
        void RequestSignalPresence();
        void RequestCurrentInputPort();

        event EventHandler<RouterEventArgs> OnInputPortsListReceived;
        event EventHandler<RouterEventArgs> OnInputPortChangeReceived;
        event EventHandler<RouterEventArgs> OnInputSignalPresenceListReceived;

        event EventHandler<RouterEventArgs> OnOutputPortsListReceived;
        event EventHandler<RouterEventArgs> OnOutputPortChangeReceived;
    }
}
