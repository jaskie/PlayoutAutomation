using System;
using System.Diagnostics;
using TAS.FFMpegUtils;
using TAS.Common;
using NLog;

namespace TAS.Server.Media
{
    public static class MediaChecker
    {
        internal static void Check(this MediaBase media, bool updateFormatAndDurations)
        {
            if (media.MediaType == TMediaType.Movie || media.MediaType == TMediaType.Still || media.MediaType == TMediaType.Unknown)
            {
#if DEBUG
                var startTickCunt = Environment.TickCount;
#endif
                using (var ffmpeg = new FFMpegWrapper(media.FullPath))
                    try
                    {
                        var r = ffmpeg.GetFrameRate();
                        var frameRate = new RationalNumber(r.Num, r.Den);
                        var videoDuration = ffmpeg.GetVideoDuration();
                        var audioDuration = ffmpeg.GetAudioDuration();
                        var mediaDuration = ((videoDuration > audioDuration) && (audioDuration > TimeSpan.Zero)
                            ? audioDuration
                            : videoDuration).Round(frameRate);
                        if (mediaDuration == TimeSpan.Zero)
                            mediaDuration = ffmpeg.GetFileDuration();
                        var vfd = VideoFormatDescription.Descriptions[media.VideoFormat];
                        if (updateFormatAndDurations)
                        {
                            media.Duration = mediaDuration;
                            if (media.DurationPlay == TimeSpan.Zero || media.DurationPlay > mediaDuration)
                                media.DurationPlay = mediaDuration;
                            var w = ffmpeg.GetWidth();
                            var h = ffmpeg.GetHeight();
                            var order = ffmpeg.GetFieldOrder();
                            var sar = ffmpeg.GetSAR();
                            if (h == 608 && w == 720)
                            {
                                media.HasExtraLines = true;
                                h = 576;
                            }
                            else
                                media.HasExtraLines = false;
                            var timecode = ffmpeg.GetTimeCode();
                            if (timecode != null
                                && timecode.IsValidSmpteTimecode(frameRate))
                            {
                                media.TcStart = timecode.SmpteTimecodeToTimeSpan(frameRate);
                                if (media.TcPlay < media.TcStart)
                                    media.TcPlay = media.TcStart;
                            }

                            var sAR =
                            (h == 576 && ((sar.Num == 608 && sar.Den == 405) || (sar.Num == 1 && sar.Den == 1) ||
                                          (sar.Num == 118 && sar.Den == 81)))
                                ? VideoFormatDescription.Descriptions[TVideoFormat.PAL_FHA].SAR
                                : (sar.Num == 152 && sar.Den == 135)
                                    ? VideoFormatDescription.Descriptions[TVideoFormat.PAL].SAR
                                    : new RationalNumber(sar.Num, sar.Den);

                            vfd = VideoFormatDescription.Match(new System.Drawing.Size(w, h), frameRate, sAR, order != FieldOrder.PROGRESSIVE);
                            media.VideoFormat = vfd.Format;
                            media.HasTransparency = ffmpeg.GetHasTransparency();
                        }
                        if (media is IngestMedia ingestMedia)
                            ingestMedia.StreamInfo = ffmpeg.GetStreamInfo();
                        if (media is TempMedia tempMedia)
                            tempMedia.StreamInfo = ffmpeg.GetStreamInfo();
#if DEBUG
                        Debug.WriteLine("FFmpeg check of {0} finished. It took {1} milliseconds", media.FullPath, Environment.TickCount - startTickCunt);
#endif
                        if (mediaDuration > vfd.FrameDuration)
                            media.MediaType = TMediaType.Movie;
                        else
                            media.MediaType = TMediaType.Still;
                        if (audioDuration != TimeSpan.Zero && Math.Abs(videoDuration.Ticks - audioDuration.Ticks) >= TimeSpan.TicksPerSecond) // when more than 1 sec difference
                            media.MediaStatus = TMediaStatus.ValidationError;
                        else
                            media.MediaStatus = TMediaStatus.Available;
                    }
                    catch
                    {
                        media.MediaStatus = TMediaStatus.ValidationError;
                        throw;
                    }
            }
            else
                media.MediaStatus = TMediaStatus.Available;
        }
    }
}

