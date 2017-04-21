using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace TAS.Client.NDIVideoPreview
{
    [Export(typeof(Common.Plugin.IVideoPreview))]
    public class VideoPreviewViewmodel : Common.Plugin.IVideoPreview
    {
        public VideoPreviewViewmodel()
        {
            View = new VideoPreviewView { DataContext = this };
        }
        public UserControl View { get; private set; }
    }
}
