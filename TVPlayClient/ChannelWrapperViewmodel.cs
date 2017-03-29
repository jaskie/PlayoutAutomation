using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;
using TAS.Client.ViewModels;
using TAS.Remoting;
using TAS.Remoting.Client;
using TAS.Remoting.Model;
using TAS.Server.Interfaces;

namespace TVPlayClient
{
    [XmlType("Channel")]
    public class ChannelWrapperViewmodel : ViewmodelBase
    {
        #region Serialization properties
        [XmlAttribute]
        public string Address { get; set; }
        [XmlAttribute]
        public bool AllowControl { get; set; } = true;
        [XmlAttribute]
        public bool ShowEngine { get; set; } = true;
        [XmlAttribute]
        public bool ShowMedia { get; set; } = true;
        #endregion

        public void Initialize()
        {
            _createView();
        }

        private RemoteClient _client;

        private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            var client = _client;
            if (client != null)
                client.Dispose();
        }

        private void _clientDisconected(object sender, EventArgs e)
        {
            var client = sender as RemoteClient;
            if (client != null)
            {
                client.Disconnected -= _clientDisconected;
                var vm = _channel;
                Application.Current?.Dispatcher.BeginInvoke((Action)delegate ()
                {
                    vm.Dispose();
                });
            }
            _createView();
        }

        private void _createView()
        {
            IsLoading = true;
            ThreadPool.QueueUserWorkItem((o) =>
            {
                while (true)
                {
                    _client = new RemoteClient(Address);
                    if (_client.IsConnected)
                    {
                        _client.Binder = new ClientTypeNameBinder();
                        Engine engine = _client.GetInitalObject<Engine>();
                        if (engine != null)
                        {
                            _client.Disconnected += _clientDisconected;
                            Application.Current?.Dispatcher.BeginInvoke((Action)delegate ()
                            {
                                Channel = new ChannelViewmodel(engine, ShowEngine, ShowMedia, AllowControl);
                                TabName = Channel.ChannelName;
                                IsLoading = false;
                            });
                            return;
                        }
                    }
                    Thread.Sleep(1000);
                }
            });
        }

        private ChannelViewmodel _channel;
        [XmlIgnore]
        public ChannelViewmodel Channel { get { return _channel; } private set { SetField(ref _channel, value, nameof(Channel)); } }

        private bool _isLoading = true;

        [XmlIgnore]
        public bool IsLoading { get { return _isLoading; } set { SetField(ref _isLoading, value, nameof(IsLoading)); } }

        private string _tabName;
        [XmlIgnore]
        public string TabName { get { return _tabName; }  private set { SetField(ref _tabName, value, nameof(TabName)); } }

        protected override void OnDispose()
        {
            var client = _client;
            if (client != null)
                client.Dispose();
            var vm = _channel;
            if (vm != null)
                vm.Dispose();
            Debug.WriteLine(this, "Disposed");
        }

    }
}
