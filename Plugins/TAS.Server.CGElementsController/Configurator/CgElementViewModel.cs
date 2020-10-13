using Microsoft.Win32;
using System;
using System.Windows.Media.Imaging;
using TAS.Client.Common;

namespace TAS.Server.CgElementsController.Configurator
{    
    public class CgElementViewModel : OkCancelViewModelBase
    {
        private string _name = String.Empty;
        private string _command = String.Empty;        
        private string _uploadClientImagePath;
        private string _uploadServerImagePath;
        private BitmapImage _serverBitmap;
        private BitmapImage _clientBitmap;
        private Model.CgElement _cgElement;

        public CgElementViewModel(Model.CgElement cgElement, string ConfirmButtonText = "Add") : base(ConfirmButtonText, "Cancel")
        {
            LoadCommands();
            _cgElement = cgElement;
            LoadData();
        }

        private void LoadData()
        {
            _name = _cgElement.Name;
            _command = _cgElement.Command;
            ClientImagePath = _cgElement.ClientImagePath;
            ServerImagePath = _cgElement.ServerImagePath;
            UploadClientImagePath = _cgElement.UploadClientImagePath;
            UploadServerImagePath = _cgElement.UploadServerImagePath;

            if (Uri.TryCreate(ServerImagePath, UriKind.Absolute, out var serverBitmapUri))
                ServerBitmap = new BitmapImage(serverBitmapUri);
            if (Uri.TryCreate(ClientImagePath, UriKind.Absolute, out var clientBitmapUri))
                ClientBitmap = new BitmapImage(clientBitmapUri);
            IsModified = false;
        }        

        private void LoadCommands()
        {
            UploadServerImageCommand = new UiCommand(UploadServerImage);
            UploadClientImageCommand = new UiCommand(UploadClientImage);
            ClearServerImageCommand = new UiCommand(ClearServerImage);
            ClearClientImageCommand = new UiCommand(ClearClientImage);
        }

        private void ClearClientImage(object obj)
        {
            ClientImagePath = null;
            ClientBitmap = null;
            UploadClientImagePath = null;
        }

        private void ClearServerImage(object obj)
        {
            ServerImagePath = null;
            ServerBitmap = null;
            UploadServerImagePath = null;
        }

        private void UploadClientImage(object obj)
        {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() != true)
                return;
            UploadClientImagePath = dialog.FileName;
            if (Uri.TryCreate(UploadClientImagePath, UriKind.Absolute, out var clientBitmapUri))
                ClientBitmap = new BitmapImage(clientBitmapUri);
            else
                ClientBitmap = null;
        }

        private void UploadServerImage(object obj)
        {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() != true)
                return;
            UploadServerImagePath = dialog.FileName;
            if (Uri.TryCreate(UploadServerImagePath, UriKind.Absolute, out var serverBitmapUri))
                ServerBitmap = new BitmapImage(serverBitmapUri);
            else
                ServerBitmap = null;
        }

        public string Name { get => _name; set => SetField(ref _name, value); }
        public string Command { get => _command; set => SetField(ref _command, value); }
        public string UploadClientImagePath { get => _uploadClientImagePath; set => SetField(ref _uploadClientImagePath, value); }
        public string UploadServerImagePath { get => _uploadServerImagePath; set => SetField(ref _uploadServerImagePath, value); }
        public string ServerImagePath { get; private set; }
        public string ClientImagePath { get; private set; }                  
        public UiCommand UploadServerImageCommand { get; private set; }
        public UiCommand UploadClientImageCommand { get; private set; }
        public UiCommand ClearServerImageCommand { get; private set; }
        public UiCommand ClearClientImageCommand { get; private set; }
        public BitmapImage ClientBitmap { get => _clientBitmap; set => SetField(ref _clientBitmap, value); }
        public BitmapImage ServerBitmap { get => _serverBitmap; set => SetField(ref _serverBitmap, value); }

        public override bool Ok(object obj)
        {
            try
            {
                _cgElement.Name = _name;
                _cgElement.Command = _command;
                _cgElement.ClientImagePath = UploadClientImagePath;
                _cgElement.ServerImagePath = UploadServerImagePath;
               
                _cgElement.UploadClientImagePath = UploadClientImagePath;             
                _cgElement.UploadServerImagePath = UploadServerImagePath;
                return true;
            }
            catch
            {
                return false;
            }
        }

        protected override void OnDispose()
        {
            //
        }
    }
}
