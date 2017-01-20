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
            _client_Disconnected(null, EventArgs.Empty);
        }

        private RemoteClient _client;

        private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            var client = _client;
            if (client != null)
                client.Dispose();
        }

        private void _client_Disconnected(object sender, EventArgs e)
        {
            var client = sender as RemoteClient;
            if (client != null)
            {
                client.Disconnected -= _client_Disconnected;
                var vm = _viewmodel;
                Application.Current?.Dispatcher.BeginInvoke((Action)delegate ()
                {
                    vm.Dispose();
                });
            }
            _createView();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
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
                        _client.Disconnected += _client_Disconnected;
                        Engine engine = _client.GetInitalObject<Engine>();
                        if (engine != null)
                        {
                            Application.Current?.Dispatcher.BeginInvoke((Action)delegate ()
                            {
                                ChannelViewmodel vm = new ChannelViewmodel(engine, ShowEngine, ShowMedia, AllowControl);
                                _viewmodel = vm;
                                TabName = vm.ChannelName;
                                View = vm.View;
                                IsLoading = false;
                            });
                            return;
                        }
                    }
                }
            });
        }

        private ViewmodelBase _viewmodel;

        private UserControl _view;
        [XmlIgnore]
        public UserControl View { get { return _view; } private set { SetField(ref _view, value, nameof(View)); } }

        private bool _isLoading = true;

        [XmlIgnore]
        public bool IsLoading { get { return _isLoading; } set { SetField(ref _isLoading, value, nameof(IsLoading)); } }

        private string _tabName;
        [XmlIgnore]
        public string TabName { get { return _tabName; }  private set { SetField(ref _tabName, value, nameof(TabName)); } }

        protected override void OnDispose()
        {
            View = null;
            var client = _client;
            if (client != null)
                client.Dispose();
            var vm = _viewmodel;
            if (vm != null)
                vm.Dispose();
            Debug.WriteLine(this, "Disposed");
        }

    }
}
