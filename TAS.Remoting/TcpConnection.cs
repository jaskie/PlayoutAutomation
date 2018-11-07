using System;
using System.Collections.Concurrent;
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

        private readonly BlockingCollection<byte[]> _sendQueue =
            new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>(), 0x100);

        private Thread _readThread;
        private Thread _writeThread;

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
        }

        public void Send(byte[] bytes)
        {
            try
            {
                if (_sendQueue.TryAdd(bytes))
                    return;
                Logger.Error("Message queue overflow");
            }
            catch (Exception e) when (e is ObjectDisposedException || e is InvalidOperationException)
            {
                // only disconnect
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        public bool IsConnected { get; private set; } = true;
        
        protected virtual void OnDispose()
        {
            IsConnected = false;
            _sendQueue.Dispose();
            Client.Client.Close();
            _readThread.Abort();
            _writeThread.Abort();
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
            while (true)
            {
                try
                {
                    var bytes = _sendQueue.Take();
                    Client.Client.NoDelay = false;
                    Client.Client.Send(BitConverter.GetBytes(bytes.Length));
                    Client.Client.NoDelay = true;
                    Client.Client.Send(bytes);
                }
                catch (Exception e) when (e is IOException || e is ThreadAbortException || e is ArgumentNullException ||
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
            while (true)
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
                        //Debug.WriteLine($"R2:  {receivedLength}");
                        dataIndex += receivedLength;
                        if (dataIndex != dataBuffer.Length)
                            continue;
                        OnMessage(dataBuffer);
                        dataBuffer = null;
                    }
                }
                catch (Exception e) when (e is IOException || e is ThreadAbortException ||
                                          e is ObjectDisposedException || e is SocketException)
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
