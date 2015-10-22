using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;

namespace TAS.Server
{
    public class TempMedia: Media, IDisposable
    {
        public TempMedia(TempDirectory directory) : base(directory) { }
        public TempMedia(TempDirectory directory, Guid guid) : base(directory, guid) { }
        public TempMedia(TempDirectory directory, Media originalMedia): base(directory, originalMedia.MediaGuid)
        {
            OriginalMedia = originalMedia;
        }

        internal Media OriginalMedia;
        public override string MediaName
        {
            get { return OriginalMedia.MediaName; }
        }

        public override TMediaType MediaType 
        { 
            get { return OriginalMedia.MediaType; }
        }

        public override TimeSpan Duration
        {
            get { return OriginalMedia.Duration; }
        }
        public override TimeSpan DurationPlay
        {
            get { return OriginalMedia.DurationPlay; }
        }
        public override TimeSpan TCStart
        {
            get { return OriginalMedia.TCStart; }
        }
        public override TimeSpan TCPlay
        {
            get { return OriginalMedia.TCPlay; }
        }
        public override TVideoFormat VideoFormat
        {
            get { return OriginalMedia.VideoFormat; }
        }
        public override TAudioChannelMapping AudioChannelMapping
        {
            get { return OriginalMedia.AudioChannelMapping; }
        }
        public override decimal AudioVolume
        {
            get { return OriginalMedia.AudioVolume; }
        }
        public override Guid MediaGuid
        {
            get { return OriginalMedia.MediaGuid; }
        }
        private bool _disposed = false;
        public void Dispose()
        {
            if (!_disposed)
                _directory.DeleteMedia(this);
        }

    }
}
