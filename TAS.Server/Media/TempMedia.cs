using System;
using System.IO;
using TAS.FFMpegUtils;
using TAS.Common;
using TAS.Common.Interfaces.Media;

namespace TAS.Server.Media
{
    public class TempMedia: MediaBase, ITempMedia
    {
       
        public TempMedia(TempDirectory directory, IMediaProperties originalMedia)
        {
            OriginalMedia = originalMedia;
            Directory = directory;
            MediaGuid = originalMedia?.MediaGuid ?? Guid.NewGuid();
            FileName = originalMedia == null 
                ? $"{MediaGuid}.tmp"
                : $"{originalMedia.MediaGuid}{Path.GetExtension(originalMedia.FileName)}";
        }

        internal IMediaProperties OriginalMedia;
        internal StreamInfo[] StreamInfo;

        public override TAudioChannelMapping AudioChannelMapping
        {
            get => OriginalMedia?.AudioChannelMapping ?? TAudioChannelMapping.Stereo;
            set { }
        }

        public override string MediaName
        {
            get => OriginalMedia?.MediaName ?? FileName;
            set { }
        }


        public override double AudioLevelIntegrated
        {
            get => OriginalMedia?.AudioLevelIntegrated ?? -23d;
            set {  }
        }

        public override double AudioLevelPeak
        {
            get => OriginalMedia?.AudioLevelPeak ?? 0d;
            set { }
        }

        public override double AudioVolume
        {
            get => OriginalMedia?.AudioVolume ?? 1d;
            set { }
        }

        public override TimeSpan Duration
        {
            get => OriginalMedia?.Duration ?? TimeSpan.Zero;
            set { }
        }

        public override TimeSpan DurationPlay
        {
            get => OriginalMedia?.DurationPlay ?? TimeSpan.Zero;
            set { }
        }

        public override TMediaCategory MediaCategory
        {
            get => OriginalMedia?.MediaCategory ?? TMediaCategory.Uncategorized;
            set {  }
        }

        public override byte Parental
        {
            get => OriginalMedia?.Parental ?? 0;
            set {  }
        }

        public override TimeSpan TcPlay
        {
            get => OriginalMedia?.TcPlay ?? TimeSpan.Zero;
            set { }
        }

        public override TimeSpan TcStart
        {
            get => OriginalMedia?.TcStart ?? TimeSpan.Zero;
            set { }
        }

        public override TVideoFormat VideoFormat
        {
            get => OriginalMedia?.VideoFormat ?? TVideoFormat.Other;
            set { }
        }

        public override bool FieldOrderInverted
        {
            get => OriginalMedia?.FieldOrderInverted ?? false;
            set { }
        }
        
        protected override void DoDispose()
        {
            Delete();
            base.DoDispose();
        }
    }
}
