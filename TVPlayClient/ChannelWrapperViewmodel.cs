using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TAS.Client.Common;
using TAS.Client.ViewModels;
using TAS.Remoting;
using TAS.Remoting.Client;
using TAS.Remoting.Model;

namespace TVPlayClient
{
    public class ChannelWrapperViewmodel : ViewModelBase
    {

        private readonly ConfigurationChannel _configurationChannel;
        private RemoteClient _client;
        private ChannelViewmodel _channel;
        private bool _isLoading = true;
        private string _tabName;

        public ChannelWrapperViewmodel(ConfigurationChannel channel)
        {
            _configurationChannel = channel;
        }

        public void Initialize()
        {
#if DEBUG
         Thread.Sleep(5000);   
#endif
            _createView();
        }

        public string TabName { get => _tabName; private set => SetField(ref _tabName, value); }

        public bool IsLoading { get => _isLoading; set => SetField(ref _isLoading, value); }

        public ChannelViewmodel Channel { get => _channel; private set => SetField(ref _channel, value); }

        protected override void OnDispose()
        {
            _channel?.Dispose();
            _client?.Dispose();
            Debug.WriteLine(this, "Disposed");
        }

        private void ClientDisconnected(object sender, EventArgs e)
        {
            if (!(sender is RemoteClient client))
                return;
            client.Disconnected -= ClientDisconnected;
            client.Dispose();
            var channel = Channel;
            OnUiThread(() =>
            {
                Channel = null;
                IsLoading = true;
                channel?.Dispose();
                _createView();
            });
        }

        private void _createView()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        _client = new RemoteClient(_configurationChannel.Address)
                        {
                            Binder = new ClientTypeNameBinder()
                        };
                        _client.Disconnected += ClientDisconnected;
                        var engine = _client.GetInitalObject<Engine>();
                        if (engine == null)
                        {
                            Thread.Sleep(1000);
                            continue;
                        }
                        OnUiThread(() =>
                        {
                            Channel = new ChannelViewmodel(engine, _configurationChannel.ShowEngine,
                                _configurationChannel.ShowMedia);
                            TabName = Channel.DisplayName;
                            IsLoading = false;
                        });
                        return;
                    }
                    catch (SocketException)
                    {
                        Thread.Sleep(1000);
                    }
                }
            });
        }

    }

}
