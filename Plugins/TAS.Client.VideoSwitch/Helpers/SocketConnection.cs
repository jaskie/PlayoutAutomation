using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace TAS.Server.VideoSwitch.Helpers
{

    /// <inheritdoc />
    /// <summary>
    /// Class to ensure non-blocking send and preserving order of messages
    /// </summary>
    public abstract class SocketConnection : INotifyPropertyChanged, IDisposable
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private bool _isConnected;
        private readonly BlockingCollection<byte[]> _sendQueue = new BlockingCollection<byte[]>(0x100);
        private readonly BlockingCollection<byte[]> _receiveQueue = new BlockingCollection<byte[]>();

        private Thread _readThread;
        private Thread _writeThread;
        private TcpClient _client;
        private readonly int _defaultPort;

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
        public virtual bool Connect(string address)
        {
            var token = Interlocked.CompareExchange(ref DisconnectTokenSource, null, new CancellationTokenSource());
            if (!(token is null))
            {
                token.Dispose();
                throw new ApplicationException("Can't connect twice");
            }
            int port = _defaultPort;
            var addressParts = address.Split(':');
            if (addressParts.Length > 1)
                int.TryParse(addressParts[1], out port);

            _client = new TcpClient
            {
                NoDelay = true,                
            };

            try
            {
                Logger.Info("Connecting to {0}:{1}", addressParts[0], port);
                _client.Connect(addressParts[0], port);
                Logger.Info("Connected to {0}:{1}", addressParts[0], port);
                StartThreads();
                IsConnected = true;
                return true;
            }
            catch
            {
                Disconnect();
            }
            return false;
        }

        public bool IsConnected { get => _isConnected; private set => SetField(ref _isConnected, value); }

        protected void Send(byte[] message)
        {
            if (!IsConnected)
                return;
            try
            {
                if (!_sendQueue.TryAdd(message))
                {
                    Logger.Error("Message queue overflow with message {0}", message);
                    Disconnect();
                    return;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Disconnect();
            }
        }

        public virtual void Disconnect()
        {
            var tokenSource = Interlocked.Exchange(ref DisconnectTokenSource, null);
            if (!tokenSource.IsCancellationRequested)
            {
                tokenSource.Cancel();
                tokenSource.Dispose();
            }
            _client.Client?.Dispose();
            _readThread?.Join();
            _writeThread?.Join();
            _sendQueue.Dispose();
            _receiveQueue.Dispose();
            tokenSource.Dispose();
            IsConnected = false;
        }

        public virtual void Dispose()
        {
            Disconnect();
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

        public event PropertyChangedEventHandler PropertyChanged;

        private void WriteThreadProc()
        {
            while (!DisconnectTokenSource.IsCancellationRequested)
            {
                try
                {
                    var serializedMessage = _sendQueue.Take(DisconnectTokenSource.Token);
                    _client.Client.Send(serializedMessage);
                }
                catch (Exception e) when (e is IOException || e is ObjectDisposedException || e is SocketException || e is OperationCanceledException)
                {
                    Disconnect();
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
            while (!DisconnectTokenSource.IsCancellationRequested)
            {
                try
                {
                    var receivedCount = stream.Read(buffer, 0, sizeof(int));
                    if (receivedCount == 0)
                    {
                        Disconnect();
                        return;
                    }
                    _receiveQueue.Add(buffer);
                }
                catch (Exception e) when (e is IOException || e is ObjectDisposedException || e is SocketException)
                {
                    Disconnect();
                    return;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Read thread unexpected exception");
                }
            }
        }

        protected byte[] TakeNextMessage()
        {
            return _receiveQueue.Take(DisconnectTokenSource.Token);
        }

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            NotifyPropertyChanged(propertyName);
            return true;
        }


    }
}
