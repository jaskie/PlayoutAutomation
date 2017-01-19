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
using TAS.Client.ViewModels;
using TAS.Remoting.Client;
using TAS.Remoting.Model;
using TAS.Server.Interfaces;

namespace TAS.Client
{
    public class MainWindowViewmodel : ViewModels.ViewmodelBase
    {
        public MainWindowViewmodel()
        {
#if DEBUG
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
            Infralution.Localization.Wpf.CultureManager.UICulture = new System.Globalization.CultureInfo("en");
            System.Threading.Thread.Sleep(2000); // wait for server to spin up
#endif
            Application.Current.Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
                _createView();
            else
                _isLoading = false;
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
                Application.Current?.Dispatcher.BeginInvoke((Action)delegate ()
                {
                    _viewmodel.Dispose();
                    View = null;
                });
                _createView();
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        private void _createView()
        {
            IsLoading = true;
            ThreadPool.QueueUserWorkItem((o) =>
            {
                while (true)
                {
                    _client = new RemoteClient(ConfigurationManager.AppSettings["Host"]);
                    if (_client.IsConnected)
                    {
                        _client.Binder = new Remoting.ClientTypeNameBinder();
                        _client.Disconnected += _client_Disconnected;
                        Engine initalObject = _client.GetInitalObject<Engine>();
                        if (initalObject != null)
                        {
                            Application.Current?.Dispatcher.BeginInvoke((Action)delegate ()
                            {
                                _viewmodel = new EngineViewmodel(initalObject, initalObject);
                                View = new Views.EngineView(initalObject.FrameRate) { DataContext = _viewmodel };
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
        public UserControl View { get { return _view; } set { SetField(ref _view, value, nameof(View)); } }

        private bool _isLoading = true;
        public bool IsLoading { get { return _isLoading; } set { SetField(ref _isLoading, value, nameof(IsLoading)); } }

        protected override void OnDispose()
        {
            Debug.WriteLine(this, "Disposed");
        }

    }
}
