using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Client.Setup
{
    public class PlayoutServerViewmodel:ViewModels.ViewmodelBase
    {
        protected override void OnDispose() { }
        readonly Model.PlayoutServer _playoutServer;
        private string _serverAddress;
        private System.Windows.Controls.UserControl _view;
        private string _mediaFolder;
        public PlayoutServerViewmodel(Model.PlayoutServer playoutServer)
        {
            this._playoutServer = playoutServer;
            this._serverAddress = playoutServer.ServerAddress;
            this._mediaFolder = playoutServer.MediaFolder;
            _view = new PlayoutServerView() { DataContext = this };
        }
        public string ServerAddress { get { return _serverAddress; } set { SetField(ref _serverAddress, value, "ServerAddress"); } }
        public string MediaFolder { get { return _mediaFolder; } set { SetField(ref _mediaFolder, value, "MediaFolder"); } }
        public System.Windows.Controls.UserControl View { get { return _view; } }
    }
}
