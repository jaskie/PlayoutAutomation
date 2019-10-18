using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TAS.Server.Router.Model;
using TAS.Common;

namespace TAS.Server.Router.RouterCommunicators
{
    class BlackmagicSVHCommunicator : IRouterCommunicator
    {
        public event EventHandler<RouterEventArgs> OnInputPortsListReceived;
        public event EventHandler<RouterEventArgs> OnInputPortChangeReceived;
        public event EventHandler<RouterEventArgs> OnInputSignalPresenceListReceived;
        public event EventHandler<RouterEventArgs> OnOutputPortsListReceived;
        public event EventHandler<RouterEventArgs> OnOutputPortChangeReceived;

        public bool Connect(string ip, int port)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void RequestCurrentInputPort()
        {
            throw new NotImplementedException();
        }

        public void RequestInputPorts()
        {
            throw new NotImplementedException();
        }

        public void RequestOutputPorts()
        {
            throw new NotImplementedException();
        }

        public void RequestSignalPresence()
        {
            throw new NotImplementedException();
        }

        public void SwitchInput(RouterPort inPort, IEnumerable<RouterPort> outPorts)
        {
            throw new NotImplementedException();
        }
    }
}
