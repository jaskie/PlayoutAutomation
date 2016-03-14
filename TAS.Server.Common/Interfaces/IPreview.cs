using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server.Interfaces
{
    public interface IPreview
    {
        void PreviewLoad(IMedia media, long seek, long duration, long position, decimal audioLevel);
        IMedia PreviewMedia { get; }
        void PreviewUnload();
        bool PreviewLoaded { get; }
        bool PreviewIsPlaying { get; }
        long PreviewPosition { get; set; }
        long PreviewSeek { get; }
        decimal PreviewAudioLevel { get; set; }
        bool PreviewPause();
        bool PreviewPlay();
    }
}
