using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TAS.Client.Router.Model;
using TAS.Common;

namespace TAS.Client.Router.RouterCommunicators
{
    class BlackmagicSVHCommunicator : IRouterCommunicator
    {
        public event EventHandler<RouterEventArgs> OnInputPortsListReceived;
        public event EventHandler<RouterEventArgs> OnInputPortChangeReceived;
        public event EventHandler<RouterEventArgs> OnOutputPortsListReceived;
        public event EventHandler<RouterEventArgs> OnOutputPortChangeReceived;

        public async Task<bool> Connect(string ip, int port)
        {
            throw new NotImplementedException();
        }

        public void Disconnect()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public bool RequestInputPorts()
        {
            throw new NotImplementedException();
        }

        public bool RequestOutputPorts()
        {
            throw new NotImplementedException();
        }

        public bool SwitchInput(RouterPort inPort, IEnumerable<RouterPort> outPorts)
        {
            throw new NotImplementedException();
        }
    }
}
