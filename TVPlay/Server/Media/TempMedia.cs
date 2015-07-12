using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;

namespace TAS.Server
{
    public class TempMedia: Media, IDisposable
    {
        internal Media OriginalMedia;
        public override string MediaName
        {
            get { return OriginalMedia._mediaName; }
        }

        public override TMediaType MediaType 
        { 
            get { return OriginalMedia.MediaType; }
        }

        public override TimeSpan Duration
        {
            get { return OriginalMedia._duration != TimeSpan.Zero? OriginalMedia._duration : _duration; }
        }
        public override TimeSpan DurationPlay
        {
            get { return OriginalMedia._durationPlay != TimeSpan.Zero ? OriginalMedia._durationPlay : _durationPlay; }
        }
        public override TimeSpan TCStart
        {
            get { return OriginalMedia._tCStart != TimeSpan.Zero ? OriginalMedia._tCStart: _tCStart; }
        }
        public override TimeSpan TCPlay
        {
            get { return OriginalMedia._tCPlay != TimeSpan.Zero ? OriginalMedia._tCPlay : _tCPlay; }
        }
        public override TVideoFormat VideoFormat
        {
            get { return OriginalMedia._videoFormat; }
        }
        public override TAudioChannelMapping AudioChannelMapping
        {
            get { return OriginalMedia._audioChannelMapping; }
        }
        public override decimal AudioVolume
        {
            get { return OriginalMedia._audioVolume; }
        }
        public override Guid MediaGuid
        {
            get { return OriginalMedia._mediaGuid; }
        }
        private bool _disposed = false;
        public void Dispose()
        {
            if (!_disposed)
                _directory.DeleteMedia(this);
        }

    }
}
