using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Documents;
using TAS.Client.Common;
using TAS.Client.Config.Model;
using TAS.Common;

namespace TAS.Client.Config.ViewModels.Plugins.CgElementsController
{
    public class CgElementViewModel : EditViewmodelBase<CgElement>, IOkCancelViewModel
    {
        private string _name = String.Empty;
        private string _command = String.Empty;
        private string _clientImagePath;
        private string _serverImagePath;
        private List<string> _serverPaths; 
        public CgElementViewModel(CgElement cgElement, List<string> serverPaths) : base(cgElement)
        {
            LoadCommands();
            _serverPaths = serverPaths;

            if (cgElement != null)
            {
                _name = cgElement.Name;
                _command = cgElement.Command;
                ClientImagePath = cgElement.ImageFile;
            }
        }

        private void LoadCommands()
        {
            UploadServerImageCommand = new UiCommand(UploadServerImage);
            UploadClientImageCommand = new UiCommand(UploadClientImage);
        }

        private void UploadClientImage(object obj)
        {
            ClientImagePath = UiServices.CommonDialogManager.OpenFileDialog();
        }

        private void UploadServerImage(object obj)
        {
            ServerImagePath = UiServices.CommonDialogManager.OpenFileDialog();
        }

        public string Name { get => _name; set => SetField(ref _name, value); }
        public string Command { get => _command; set => SetField(ref _command, value); }
        public string ClientImagePath { get => _clientImagePath; set => SetField(ref _clientImagePath, value); }
        public string ServerImagePath { get => _serverImagePath; set => SetField(ref _serverImagePath, value); }        

        public bool IsClientImageVisible { get; } = true;

        public UiCommand UploadServerImageCommand { get; private set; }
        public UiCommand UploadClientImageCommand { get; private set; }

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
                if (_serverImagePath != null && _serverImagePath.Length>0)
                {
                    foreach (var path in _serverPaths)
                    {
                        File.Copy(_serverImagePath, Path.Combine(path, Path.GetFileName(_serverImagePath)), true);
                    }
                }                
                
                if (_clientImagePath != null && _clientImagePath.Length>0)
                {
                    string configPath = FileUtils.ConfigurationPath;
                    switch (Model.CgType)
                    {
                        case CgElement.Type.Parental:
                            configPath = Path.Combine(configPath, "Parentals");
                            break;

                        case CgElement.Type.Aux:
                            configPath = Path.Combine(configPath, "Auxes");
                            break;

                        case CgElement.Type.Crawl:
                            configPath = Path.Combine(configPath, "Crawls");
                            break;

                        case CgElement.Type.Logo:
                            configPath = Path.Combine(configPath, "Logos");
                            break;
                    }

                    if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), configPath)))
                        Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), configPath));

                    var clientPath = Path.Combine(Directory.GetCurrentDirectory(), configPath, Path.GetFileName(_clientImagePath));
                    File.Copy(_clientImagePath, clientPath, true);
                }
                
                Update();
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
