using System;
using System.IO;
using TAS.FFMpegUtils;
using TAS.Server.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Server.Media
{
    public class TempMedia: MediaBase, ITempMedia
    {
        public TempMedia(TempDirectory directory, IMediaProperties originalMedia): base(directory, originalMedia == null? Guid.NewGuid(): originalMedia.MediaGuid)
        {
            OriginalMedia = originalMedia;
            FileName = $"{_mediaGuid}{(originalMedia == null ? FileUtils.TempFileExtension : Path.GetExtension(originalMedia.FileName))}";
        }

        internal IMediaProperties OriginalMedia;
        internal StreamInfo[] StreamInfo;

        public override string MediaName => OriginalMedia == null ? _mediaName : OriginalMedia.MediaName;

        public override TMediaType MediaType => OriginalMedia?.MediaType ?? _mediaType;

        public override TimeSpan Duration => OriginalMedia == null || OriginalMedia.Duration == TimeSpan.Zero ? _duration : OriginalMedia.Duration;

        public override TimeSpan DurationPlay => OriginalMedia == null || OriginalMedia.DurationPlay == TimeSpan.Zero ? _durationPlay : OriginalMedia.DurationPlay;

        public override TimeSpan TcStart => OriginalMedia == null || OriginalMedia.TcStart == TimeSpan.Zero ? _tcStart : OriginalMedia.TcStart;

        public override TimeSpan TcPlay => OriginalMedia == null || OriginalMedia.TcPlay == TimeSpan.Zero ? _tcPlay : OriginalMedia.TcPlay;

        public override decimal AudioVolume => OriginalMedia?.AudioVolume ?? _audioVolume;

        protected override void DoDispose()
        {
            base.DoDispose();
            _directory.DeleteMedia(this);
        }

    }
}
