using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Client.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.ViewModels
{
    public class MediaEditWindowViewmodel : OkCancelViewmodelBase<TAS.Server.Interfaces.IMedia>
    {
        public readonly MediaEditViewmodel editViewModel;
        public MediaEditWindowViewmodel(IMedia media, IMediaManager mediaManager)
            : base(media, new MediaEditView(media.FrameRate), media.MediaName)
        {
            editViewModel = new MediaEditViewmodel(media, mediaManager, null, false);
            Editor.DataContext = editViewModel;
        }

        protected override void OnDispose()
        {
            editViewModel.Dispose();
        }

        public override bool Modified
        {
            get
            {
                return _modified || editViewModel.Modified;
            }
        }

        public override void Save(object destObject = null)
        {
            editViewModel.Save();
            base.Save(destObject);
        }
    }
}
