using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace TAS.Remoting
{

    /// <summary>
    /// Class to ensure non-blocking send and preserving order of messages
    /// </summary>
    public abstract class TcpConnection: IDisposable
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(TcpConnection));
        private int _disposed;
        private readonly BlockingCollection<byte[]> _sendQueue = new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>());
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
            _sendQueue.Add(bytes);
        }

        public bool IsConnected { get; private set; } = true;

        protected virtual void OnDispose()
        {
            Client.Client.Close();
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

        public event EventHandler SessionClosed;

        private void WriteThreadProc()
        {
            try
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
                    catch (Exception e) when (e is IOException || e is ThreadAbortException ||
                                              e is ObjectDisposedException || e is SocketException)
                    {
                        _readThread.Abort();
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Read thread unexpected exception");
            }
        }

        protected virtual void ReadThreadProc()
        {
            try
            {
                try
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
                                dataIndex += stream.Read(dataBuffer, dataIndex, dataBuffer.Length - dataIndex);
                                if (dataIndex == dataBuffer.Length)
                                {
                                    OnMessage(dataBuffer);
                                    dataBuffer = null;
                                }
                            }
                        }
                        catch (Exception e) when (e is IOException || e is ThreadAbortException ||
                                                  e is ObjectDisposedException || e is SocketException)
                        {
                            _writeThread.Abort();
                            break;
                        }
                    }
                }
                finally
                {
                    IsConnected = false;
                    SessionClosed?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Read thread unexpected exception");
            }
        }

        protected abstract void OnMessage(byte[] dataBuffer);



    }
}
