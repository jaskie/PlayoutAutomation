using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using jNet.RPC.Client;
using TAS.Client.Common;
using TAS.Client.ViewModels;
using TAS.Remoting;
using TAS.Remoting.Model;

namespace TVPlayClient
{
    public class ChannelWrapperViewmodel : ViewModelBase
    {

        private readonly ChannelConfiguration _channelConfiguration;
        private RemoteClient _client;
        private ChannelViewmodel _channel;
        private bool _isLoading = true;
        private string _tabName;

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
            Channel = new ChannelViewmodel(engine, _channelConfiguration.ShowEngine, _channelConfiguration.ShowMedia);
            TabName = Channel.DisplayName;
            IsLoading = false;
        }

        private async void CreateView()
        {
            try
            {
                _client = new RemoteClient(ClientTypeNameBinder.Current);
                _client.Disconnected += ClientDisconnected;
                await _client.ConnectAsync(_channelConfiguration.Address);

                var engine = _client.GetRootObject<Engine>();
                if (engine == null)
                {
                    await Task.Delay(1000);
                    return;
                }
                OnUiThread(() => SetupChannel(engine));
            }
            catch (OperationCanceledException)
            {
                await Task.Delay(1000);
            }
        }
    }
}
