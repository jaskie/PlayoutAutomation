using System.Windows.Input;
using TAS.Client.Common;
using TAS.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class MediaEditWindowViewmodel : ViewmodelBase
    {
        public MediaEditViewmodel Editor { get; }

        public string WindowTitle { get; }

        public MediaEditWindowViewmodel(IMedia media, IMediaManager mediaManager)
        {
            Editor = new MediaEditViewmodel(media, mediaManager, false);
            WindowTitle = media.MediaName;
        }

        protected override void OnDispose()
        {
            Editor.Dispose();
        }

        public ICommand CommandOk => Editor.CommandSaveEdit;

    }
}
