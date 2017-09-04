using System;
using System.IO;
using TAS.FFMpegUtils;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Server.Media
{
    public class TempMedia: MediaBase, ITempMedia
    {
       
        public TempMedia(TempDirectory directory, IMediaProperties originalMedia): base(directory, originalMedia.MediaGuid)
        {
            OriginalMedia = originalMedia;
            FileName = $"{originalMedia.MediaGuid}{Path.GetExtension(originalMedia.FileName)}";
        }

        internal IMediaProperties OriginalMedia;
        internal StreamInfo[] StreamInfo;

        public override TAudioChannelMapping AudioChannelMapping
        {
            get { return OriginalMedia.AudioChannelMapping; }
            set { }
        }

        public override double AudioLevelIntegrated
        {
            get { return OriginalMedia.AudioLevelIntegrated; }
            set {  }
        }

        public override double AudioLevelPeak
        {
            get { return OriginalMedia.AudioLevelPeak; }
            set { }
        }

        public override double AudioVolume
        {
            get { return OriginalMedia.AudioVolume; }
            set { }
        }

        public override TimeSpan Duration
        {
            get { return OriginalMedia.Duration; }
            set { }
        }

        public override TimeSpan DurationPlay
        {
            get { return OriginalMedia.DurationPlay; }
            set { }
        }

        public override TMediaCategory MediaCategory
        {
            get { return OriginalMedia.MediaCategory; }
            set {  }
        }

        public override byte Parental
        {
            get { return OriginalMedia.Parental; }
            set {  }
        }

        public override string MediaName
        {
            get { return OriginalMedia.MediaName; }
            set { }
        }

        public override TMediaType MediaType
        {
            get { return OriginalMedia.MediaType; }
            set { }
        }

        public override TimeSpan TcPlay
        {
            get { return OriginalMedia.TcPlay; }
            set { }
        }

        public override TimeSpan TcStart
        {
            get { return OriginalMedia.TcStart; }
            set { }
        }

        public override TVideoFormat VideoFormat
        {
            get { return OriginalMedia.VideoFormat; }
            set { }
        }

        public override bool FieldOrderInverted
        {
            get { return OriginalMedia.FieldOrderInverted; }
            set { }
        }
        
        protected override void DoDispose()
        {
            Delete();
            base.DoDispose();
        }
    }
}
