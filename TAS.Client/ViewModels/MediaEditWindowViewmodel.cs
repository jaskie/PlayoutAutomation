using TAS.Client.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Client.ViewModels
{
    public class MediaEditWindowViewmodel : OkCancelViewmodelBase<IMedia>
    {
        private readonly MediaEditViewmodel _editViewModel;
        public MediaEditWindowViewmodel(IMedia media, IMediaManager mediaManager)
            : base(media, new MediaEditView(), media.MediaName)
        {
            _editViewModel = new MediaEditViewmodel(media, mediaManager, null, false);
            Editor.DataContext = _editViewModel;
        }

        protected override void OnDispose()
        {
            _editViewModel.Dispose();
        }

        public override bool IsModified => base.IsModified || _editViewModel.IsModified;

        public override void ModelUpdate(object destObject = null)
        {
            _editViewModel.ModelUpdate();
            base.ModelUpdate(destObject);
        }
    }
}
