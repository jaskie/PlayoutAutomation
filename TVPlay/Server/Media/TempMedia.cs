using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;

namespace TAS.Server
{
    public class TempMedia: Media, IDisposable
    {
        public TempMedia(TempDirectory directory, Media originalMedia, string fileExtension): base(directory, originalMedia.MediaGuid)
        {
            OriginalMedia = originalMedia;
            _fileName = string.Format("{0}.{1}", _mediaGuid, fileExtension);
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
            get { return OriginalMedia.Duration == TimeSpan.Zero ? _duration : OriginalMedia.Duration; }
        }
        public override TimeSpan DurationPlay
        {
            get { return OriginalMedia.DurationPlay == TimeSpan.Zero ? _durationPlay : OriginalMedia.DurationPlay; }
        }
        public override TimeSpan TCStart
        {
            get { return OriginalMedia.TCStart == TimeSpan.Zero ? _tCStart : OriginalMedia.TCStart; }
        }
        public override TimeSpan TCPlay
        {
            get { return OriginalMedia.TCPlay == TimeSpan.Zero ? _tCPlay : OriginalMedia.TCPlay; }
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
