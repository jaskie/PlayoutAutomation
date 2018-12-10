using System;
using System.ComponentModel.Composition;
using TAS.Client.Common.Plugin;

namespace TAS.Client.NDIVideoPreview
{
    [Export(typeof(IUiPluginFactory))]
    public class VideoPreviewViewmodelFactory: IUiPluginFactory
    {
        public object CreateNew(IUiPluginContext context)
        {
            return new VideoPreviewViewmodel();
        }

        public Type Type { get; } = typeof(VideoPreviewViewmodel);
    }
}
