using System;
using TAS.Client.Common;
using TAS.Client.Config.Model;

namespace TAS.Client.Config.ViewModels.Plugins.CgElementsController
{    
    public class CgElementViewModel : OkCancelViewModelBase
    {
        private string _name = String.Empty;
        private string _command = String.Empty;        
        private string _uploadClientImagePath;
        private string _uploadServerImagePath;        
        private CgElement _cgElement;

        public CgElementViewModel(CgElement cgElement) : base("Add", "Cancel")
        {
            LoadCommands();           

            if (cgElement != null)
            {
                _cgElement = cgElement;
                OkButtonContent = "Edit";

                LoadData();                                
            }
        }

        private void LoadData()
        {
            _name = _cgElement.Name;
            _command = _cgElement.Command;
            ClientImagePath = _cgElement.ClientImagePath;
            ServerImagePath = _cgElement.ServerImagePath;
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
        
        //probably obsolete... will see later
        public bool IsClientImageVisible { get; } = true;

        public UiCommand UploadServerImageCommand { get; private set; }
        public UiCommand UploadClientImageCommand { get; private set; }
        

        public override bool Ok(object obj)
        {
            try
            {
                
                
                
                return true;
            }
            catch(Exception ex)
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
