using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Svt.Network.Osc
{
    public static class OscPacketDispatcher
    {

        public static void Bind(int port, Action<OscPacketEventArgs> packetReceivedDelegate)
        {
            UdpListener listener = null;
            if (!_listeners.TryGetValue(port, out listener))
            {
                listener = new UdpListener(port);
                _listeners[port] = listener;
            }
            listener.AddDelegate(packetReceivedDelegate);
        }

        public static bool UnBind(int port, Action<OscPacketEventArgs> packetReceivedDelegate)
        {
            UdpListener listener = null;
            if (_listeners.TryGetValue(port, out listener))
            {
                listener.RemoveDelegate(packetReceivedDelegate);
                if (listener.DelegatesCount() == 0 && _listeners.TryRemove(port, out listener))
                    listener.Dispose();
                return true;
            }
            return false;
        }
        
        private static ConcurrentDictionary<int, UdpListener> _listeners = new ConcurrentDictionary<int, UdpListener>();
                
    }
}
