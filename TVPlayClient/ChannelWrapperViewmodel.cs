using System;
using System.Threading.Tasks;
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

        public ChannelWrapperViewmodel(ChannelConfiguration channel)
        {
            _channelConfiguration = channel;
        }

        public void Initialize()
        {
            Task.Run(CreateView);
        }

        public string TabName
        {
            get => _tabName;
            private set => SetField(ref _tabName, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetField(ref _isLoading, value);
        }

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
            var channel = Channel;
            Channel = null;
            IsLoading = true;
            channel?.Dispose();
            CreateView();
        }

        private void SetupChannel(IEngine engine)
        {
            Channel = new ChannelViewmodel(engine, _channelConfiguration.ShowEngine, _channelConfiguration.ShowMedia);
            TabName = Channel.DisplayName;
            IsLoading = false;
        }

        private async void CreateView()
        {
            while (!_isDisposed)
                try
                {
                    _client = new RemoteClient(_channelConfiguration.Address, ClientTypeNameBinder.Current);
                    var engine = _client.GetRootObject<IEngine>();
                    if (engine is null)
                    {
                        _client.Dispose();
                        await Task.Delay(5000);
                    }
                    else
                    {
                        _client.Disconnected += ClientDisconnected;
                        OnUiThread(() => SetupChannel(engine));
                        return;
                    }
                }
                catch
                {
                    await Task.Delay(5000);
                }
        }
    }
}
