using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TAS.Client.Common;
using TAS.Client.Config.Model;

namespace TAS.Client.Config.ViewModels.Plugins.CgElementsController
{
    public class CgElementViewModel : EditViewmodelBase<CgElement>
    {
        private string _name = String.Empty;
        private string _command = String.Empty;
        private string _imageFile = String.Empty;
        private string _elementClientPath = String.Empty; //used only with parentals
        private string _elementServerPath = String.Empty;
        public CgElementViewModel(CgElement cgElement) : base(cgElement)
        {
            if (cgElement != null)
            {
                _name = cgElement.Name;
                _command = cgElement.Command;
                _imageFile = cgElement.ImageFile;
            }
        }

        public string Name { get => _name; set => SetField(ref _name, value); }
        public string Command { get => _command; set => SetField(ref _command, value); }
        public string ElementClientPath { get => _elementClientPath; set => SetField(ref _elementClientPath, value); }
        public string ElementServerPath { get => _elementServerPath; set => SetField(ref _elementServerPath, value); }
        public string ImageFile { get => _imageFile; set => _imageFile = value; }

        protected override void OnDispose()
        {
            //
        }
    }
}
