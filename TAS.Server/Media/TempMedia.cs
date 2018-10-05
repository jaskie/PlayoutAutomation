using System;
using System.IO;
using TAS.FFMpegUtils;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;

namespace TAS.Server.Media
{
    public class TempMedia: MediaBase, ITempMedia
    {
       
        public TempMedia(TempDirectory directory, IMediaProperties originalMedia)
        {
            OriginalMedia = originalMedia;
            FileName = $"{MediaGuid}{Path.GetExtension(originalMedia?.FileName ?? FileUtils.DefaultFileExtension(TMediaType.Unknown))}";
        }

        internal IMediaProperties OriginalMedia;
        internal StreamInfo[] StreamInfo;

        public override TAudioChannelMapping AudioChannelMapping
        {
            get { return OriginalMedia?.AudioChannelMapping ?? TAudioChannelMapping.Stereo; }
            set { }
        }

        public override double AudioLevelIntegrated
        {
            get { return OriginalMedia?.AudioLevelIntegrated ?? -23d; }
            set {  }
        }

        public override double AudioLevelPeak
        {
            get { return OriginalMedia?.AudioLevelPeak ?? 0d; }
            set { }
        }

        public override double AudioVolume
        {
            get { return OriginalMedia?.AudioVolume ?? 1d; }
            set { }
        }

        public override TimeSpan Duration
        {
            get { return OriginalMedia?.Duration ?? TimeSpan.Zero; }
            set { }
        }

        public override TimeSpan DurationPlay
        {
            get { return OriginalMedia?.DurationPlay ?? TimeSpan.Zero; }
            set { }
        }

        public override TMediaCategory MediaCategory
        {
            get { return OriginalMedia?.MediaCategory ?? TMediaCategory.Uncategorized; }
            set {  }
        }

        public override byte Parental
        {
            get { return OriginalMedia?.Parental ?? 0; }
            set {  }
        }

        public override TimeSpan TcPlay
        {
            get { return OriginalMedia?.TcPlay ?? TimeSpan.Zero; }
            set { }
        }

        public override TimeSpan TcStart
        {
            get { return OriginalMedia?.TcStart ?? TimeSpan.Zero; }
            set { }
        }

        public override TVideoFormat VideoFormat
        {
            get { return OriginalMedia?.VideoFormat ?? TVideoFormat.Other; }
            set { }
        }

        public override bool FieldOrderInverted
        {
            get { return OriginalMedia?.FieldOrderInverted ?? false; }
            set { }
        }
        
        protected override void DoDispose()
        {
            Delete();
            base.DoDispose();
        }
    }
}
