using TAS.Client.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class MediaEditWindowViewmodel : OkCancelViewmodelBase<MediaEditViewmodel>
    {
        public MediaEditWindowViewmodel(IMedia media, IMediaManager mediaManager)
            : base(new MediaEditViewmodel(media, mediaManager, null, false), typeof(MediaEditView), media.MediaName)
        {
            Editor.DataContext = Model;
        }

        protected override void OnDispose()
        {
            Model.Dispose();
        }

        public override void Update(object destObject = null)
        {
            Model.Update();
        }
    }
}
