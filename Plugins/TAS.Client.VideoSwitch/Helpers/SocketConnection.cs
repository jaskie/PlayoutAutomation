using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;

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
            Disconnect();

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
                DisconnectTokenSource = new CancellationTokenSource();
                Logger.Info("Connected to {0}:{1}", addressParts[0], port);
                StartThreads();
                IsConnected = true;
                return true;
            }
            catch
            {
                DisconnectTokenSource?.Cancel();
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
                    DisconnectTokenSource?.Cancel();
                    return;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                DisconnectTokenSource?.Cancel();
            }
        }

        public virtual void Disconnect()
        {
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

        public virtual void Dispose()
        {
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

        public event PropertyChangedEventHandler PropertyChanged;

        private void WriteThreadProc()
        {
            while (DisconnectTokenSource?.IsCancellationRequested == false)
            {
                try
                {
                    var serializedMessage = _sendQueue.Take(DisconnectTokenSource.Token);
                    Logger.Trace("Message sent: {0}", BitConverter.ToString(serializedMessage));
                    _client.Client.Send(serializedMessage);
                }
                catch (Exception e) when (e is IOException || e is ObjectDisposedException || e is SocketException || e is OperationCanceledException)
                {

                    DisconnectTokenSource?.Cancel();
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
            while (DisconnectTokenSource?.IsCancellationRequested == false)
            {
                try
                {
                    var receivedCount = stream.Read(buffer, 0, buffer.Length);
                    if (receivedCount == 0)
                    {
                        DisconnectTokenSource?.Cancel();
                        return;
                    }
                    var response = new byte[receivedCount];
                    Buffer.BlockCopy(buffer, 0, response, 0, receivedCount);
                    OnMessageReceived(response);
                }
                catch (Exception e) when (e is IOException || e is ObjectDisposedException || e is SocketException)
                {
                    DisconnectTokenSource?.Cancel();
                    return;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Read thread unexpected exception");
                }
            }
        }

        protected abstract void OnMessageReceived(byte[] message);

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
