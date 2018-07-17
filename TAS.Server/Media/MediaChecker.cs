using System;
using System.Diagnostics;
using TAS.FFMpegUtils;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Server.Media
{
    public static class MediaChecker
    {
        internal static void Check(this MediaBase media)
        {
            if (media.MediaType == TMediaType.Movie || media.MediaType == TMediaType.Unknown)
            {
                int startTickCunt = Environment.TickCount;
                using (FFMpegWrapper ffmpeg = new FFMpegWrapper(media.FullPath))
                {
                    Rational r = ffmpeg.GetFrameRate();
                    RationalNumber frameRate = new RationalNumber(r.Num, r.Den);
                    var videoDuration = (TimeSpan) ffmpeg.GetVideoDuration();
                    var audioDuration = (TimeSpan) ffmpeg.GetAudioDuration();
                    var mediaDuration = ((videoDuration > audioDuration) && (audioDuration > TimeSpan.Zero) ? audioDuration : videoDuration).Round(frameRate);
                    if (mediaDuration == TimeSpan.Zero)
                        mediaDuration = (TimeSpan) ffmpeg.GetFileDuration();
                    media.Duration = mediaDuration;
                    if (media.DurationPlay == TimeSpan.Zero || media.DurationPlay > mediaDuration)
                        media.DurationPlay = mediaDuration;
                    int w = ffmpeg.GetWidth();
                    int h = ffmpeg.GetHeight();
                    FieldOrder order = ffmpeg.GetFieldOrder();
                    Rational sar = ffmpeg.GetSAR();
                    if (h == 608 && w == 720)
                    {
                        media.HasExtraLines = true;
                        h = 576;
                    }
                    else
                        media.HasExtraLines = false;
                    string timecode = ffmpeg.GetTimeCode();
                    if (timecode != null
                        && timecode.IsValidSMPTETimecode(frameRate))
                    {
                        media.TcStart = timecode.SMPTETimecodeToTimeSpan(frameRate);
                        if (media.TcPlay < media.TcStart)
                            media.TcPlay = media.TcStart;
                    }                    

                    RationalNumber sAR = (h == 576 && ((sar.Num == 608 && sar.Den == 405) || (sar.Num == 1 && sar.Den == 1) || (sar.Num == 118 && sar.Den == 81))) ? VideoFormatDescription.Descriptions[TVideoFormat.PAL_FHA].SAR
                        : (sar.Num == 152 && sar.Den == 135) ? VideoFormatDescription.Descriptions[TVideoFormat.PAL].SAR
                        : new RationalNumber(sar.Num, sar.Den);
                    
                    var vfd = VideoFormatDescription.Match(new System.Drawing.Size(w, h), frameRate, sAR, order != FieldOrder.PROGRESSIVE);
                    media.VideoFormat = vfd.Format;
                    if (media is IngestMedia ingestMedia)
                        ingestMedia.StreamInfo = ffmpeg.GetStreamInfo();
                    if (media is TempMedia tempMedia)
                        tempMedia.StreamInfo = ffmpeg.GetStreamInfo();

                    Debug.WriteLine("FFmpeg check of {0} finished. It took {1} milliseconds", media.FullPath, Environment.TickCount - startTickCunt);

                    if (videoDuration > TimeSpan.Zero)
                    {
                        media.MediaType = TMediaType.Movie;
                        if (Math.Abs(videoDuration.Ticks - audioDuration.Ticks) >= TimeSpan.TicksPerSecond
                            && audioDuration != TimeSpan.Zero)
                            // when more than 0.5 sec difference
                            media.MediaStatus = TMediaStatus.ValidationError;
                        media.MediaStatus = TMediaStatus.Available;
                    }
                    media.MediaStatus = TMediaStatus.ValidationError;
                }
            }
            media.MediaStatus = TMediaStatus.Available;
        }
    }
}

