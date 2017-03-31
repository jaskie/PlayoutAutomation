using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Svt.Network.Osc
{
    public class UdpListener : IDisposable
    {
        public int Port { get; private set; }
        public bool IsConnected { get; private set; } = false;
        public IPAddress[] RemoteAddreses { get; private set; }
        
        UdpClient receivingUdpClient;
        IPEndPoint RemoteIpEndPoint;

        public event EventHandler<OscPacketEventArgs> PacketReceived;
        public event EventHandler<OscBytesEventArgs> BytestReceived;

        ManualResetEvent ClosingEvent = new ManualResetEvent(false);
        object callbackLock = new object();

        public bool Connect (string hostname, int port)
        {
            Port = port;
            RemoteAddreses = Dns.GetHostAddresses(hostname);

            // try to open the port 10 times, else fail
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    receivingUdpClient = new UdpClient();
                    receivingUdpClient.ExclusiveAddressUse = false;
                    receivingUdpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    receivingUdpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));
                    break;
                }
                catch (Exception)
                {
                    // Failed in ten tries, throw the exception and give up
                    if (i >= 9)
                        return false;

                    Thread.Sleep(5);
                }
            }
            RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

            // setup first async event
            AsyncCallback callBack = new AsyncCallback(ReceiveCallback);
            receivingUdpClient.BeginReceive(callBack, null);
            IsConnected = true;
            return true;
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
                if (bytes?.Length > 0 && RemoteAddreses.Any(a => a.Equals(RemoteIpEndPoint.Address)))
                {
                    BytestReceived?.Invoke(this, new OscBytesEventArgs(bytes));
                    var packetCallback = PacketReceived;
                    if (packetCallback != null)
                    {
                        OscPacket packet = null;
                        try
                        {
                            packet = OscPacket.GetPacket(bytes);
                        }
                        catch (Exception)
                        {
                            // If there is an error reading the packet, null is sent to the callback
                        }
                        if (packet != null)
                            packetCallback.Invoke(this, new OscPacketEventArgs(packet));
                    }
                }
            }
        }

        bool closing = false;
        public void Close()
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
