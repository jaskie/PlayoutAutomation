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
            _originalMedia = originalMedia;
            Directory = directory;
            MediaGuid = originalMedia?.MediaGuid ?? Guid.NewGuid();
            FileName = originalMedia == null 
                ? $"{MediaGuid}.tmp"
                : $"{originalMedia.MediaGuid}{Path.GetExtension(originalMedia.FileName)}";
        }

        private IMediaProperties _originalMedia;
        internal StreamInfo[] StreamInfo;

        public override TAudioChannelMapping AudioChannelMapping
        {
            get => _originalMedia?.AudioChannelMapping ?? TAudioChannelMapping.Stereo;
            set { }
        }

        public override string MediaName
        {
            get => _originalMedia?.MediaName ?? FileName;
            set { }
        }


        public override double AudioLevelIntegrated
        {
            get => _originalMedia?.AudioLevelIntegrated ?? -23d;
            set {  }
        }

        public override double AudioLevelPeak
        {
            get => _originalMedia?.AudioLevelPeak ?? 0d;
            set { }
        }

        public override double AudioVolume
        {
            get => _originalMedia?.AudioVolume ?? 1d;
            set { }
        }

        public override TimeSpan Duration
        {
            get => _originalMedia?.Duration ?? TimeSpan.Zero;
            set { }
        }

        public override TimeSpan DurationPlay
        {
            get => _originalMedia?.DurationPlay ?? TimeSpan.Zero;
            set { }
        }

        public override TMediaCategory MediaCategory
        {
            get => _originalMedia?.MediaCategory ?? TMediaCategory.Uncategorized;
            set {  }
        }

        public override byte Parental
        {
            get => _originalMedia?.Parental ?? 0;
            set {  }
        }

        public override TimeSpan TcPlay
        {
            get => _originalMedia?.TcPlay ?? TimeSpan.Zero;
            set { }
        }

        public override TimeSpan TcStart
        {
            get => _originalMedia?.TcStart ?? TimeSpan.Zero;
            set { }
        }

        public override TVideoFormat VideoFormat
        {
            get => _originalMedia?.VideoFormat ?? TVideoFormat.Other;
            set { }
        }

        public override bool FieldOrderInverted
        {
            get => _originalMedia?.FieldOrderInverted ?? false;
            set { }
        }
        
        protected override void DoDispose()
        {
            Delete();
            base.DoDispose();
        }
    }
}
