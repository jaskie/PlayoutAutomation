using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.FFMpegUtils;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Server
{
    public class TempMedia: Media, ITempMedia
    {
        public TempMedia(TempDirectory directory, IMediaProperties originalMedia): base(directory, originalMedia == null? Guid.NewGuid(): originalMedia.MediaGuid)
        {
            OriginalMedia = originalMedia;
            FileName = string.Format("{0}{1}", _mediaGuid, originalMedia == null ? FileUtils.TempFileExtension : Path.GetExtension(originalMedia.FileName));
        }

        internal IMediaProperties OriginalMedia;
        internal StreamInfo[] StreamInfo;

        public override string MediaName
        {
            get { return OriginalMedia == null ? _mediaName : OriginalMedia.MediaName; }
        }
        public override TMediaType MediaType 
        { 
            get { return OriginalMedia == null ? _mediaType : OriginalMedia.MediaType; }
        }
        public override TimeSpan Duration
        {
            get { return OriginalMedia == null || OriginalMedia.Duration == TimeSpan.Zero ? _duration : OriginalMedia.Duration; }
        }
        public override TimeSpan DurationPlay
        {
            get { return OriginalMedia == null || OriginalMedia.DurationPlay == TimeSpan.Zero ? _durationPlay : OriginalMedia.DurationPlay; }
        }
        public override TimeSpan TcStart
        {
            get { return OriginalMedia == null || OriginalMedia.TcStart == TimeSpan.Zero ? _tcStart : OriginalMedia.TcStart; }
        }
        public override TimeSpan TcPlay
        {
            get { return OriginalMedia == null || OriginalMedia.TcPlay == TimeSpan.Zero ? _tcPlay : OriginalMedia.TcPlay; }
        }
        public override decimal AudioVolume
        {
            get { return OriginalMedia == null ? _audioVolume : OriginalMedia.AudioVolume; }
        }

        private bool _disposed = false;
        public void Dispose()
        {
            if (!_disposed)
                _directory.DeleteMedia(this);
        }

    }
}
