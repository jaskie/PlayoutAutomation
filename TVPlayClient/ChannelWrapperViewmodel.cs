using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Input;
using jNet.RPC.Client;
using TAS.Client.Common;
using TAS.Client.ViewModels;
using TAS.Common.Interfaces;
using TAS.Remoting;

namespace TVPlayClient
{
    public class ChannelWrapperViewmodel : ViewModelBase
    {

        private readonly ChannelConfiguration _channelConfiguration;
        private RemoteClient _client;
        private ChannelViewmodel _channel;
        private bool _isLoading = true;
        private string _tabName;
        private bool _isDisposed;
        private bool _isFailed;
        private string _failedMessage;
        private int _retryCount;

        public ChannelWrapperViewmodel(ChannelConfiguration channel)
        {
            _channelConfiguration = channel;
            RetryCommand = new UiCommand(nameof(Retry), _ => Retry(), _ => IsFailed);
        }

        public void Initialize()
        {
            Task.Run(CreateView);
        }

        public string TabName { get => _tabName; private set => SetField(ref _tabName, value); }

        public bool IsLoading { get => _isLoading; private set => SetField(ref _isLoading, value); }

        public bool IsFailed
        {
            get => _isFailed;
            private set
            {
                if (!SetField(ref _isFailed, value))
                    return;
                InvalidateRequerySuggested();
            }
        }

        public string ConnectionMessage { get => _failedMessage; private set => SetField(ref _failedMessage, value); }

        public ICommand RetryCommand { get; } 

        public ChannelViewmodel Channel
        {
            get => _channel;
            private set => SetField(ref _channel, value);
        }

        protected override void OnDispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;
            _channel?.Dispose();
            if (_client != null)
            {
                _client.Disconnected -= ClientDisconnected;
                _client.Dispose();
            }
        }

        private void ClientDisconnected(object sender, EventArgs e)
        {
            if (!(sender is RemoteClient client))
                return;
            client.Disconnected -= ClientDisconnected;
            client.Dispose();
            Channel?.Dispose();
            Channel = null;
            Retry();
        }

        private void SetupChannel(IEngine engine)
        {
            Channel = new ChannelViewmodel(engine, _channelConfiguration.ShowEngine, _channelConfiguration.ShowMedia);
            TabName = Channel.DisplayName;
            IsLoading = false;
        }

        private void Retry()
        {
            IsFailed = false;
            ConnectionMessage = string.Empty;
            Task.Run(CreateView);
        }

        private async Task CreateView()
        {
            ConnectionMessage = string.Format(Properties.Resources._message_Connecting, _channelConfiguration.Address);
            IsLoading = true;
            _retryCount = 1;
            while (!_isDisposed)
            {
                _client = new RemoteClient(_channelConfiguration.Address, ClientTypeNameBinder.Current);
                var engine = _client.GetRootObject<IEngine>();
                if (engine is null)
                {
                    try
                    {
                        switch (_client.ClientConnectionState)
                        {
                            case ClientConnectionState.Disconnected:
                                if (_retryCount++ < 10)
                                    ConnectionMessage = string.Format(Properties.Resources._message_ConnectionRetry, _channelConfiguration.Address, _retryCount);
                                else
                                {
                                    SetFailed(Properties.Resources._message_ConnectionFailed);
                                    return;
                                }
                                await Task.Delay(5000);
                                continue;
                            case ClientConnectionState.Rejected:
                                SetFailed(Properties.Resources._message_ConnectionRejected);
                                return;
                            default:
                                SetFailed(Properties.Resources._message_ReceivedEmptyRoot);
                                break;
                        }
                    }
                    finally
                    {
                        _client.Dispose();
                        _client = null;
                    }
                }
                else
                {
                    _client.Disconnected += ClientDisconnected;
                    OnUiThread(() => SetupChannel(engine));
                }
                return;
            }
        }

        private void SetFailed(string messageToFormat)
        {
            IsLoading = false;
            ConnectionMessage = string.Format(messageToFormat, _channelConfiguration.Address);
            IsFailed = true;
        }
    }
}
