using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
            _client?.Dispose();
            _channel?.Dispose();
            Debug.WriteLine(this, "Disposed");
        }

        private void _clientDisconected(object sender, EventArgs e)
        {
            if (sender is RemoteClient client)
            {
                client.Disconnected -= _clientDisconected;
                var vm = _channel;
                Application.Current?.Dispatcher.BeginInvoke((Action)delegate 
                {
                    vm.Dispose();
                });
            }
            _createView();
        }

        private void _createView()
        {
            IsLoading = true;
            Task.Run(() =>
            {
                while (true)
                {
                    _client = new RemoteClient(_configurationChannel.Address);
                    if (_client.IsConnected)
                    {
                        _client.Binder = new ClientTypeNameBinder();
                        var engine = _client.GetInitalObject<Engine>();
                        if (engine != null)
                        {
                            _client.Disconnected += _clientDisconected;
                            Application.Current?.Dispatcher.BeginInvoke((Action)delegate 
                            {
                                Channel = new ChannelViewmodel(engine, _configurationChannel.ShowEngine, _configurationChannel.ShowMedia);
                                TabName = Channel.DisplayName;
                                IsLoading = false;
                            });
                            return;
                        }
                    }
                    Thread.Sleep(1000);
                }
            });
        }

    }

}
