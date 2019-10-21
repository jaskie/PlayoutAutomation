using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TAS.Server.Router.Model;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Server.Router.RouterCommunicators
{
    class BlackmagicSVHCommunicator : IRouterCommunicator
    {
        public event EventHandler<EventArgs<IEnumerable<IRouterPort>>> OnInputPortsListReceived;
        public event EventHandler<EventArgs<IEnumerable<IRouterPort>>> OnInputPortChangeReceived;
        public event EventHandler<EventArgs<IEnumerable<IRouterPort>>> OnInputSignalPresenceListReceived;
        public event EventHandler<EventArgs<bool>> OnRouterConnectionStateChanged;
        public event EventHandler<EventArgs<IEnumerable<IRouterPort>>> OnOutputPortsListReceived;

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

        public void SelectInput(int inPort, IEnumerable<IRouterPort> outPorts)
        {
            throw new NotImplementedException();
        }
    }
}
