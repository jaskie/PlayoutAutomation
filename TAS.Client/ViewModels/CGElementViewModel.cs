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
            Thumbnail = BitmapTools.BitmapToImageSource(element.Thumbnail);
        }

        public byte Id => _element.Id;
        
        public string Name => _element.Name;
        
        public BitmapImage Thumbnail { get; }

        protected override void OnDispose()
        {
            
        }
    }
}
