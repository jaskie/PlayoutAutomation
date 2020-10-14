using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;
using TAS.Client.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class CGElementViewModel : ViewModelBase
    {
        private readonly ICGElement _element;

        public CGElementViewModel(ICGElement element)
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
