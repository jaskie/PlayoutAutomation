using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;
using TAS.Client.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class CGElementViewmodel : ViewmodelBase
    {
        private readonly ICGElement _element;

        public CGElementViewmodel(ICGElement element)
        {
            _element = element;
            if (element.Image != null)
                using (MemoryStream memory = new MemoryStream())
                {
                    element.Image.Save(memory, ImageFormat.Png);
                    memory.Position = 0;
                    Image = new BitmapImage();
                    Image.BeginInit();
                    Image.StreamSource = memory;
                    Image.CacheOption = BitmapCacheOption.OnLoad;
                    Image.EndInit();
                    Image.Freeze();
                }
        }

        public byte Id => _element.Id;
        public string Name => _element.Name;
        public BitmapImage Image { get; }

        protected override void OnDispose()
        {
            
        }
    }
}
