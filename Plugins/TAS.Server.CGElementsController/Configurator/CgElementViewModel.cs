using System;
using TAS.Client.Common;

namespace TAS.Server.CgElementsController.Configurator
{    
    public class CgElementViewModel : OkCancelViewModelBase
    {
        private string _name = String.Empty;
        private string _command = String.Empty;        
        private string _uploadClientImagePath;
        private string _uploadServerImagePath;        
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
            IsModified = false;
        }        

        private void LoadCommands()
        {
            UploadServerImageCommand = new UiCommand(UploadServerImage);
            UploadClientImageCommand = new UiCommand(UploadClientImage);
        }

        private void UploadClientImage(object obj)
        {
            UploadClientImagePath = UiServices.CommonDialogManager.OpenFileDialog();
        }

        private void UploadServerImage(object obj)
        {
            UploadServerImagePath = UiServices.CommonDialogManager.OpenFileDialog();
        }

        public string Name { get => _name; set => SetField(ref _name, value); }
        public string Command { get => _command; set => SetField(ref _command, value); }
        public string UploadClientImagePath { get => _uploadClientImagePath; set => SetField(ref _uploadClientImagePath, value); }
        public string UploadServerImagePath { get => _uploadServerImagePath; set => SetField(ref _uploadServerImagePath, value); }
        public string ServerImagePath { get; private set; }
        public string ClientImagePath { get; private set; }                

        public UiCommand UploadServerImageCommand { get; private set; }
        public UiCommand UploadClientImageCommand { get; private set; }
        

        public override bool Ok(object obj)
        {
            try
            {
                _cgElement.Name = _name;
                _cgElement.Command = _command;
                _cgElement.ClientImagePath = ClientImagePath;
                _cgElement.ServerImagePath = ServerImagePath;
               
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
