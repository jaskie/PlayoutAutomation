using System;
using TAS.Common.Interfaces.Media;

namespace TAS.Common
{
    public class MediaOnLayerEventArgs : EventArgs
    {
        public MediaOnLayerEventArgs(IMedia media, VideoLayer layer)
        {
            Media = media;
            Layer = layer;
        }

        public IMedia Media { get; }

        public VideoLayer Layer { get; }
    }
}
