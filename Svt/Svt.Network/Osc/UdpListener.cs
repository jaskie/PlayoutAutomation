using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Svt.Network.Osc
{
    internal class UdpListener : IDisposable
    {
        public int Port { get; private set; }
        public bool IsConnected { get; private set; } = false;
        
        UdpClient receivingUdpClient;
        IPEndPoint RemoteIpEndPoint;

        ManualResetEvent ClosingEvent = new ManualResetEvent(false);
        private HashSet<Action<OscPacketEventArgs>> _deletates = new HashSet<Action<OscPacketEventArgs>>();

        internal void AddDelegate(Action<OscPacketEventArgs> packetReceivedDelegate)
        {
            lock (callbackLock)
                _deletates.Add(packetReceivedDelegate);
        }

        internal void RemoveDelegate(Action<OscPacketEventArgs> packetReceivedDelegate)
        {
            lock (callbackLock)
                _deletates.Remove(packetReceivedDelegate);
        }

        object callbackLock = new object();

        public UdpListener(int port)
        {
            Port = port;

            try
            {
                receivingUdpClient = new UdpClient();
                receivingUdpClient.ExclusiveAddressUse = false;
                receivingUdpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                receivingUdpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));
            }
            catch (Exception)
            {
                
            }
            RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            // setup first async event
            AsyncCallback callBack = new AsyncCallback(ReceiveCallback);
            receivingUdpClient.BeginReceive(callBack, null);
            IsConnected = true;
        }

        internal int DelegatesCount()
        {
            lock (callbackLock)
                return _deletates.Count();
        }

        void ReceiveCallback(IAsyncResult result)
        {
            lock (callbackLock)
            {
                Byte[] bytes = null;

                try
                {
                    bytes = receivingUdpClient.EndReceive(result, ref RemoteIpEndPoint);
                }
                catch (ObjectDisposedException)
                {
                    // Ignore if disposed. This happens when closing the listener
                }

                if (closing)
                    ClosingEvent.Set();
                else
                {
                    // Setup next async event
                    AsyncCallback callBack = new AsyncCallback(ReceiveCallback);
                    receivingUdpClient.BeginReceive(callBack, null);
                }

                // Process bytes
                if (bytes?.Length > 0)
                {
                    OscPacket packet = null;
                    try
                    {
                        packet = OscPacket.GetPacket(bytes);
                    }
                    catch (Exception)
                    {
                        // If there is an error reading the packet, no packet will be sent
                    }
                    if (packet != null)
                        foreach (var del in _deletates)
                        {
                            del(new OscPacketEventArgs(packet, RemoteIpEndPoint.Address));
                        }
                }
            }
        }

        bool closing = false;
        private void Close()
        {
            lock (callbackLock)
            {
                ClosingEvent.Reset();
                closing = true;
                receivingUdpClient.Close();
            }
            ClosingEvent.WaitOne();
        }

        public void Dispose()
        {
            this.Close();
        }

    }
}
