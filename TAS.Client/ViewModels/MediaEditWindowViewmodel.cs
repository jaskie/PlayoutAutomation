using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;

namespace TAS.Client.ViewModels
{
    public class MediaEditWindowViewModel : ViewModelBase
    {
        public MediaEditViewModel Editor { get; }

        public string WindowTitle { get; }

        public MediaEditWindowViewModel(IMedia media, IMediaManager mediaManager)
        {
            Editor = new MediaEditViewModel(media, mediaManager, false);
            WindowTitle = media.MediaName;
        }

        protected override void OnDispose()
        {
            Editor.Dispose();
        }
    }
}
