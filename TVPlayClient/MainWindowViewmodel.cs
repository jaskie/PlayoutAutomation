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
            _createView();
        }

        private void _client_Disconnected(object sender, EventArgs e)
        {
            var client = sender as RemoteClient;
            if (client != null)
            {
                client.Disconnected -= _client_Disconnected;
                _createView();
            }
        }

        private void _createView()
        {
            IsLoading = true;
            ThreadPool.QueueUserWorkItem((o) =>
            {
                while (true)
                {
                    RemoteClient client;
                    client = new RemoteClient(ConfigurationManager.AppSettings["Host"]);
                    if (client.IsConnected)
                    {
                        client.Binder = new Remoting.ClientTypeNameBinder();
                        client.Disconnected += _client_Disconnected;
                        Engine initalObject = client.GetInitalObject<Engine>();
                        if (initalObject != null)
                        {
                            Application.Current.Dispatcher.BeginInvoke((Action)delegate ()
                            {
                                View = new Views.EngineView(initalObject.FrameRate) { DataContext = new EngineViewmodel(initalObject, initalObject) };
                                IsLoading = false;
                            });
                            return;
                        }
                    }
                }
            });
        }

        private UserControl _view;
        public UserControl View { get { return _view; } set { SetField(ref _view, value, nameof(View)); } }

        private bool _isLoading = true;
        public bool IsLoading { get { return _isLoading; } set { SetField(ref _isLoading, value, nameof(IsLoading)); } }

        protected override void OnDispose()
        {
            
        }

    }
}
