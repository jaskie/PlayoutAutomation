using System;
using System.IO;
using TAS.Client.Common;
using TAS.Client.Config.Model;

namespace TAS.Client.Config.ViewModels.Plugins.CgElementsController
{
    public class CgElementViewModel : EditViewmodelBase<CgElement>, IOkCancelViewModel
    {
        private string _name = String.Empty;
        private string _command = String.Empty;
        private string _imageFile = String.Empty;
        private string _clientImagePath = String.Empty; //used only with parentals
        private string _serverImagePath = String.Empty;
        private string _clientPath = String.Empty; //used only with parentals
        private string _serverPath = String.Empty;
        public CgElementViewModel(CgElement cgElement, bool isParental) : base(cgElement)
        {
            IsClientImageVisible = isParental;
            if (cgElement != null)
            {
                _name = cgElement.Name;
                _command = cgElement.Command;
                _imageFile = cgElement.ImageFile;
            }
        }

        public string Name { get => _name; set => SetField(ref _name, value); }
        public string Command { get => _command; set => SetField(ref _command, value); }
        public string ClientImagePath { get => _clientImagePath; set => SetField(ref _clientImagePath, value); }
        public string ServerImagePath { get => _serverImagePath; set => SetField(ref _serverImagePath, value); }
        public string ImageFile { get => _imageFile; set => _imageFile = value; }
        public bool IsClientImageVisible { get; }

        public bool CanCancel(object obj)
        {
            return true;
        }

        public void Cancel(object obj)
        {
            //
        }

        public bool CanOk(object obj)
        {
            return IsModified;            
        }

        public bool Ok(object obj)
        {
            try
            {
                File.Copy(_serverImagePath, _serverPath);
                if (_clientImagePath.Length > 0)
                    File.Copy(_clientImagePath, _clientPath);

                Update();
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
