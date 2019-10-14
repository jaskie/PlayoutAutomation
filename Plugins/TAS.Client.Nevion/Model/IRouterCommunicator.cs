using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TAS.Common;

namespace TAS.Client.Router.Model
{
    public interface IRouterCommunicator : IDisposable
    {
        Task<bool> Connect(string ip, int port);       
        bool SwitchInput(RouterPort inPort, IEnumerable<RouterPort> outPorts);
        
        bool RequestInputPorts();
        bool RequestOutputPorts();

        bool RequestCurrentInputPort();

        event EventHandler<RouterEventArgs> OnInputPortsListReceived;
        event EventHandler<RouterEventArgs> OnInputPortChangeReceived;

        event EventHandler<RouterEventArgs> OnOutputPortsListReceived;
        event EventHandler<RouterEventArgs> OnOutputPortChangeReceived;
    }
}
