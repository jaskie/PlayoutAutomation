using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace TAS.Remoting
{

    /// <inheritdoc />
    /// <summary>
    /// Class to ensure non-blocking send and preserving order of messages
    /// </summary>
    public abstract class TcpConnection : IDisposable
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(TcpConnection));
        private int _disposed;

        private readonly List<byte[]> _sendQueue = new List<byte[]>();
        private const int MaxQueueSize = 0x1000;

        private Thread _readThread;
        private Thread _writeThread;
        private readonly AutoResetEvent _sendAutoResetEvent = new AutoResetEvent(false);


        public TcpClient Client { get; }

        protected TcpConnection(TcpClient client)
        {
            Client = client;
            client.NoDelay = true;
        }

        protected TcpConnection(string address)
        {
            var port = 1060;
            var addressParts = address.Split(':');
            if (addressParts.Length > 1)
                int.TryParse(addressParts[1], out port);
            Client = new TcpClient
            {
                NoDelay = true,
            };
            Client.Connect(addressParts[0], port);
            Logger.Info("Connection opened to {0}:{1}.", addressParts[0], port);
        }

        public void Send(byte[] bytes)
        {
            if (!IsConnected)
                return;
            try
            {
                lock (((ICollection) _sendQueue).SyncRoot)
                    if (_sendQueue.Count < MaxQueueSize)
                    {
                        _sendQueue.Add(bytes);
                        _sendAutoResetEvent.Set();
                        return;
                    }
                Logger.Error("Message queue overflow");
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            NotifyDisconnection();
        }

        public bool IsConnected { get; private set; } = true;
        
        protected virtual void OnDispose()
        {
            IsConnected = false;
            Client.Client.Close();
            _sendAutoResetEvent.Set();
            _sendAutoResetEvent.Dispose();
            Logger.Info("Connection closed.");
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != default(int))
                return;
            OnDispose();
        }

        public void StartThreads()
        {
            _readThread = new Thread(ReadThreadProc)
            {
                IsBackground = true,
                Name = $"TCP read thread for {Client.Client.RemoteEndPoint}"
            };
            _readThread.Start();
            _writeThread = new Thread(WriteThreadProc)
            {
                IsBackground = true,
                Name = $"TCP write thread for {Client.Client.RemoteEndPoint}"
            };
            _writeThread.Start();
        }

        public event EventHandler Disconnected;

        private void WriteThreadProc()
        {
            while (IsConnected)
            {
                try
                {
                    _sendAutoResetEvent.WaitOne();
                    byte[][] sendPackets;
                    lock (((ICollection) _sendQueue).SyncRoot)
                    {
                        sendPackets = _sendQueue.ToArray();
                        _sendQueue.Clear();
                    }
                    foreach (var bytes in sendPackets)
                    {
                        Client.Client.NoDelay = false;
                        Client.Client.Send(BitConverter.GetBytes(bytes.Length));
                        Client.Client.NoDelay = true;
                        Client.Client.Send(bytes);
                    }
                }
                catch (Exception e) when (e is IOException || e is ArgumentNullException ||
                                          e is ObjectDisposedException || e is SocketException)
                {
                    NotifyDisconnection();
                    return;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Write thread unexpected exception");
                }
            }
        }

        protected virtual void ReadThreadProc()
        {
            var stream = Client.GetStream();
            byte[] dataBuffer = null;
            var sizeBuffer = new byte[sizeof(int)];
            var dataIndex = 0;
            while (IsConnected)
            {
                try
                {
                    if (dataBuffer == null)
                    {
                        if (stream.Read(sizeBuffer, 0, sizeof(int)) == sizeof(int))
                        {
                            var dataLength = BitConverter.ToInt32(sizeBuffer, 0);
                            dataBuffer = new byte[dataLength];
                        }
                        dataIndex = 0;
                    }
                    else
                    {
                        var receivedLength = stream.Read(dataBuffer, dataIndex, dataBuffer.Length - dataIndex);
                        dataIndex += receivedLength;
                        if (dataIndex != dataBuffer.Length)
                            continue;
                        OnMessage(dataBuffer);
                        dataBuffer = null;
                    }
                }
                catch (Exception e) when (e is IOException || e is ObjectDisposedException || e is SocketException)
                {
                    NotifyDisconnection();
                    return;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Read thread unexpected exception");
                }
            }
        }

        private void NotifyDisconnection()
        {
            if (!IsConnected)
                return;
            Disconnected?.Invoke(this, EventArgs.Empty);
            Debug.WriteLine("Disconnected");
        }

        protected abstract void OnMessage(byte[] dataBuffer);
        
    }
}
