using System;
using System.Threading.Tasks;
using jNet.RPC.Client;
using TAS.Client.Common;
using TAS.Client.ViewModels;
using TAS.Remoting.Model;

namespace TVPlayClient
{
    public class ChannelWrapperViewModel : ViewModelBase
    {

        private readonly ChannelConfiguration _channelConfiguration;
        private RemoteClient _client;
        private ChannelViewModel _channel;
        private bool _isLoading = true;
        private string _tabName;

        public ChannelWrapperViewModel(ChannelConfiguration channel)
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

        public ChannelViewModel Channel
        {
            get => _channel;
            private set => SetField(ref _channel, value);
        }

        protected override void OnDispose()
        {
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

        private void SetupChannel(Engine engine)
        {
            Channel = new ChannelViewModel(engine, _channelConfiguration.ShowEngine, _channelConfiguration.ShowMedia);
            TabName = Channel.DisplayName;
            IsLoading = false;
        }

        private async void CreateView()
        {
            _client = new RemoteClient();
            _client.AddProxyAssembly(typeof(Engine).Assembly);
            _client.Disconnected += ClientDisconnected;
            if (await _client.ConnectAsync(_channelConfiguration.Address))
            {
                var engine = _client.GetRootObject<Engine>();
                if (engine != null)
                {
                    OnUiThread(() => SetupChannel(engine));
                    return;
                }
            }
            await Task.Delay(1000);
        }
    }
}
