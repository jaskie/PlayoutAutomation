using System;
using System.ComponentModel.Composition;
using TAS.Client.Common.Plugin;

namespace TAS.Client.NDIVideoPreview
{
    [Export(typeof(IUiPluginFactory))]
    public class VideoPreviewViewmodelFactory: IUiPluginFactory
    {
        object[] IUiPluginFactory.Create(IUiPluginContext context)
        {
            return new object[] { new VideoPreviewViewmodel() };
        }

        public Type Type { get; } = typeof(VideoPreviewViewmodel);
    }
}
