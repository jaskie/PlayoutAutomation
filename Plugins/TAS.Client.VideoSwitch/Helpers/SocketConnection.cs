using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TAS.Server.VideoSwitch.Helpers
{

    /// <inheritdoc />
    /// <summary>
    /// Class to ensure non-blocking send and preserving order of messages
    /// </summary>
    public abstract class SocketConnection : jNet.RPC.Server.ServerObjectBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private bool _isConnected;
        private readonly BlockingCollection<byte[]> _sendQueue = new BlockingCollection<byte[]>(0x100);

        private Thread _readThread;
        private Thread _writeThread;
        private TcpClient _client;
        private readonly int _defaultPort;
        private string _address;

        protected CancellationTokenSource DisconnectTokenSource;

        protected SocketConnection(int defaultPort)
        {
            _defaultPort = defaultPort;
        }

        /// <summary>
        /// Connects to specified address
        /// </summary>
        /// <param name="address">address and port to connect to</param>
        /// <returns></returns>
        protected virtual async Task Connect(string address, CancellationToken cancellationToken)
        {
            Disconnect();
            _address = address;
            int port = _defaultPort;
            var addressParts = address.Split(':');
            if (addressParts.Length > 1)
                int.TryParse(addressParts[1], out port);

            _client = new TcpClient
            {
                NoDelay = true,
            };
            var disconnectTokenSource = new CancellationTokenSource();
            try
            {
                Logger.Info("Connecting to {0}:{1}", addressParts[0], port);
                await Task.Run(() => _client.Connect(addressParts[0], port), cancellationToken);
                DisconnectTokenSource = disconnectTokenSource;
                Logger.Info("Connected to {0}:{1}", addressParts[0], port);
                StartThreads();
                IsConnected = true;
            }
            catch (SocketException)
            { }
            catch
            {
                if (!disconnectTokenSource.IsCancellationRequested)
                    disconnectTokenSource.Cancel();
            }
        }

        public bool IsConnected { get => _isConnected; private set => SetField(ref _isConnected, value); }

        protected void Send(byte[] message)
        {
            var disconnectTokenSource = DisconnectTokenSource;
            if (!IsConnected || disconnectTokenSource is null)
                return;
            try
            {
                if (!_sendQueue.TryAdd(message))
                {
                    Logger.Error("Message queue overflow with message {0}", message);
                    if (!disconnectTokenSource.IsCancellationRequested)
                        disconnectTokenSource.Cancel();
                    return;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                if (!disconnectTokenSource.IsCancellationRequested)
                    disconnectTokenSource.Cancel();
            }
        }

        public virtual void Disconnect()
        {
            if (!IsConnected)
                return;
            Logger.Info("Disconnected from {0}", _address);
            var tokenSource = DisconnectTokenSource;
            if (tokenSource?.IsCancellationRequested == false)
                tokenSource.Cancel();
            _client?.Client?.Dispose();
            _readThread?.Join();
            _writeThread?.Join();
            tokenSource?.Dispose();
            DisconnectTokenSource = null;
            IsConnected = false;
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            Disconnect();
            _sendQueue.Dispose();
        }

        protected void StartThreads()
        {
            _readThread = new Thread(ReadThreadProc)
            {
                IsBackground = true,
                Name = $"TCP read thread for {_client.Client.RemoteEndPoint}"
            };
            _readThread.Start();

            _writeThread = new Thread(WriteThreadProc)
            {
                IsBackground = true,
                Name = $"TCP write thread for {_client.Client.RemoteEndPoint}"
            };
            _writeThread.Start();            
        }

        private void WriteThreadProc()
        {
            var disconnectTokenSource = DisconnectTokenSource;
            while (!disconnectTokenSource.IsCancellationRequested)
            {
                try
                {
                    var serializedMessage = _sendQueue.Take(disconnectTokenSource.Token);
                    Logger.Trace("Message sent: {0}", BitConverter.ToString(serializedMessage));
                    _client.Client.Send(serializedMessage);
                }
                catch (Exception e) when (e is IOException || e is ObjectDisposedException || e is SocketException || e is OperationCanceledException)
                {

                    if (!disconnectTokenSource.IsCancellationRequested)
                        disconnectTokenSource.Cancel();
                    return;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Write thread unexpected exception");
                }
            }
        }

        private void ReadThreadProc()
        {
            var stream = _client.GetStream();
            var buffer = new byte[256];
            var disconnectTokenSource = DisconnectTokenSource;
            while (!disconnectTokenSource.IsCancellationRequested)
            {
                try
                {
                    var receivedCount = stream.Read(buffer, 0, buffer.Length);
                    if (receivedCount == 0)
                    {
                        if (!disconnectTokenSource.IsCancellationRequested)
                            disconnectTokenSource.Cancel();
                        return;
                    }
                    var response = new byte[receivedCount];
                    Buffer.BlockCopy(buffer, 0, response, 0, receivedCount);
                    OnMessageReceived(response);
                }
                catch (Exception e) when (e is IOException || e is ObjectDisposedException || e is SocketException)
                {
                    if (!disconnectTokenSource.IsCancellationRequested)
                        disconnectTokenSource.Cancel();
                    return;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Read thread unexpected exception");
                }
            }
        }

        protected abstract void OnMessageReceived(byte[] message);

    }
}
