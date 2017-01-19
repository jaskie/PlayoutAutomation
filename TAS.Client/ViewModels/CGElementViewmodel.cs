using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using TAS.Server.Interfaces;

namespace TAS.Client.ViewModels
{
    public class CGElementViewmodel : ViewmodelBase
    {
        private readonly ICGElement _element;
        private readonly BitmapImage _image;
        public CGElementViewmodel(ICGElement element)
        {
            _element = element;
            if (element.Image != null)
                using (MemoryStream memory = new MemoryStream())
                {
                    element.Image.Save(memory, ImageFormat.Png);
                    memory.Position = 0;
                    _image = new BitmapImage();
                    _image.BeginInit();
                    _image.StreamSource = memory;
                    _image.CacheOption = BitmapCacheOption.OnLoad;
                    _image.EndInit();
                    _image.Freeze();
                }
        }

        public byte Id { get { return _element.Id; } }
        public string Name { get { return _element.Name; } }
        public BitmapImage Image { get { return _image; } }

        protected override void OnDispose()
        {
            
        }
    }
}
