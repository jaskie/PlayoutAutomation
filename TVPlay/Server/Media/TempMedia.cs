using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Server
{
    public class TempMedia: Media, ITempMedia
    {
        public TempMedia(TempDirectory directory, IMedia originalMedia, string fileExtension): base(directory, originalMedia.MediaGuid)
        {
            OriginalMedia = originalMedia;
            _fileName = string.Format("{0}.{1}", _mediaGuid, fileExtension);
        }

        internal IMedia OriginalMedia;
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
            get { return OriginalMedia.Duration == TimeSpan.Zero ? _duration : OriginalMedia.Duration; }
        }
        public override TimeSpan DurationPlay
        {
            get { return OriginalMedia.DurationPlay == TimeSpan.Zero ? _durationPlay : OriginalMedia.DurationPlay; }
        }
        public override TimeSpan TcStart
        {
            get { return OriginalMedia.TcStart == TimeSpan.Zero ? _tcStart : OriginalMedia.TcStart; }
        }
        public override TimeSpan TcPlay
        {
            get { return OriginalMedia.TcPlay == TimeSpan.Zero ? _tcPlay : OriginalMedia.TcPlay; }
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
