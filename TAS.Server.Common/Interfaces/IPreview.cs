using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server.Interfaces
{
    public interface IPreview
    {
        void PreviewLoad(IServerMedia media, long seek, long duration, long position);
        IServerMedia PreviewMedia { get; }
        void PreviewUnload();
        bool PreviewLoaded { get; }
        bool PreviewIsPlaying { get; }
        long PreviewPosition { get; set; }
        long PreviewSeek { get; }
        bool PreviewPause();
        bool PreviewPlay();
    }
}
