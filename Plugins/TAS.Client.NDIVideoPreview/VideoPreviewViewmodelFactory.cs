using System;
using System.ComponentModel.Composition;
using TAS.Client.Common.Plugin;

namespace TAS.Client.NDIVideoPreview
{
    [Export(typeof(IUiPluginFactory))]
    public class VideoPreviewViewModelFactory: IUiPluginFactory
    {
        object[] IUiPluginFactory.Create(IUiPluginContext context)
        {
            return new object[] { new VideoPreviewViewModel() };
        }

        public Type Type { get; } = typeof(VideoPreviewViewModel);
    }
}
