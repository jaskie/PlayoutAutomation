using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Diagnostics;
using System.Configuration;
using System.Text.RegularExpressions;
using MediaInfoLib;
using System.Threading;
using System.Runtime.Remoting.Messaging;
using System.Globalization;
using System.Text;
using System.ComponentModel;

namespace TAS.Server
{
    public class ConvertOperation : FileOperation
    {
        

        #region Properties

        private bool _hasMoreThan8AudioChannels = false;
        private TimeSpan _duration;

        private static string _ffExe = "ffmpeg.exe";
        private static string _lProgressPattern = "time=" + @"\d\d:\d\d:\d\d\.?\d*";
        private static string _progressPattern = @"\d\d:\d\d:\d\d\.?\d*";
        private readonly Regex _regexlProgress = new Regex(_lProgressPattern, RegexOptions.None);
        private readonly Regex _regexProgress = new Regex(_progressPattern, RegexOptions.None);

        public ConvertOperation()
        {
            Kind = TFileOperationKind.Convert;
            AspectConversion = AspectConversions.NoConversion;
            SourceFieldOrderEnforceConversion = SourceFieldOrderEnforceConversions.Detect;
            AudioChannelMappingConversion = AudioChannelMappingConversions.FirstTwoChannels;
        }

        #endregion // properties

        #region CheckFile
        private void CheckInputFile(Media mf)
        {
            MediaInfo mi = new MediaInfo();
            try
            {
                mi.Open(mf.FullPath);
                mi.Option("Complete");
                _hasMoreThan8AudioChannels = mi.Count_Get(StreamKind.Audio) > 8;
                string miOutput = mi.Inform();

                Regex format_lxf = new Regex("Format\\s*:\\s*LXF");
                if (format_lxf.Match(miOutput).Success)
                {
                    string[] miOutputLines = miOutput.Split('\n');
                    Regex vitc = new Regex("ATC_VITC");
                    Regex re = new Regex("Time code of first frame\\s*:[\\s]\\d{2}:\\d{2}:\\d{2}:\\d{2}");
                    for (int i = 0; i < miOutputLines.Length; i++)
                    {
                        if (vitc.Match(miOutputLines[i]).Success && i >= 1)
                        {
                            Match m_tcs = re.Match(miOutputLines[i - 1]);
                            if (m_tcs.Success)
                            {
                                Regex reg_tc = new Regex("\\d{2}:\\d{2}:\\d{2}:\\d{2}");
                                Match m_tc = reg_tc.Match(m_tcs.Value);
                                if (m_tc.Success)
                                {
                                    DestMedia.TCStart = Common.SMPTETimecode.TimecodeToTimeSpan(reg_tc.Match(m_tc.Value).Value);
                                    if (DestMedia.TCPlay == TimeSpan.Zero)
                                        DestMedia.TCPlay = DestMedia.TCStart;
                                    break;
                                }
                            }
                        }
                    }
                }

                Regex format_mxf = new Regex(@"Format\s*:\s*MXF");
                if (format_mxf.Match(miOutput).Success)
                {
                    string[] miOutputLines = miOutput.Split('\n');
                    Regex mxf_tc = new Regex(@"MXF TC");
                    Regex re = new Regex(@"Time code of first frame\s*:[\s]\d{2}:\d{2}:\d{2}:\d{2}");
                    for (int i = 0; i < miOutputLines.Length; i++)
                    {
                        Match mxf_match = mxf_tc.Match(miOutputLines[i]);
                        if (mxf_match.Success && i < miOutputLines.Length - 1)
                        {
                            Regex reg_tc = new Regex(@"\d{2}:\d{2}:\d{2}:\d{2}");
                            Match m_tc = re.Match(miOutputLines[i + 1]);
                            if (m_tc.Success)
                            {
                                DestMedia.TCStart = Common.SMPTETimecode.TimecodeToTimeSpan(reg_tc.Match(m_tc.Value).Value);
                                if (DestMedia.TCPlay == TimeSpan.Zero)
                                    DestMedia.TCPlay = DestMedia.TCStart;
                                break;
                            }
                        }
                    }
                }
            }
            finally
            {
                mi.Close();
            }
        }


        #endregion // Checkfile

        private MediaConversion _aspectConversion;
        public MediaConversion AspectConversion { get { return _aspectConversion; } set { SetField(ref _aspectConversion, value, "AspectConversion"); } }

        private MediaConversion _audioChannelMappingConversion;
        public MediaConversion AudioChannelMappingConversion { get { return _audioChannelMappingConversion; } set { SetField(ref _audioChannelMappingConversion, value, "AudioChannelMappingConversion"); } }

        private decimal _audioVolume;
        public decimal AudioVolume
        {
            get { return _audioVolume; }
            set { SetField(ref _audioVolume, value, "AudioVolume");}
        }

        private MediaConversion _sourceFieldOrderEnforceConversion;
        public MediaConversion SourceFieldOrderEnforceConversion { get { return _sourceFieldOrderEnforceConversion; } set { SetField(ref _sourceFieldOrderEnforceConversion, value, "SourceFieldOrderEnforceConversion"); } }

        
        private bool RunProcess(string parameters)
        {
            //create a process info
            ProcessStartInfo oInfo = new ProcessStartInfo(ConvertOperation._ffExe, parameters);
            oInfo.UseShellExecute = false;
            oInfo.CreateNoWindow = true;
            oInfo.RedirectStandardError = true;

            //try the process
            Debug.WriteLine(parameters, "Starting ffmpeg with parameters");
            try
            {
                using (Process _procFFmpeg = Process.Start(oInfo))
                {
                    _procFFmpeg.ErrorDataReceived += ProcOutputHandler;
                    _procFFmpeg.BeginErrorReadLine();
                    bool finished = false;
                    while (!(Aborted || finished))
                        finished = _procFFmpeg.WaitForExit(1000);
                    if (Aborted)
                    {
                        _procFFmpeg.Kill();
                        Thread.Sleep(1000);
                        DestMedia.Delete();
                        Debug.WriteLine(this, "Aborted");
                    }
                    return finished && (_procFFmpeg.ExitCode == 0);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message, "Error running FFmpeg process");
                return false;
            }
        }

        internal override bool Do()
        {
            if (Kind == TFileOperationKind.Convert)
            {
                StartTime = DateTime.UtcNow;
                OperationStatus = FileOperationStatus.InProgress;
                IsIndeterminate = true;
                try
                {

                    if (!(SourceMedia is IngestMedia))
                        throw new Exception("ConvertOperation: source media is not of type IngestMedia");
                    bool success = false;
                    if (SourceMedia.Directory.AccessType != TDirectoryAccessType.Direct)
                        using (TempMedia _localSourceMedia = FileManager.TempDirectory.Get(SourceMedia))
                        {
                            _localSourceMedia.PropertyChanged += _localSourceMedia_PropertyChanged;
                            if (SourceMedia.CopyMediaTo(_localSourceMedia, ref _aborted))
                            {
                                _localSourceMedia.Verify();
                                _duration = _localSourceMedia._duration;
                                try
                                {
                                    success = _do(_localSourceMedia);
                                }
                                finally
                                {
                                    _localSourceMedia.PropertyChanged -= _localSourceMedia_PropertyChanged;
                                }

                                if (!success)
                                    TryCount--;
                                return success;
                            }
                            return false;
                        }

                    else
                    {
                        _duration = SourceMedia.Duration;
                        success = _do(SourceMedia);
                        if (!success)
                            TryCount--;
                        return success;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    TryCount--;
                    return false;
                }

            }
            else
                return base.Do();
        }

        void _localSourceMedia_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FileSize")
            {
                ulong fs = SourceMedia.FileSize;
                if (fs > 0 && sender is Media)
                    Progress = (int)(((sender as Media).FileSize * 100ul) / fs);
            }
        }

        private void _addConversion(MediaConversion conversion, StringBuilder parameters, List<string> videoFilters, List<string> audioFilters)
        {
            if (conversion != null)
            {
                if (!string.IsNullOrWhiteSpace(conversion.FFMpegParameter))
                    parameters.AppendFormat(" -{0}", conversion.FFMpegParameter);
                if (!string.IsNullOrWhiteSpace(conversion.FFMpegAudioFilter))
                    audioFilters.Add(conversion.FFMpegAudioFilter);
                if (!string.IsNullOrWhiteSpace(conversion.FFMpegVideoFilter))
                    videoFilters.Add(conversion.FFMpegVideoFilter);
            }
        }

        private string _encodeParameters(Media inputMedia)
        {
            List<string> vf = new List<string>();
            List<string> af = new List<string>();
            StringBuilder ep = new StringBuilder(((IngestDirectory)SourceMedia.Directory).EncodeParams);

            if (inputMedia.HasExtraLines)
            {
                vf.Add("crop=720:576:0:32");
                vf.Add("setdar=dar=16/9");
            }
            if (_hasMoreThan8AudioChannels)
                af.Add("aformat=channel_layouts=0xFFFF");
            _addConversion(AspectConversion, ep, vf, af);
            _addConversion(SourceFieldOrderEnforceConversion, ep, vf, af);
            _addConversion(AudioChannelMappingConversion, ep, vf, af);
            if (AudioVolume != decimal.Zero)
                _addConversion(new MediaConversionAudioVolume(AudioVolume), ep, vf, af);
            if (vf.Any())
                ep.AppendFormat(" -filter:v \"{0}\"", string.Join(", ", vf));
            if (af.Any())
                ep.AppendFormat(" -filter:a \"{0}\"", string.Join(", ", af));
            return ep.ToString();
        }

        private bool _do(Media inputMedia)
        {
            Debug.WriteLine(this, "Convert operation started");
            DestMedia.MediaStatus = TMediaStatus.Copying;
            string encodeParams = _encodeParameters(inputMedia);

            CheckInputFile(inputMedia);

            string Params = string.Format("-i \"{0}\" -y {1} -timecode {2} \"{3}\"",
                    inputMedia.FullPath,
                    encodeParams,
                    Common.SMPTETimecode.TimeSpanToTimeCode(DestMedia.TCStart),
                    DestMedia.FullPath);

            if (DestMedia is ArchiveMedia && !Directory.Exists(Path.GetDirectoryName(DestMedia.FullPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(DestMedia.FullPath));
            if (AspectConversion != null)
                DestMedia.VideoFormat = (TVideoFormat)AspectConversion.OutputFormat;
            if (AudioChannelMappingConversion != null)
                DestMedia.AudioChannelMapping = (TAudioChannelMapping)AudioChannelMappingConversion.OutputFormat;
            if (RunProcess(Params)  // FFmpeg 
                && DestMedia.FileExists())
            {
                DestMedia.MediaStatus = TMediaStatus.Copied;
                DestMedia.Verify();
                if (Math.Abs(DestMedia.Duration.Ticks - inputMedia.Duration.Ticks) > TimeSpan.TicksPerSecond / 2)
                {
                    DestMedia.MediaStatus = TMediaStatus.CopyError;
                    if (DestMedia is PersistentMedia)
                        (DestMedia as PersistentMedia).Save();
                    Debug.WriteLine(this, "Convert operation succeed, but durations are diffrent");
                }
                else
                {
                    if ((SourceMedia.Directory is IngestDirectory) && ((IngestDirectory)SourceMedia.Directory).DeleteSource)
                        FileManager.Queue(new FileOperation { Kind = TFileOperationKind.Delete, SourceMedia = SourceMedia });
                    Debug.WriteLine(this, "Convert operation succeed");
                }
                OperationStatus = FileOperationStatus.Finished;
                return true;
            }
            Debug.WriteLine("FFmpeg rewraper Do(): Failed for {0}. Command line was {1}", (object)SourceMedia, Params);
            return false;
        }

        private void ProcOutputHandler(object sendingProcess,
            DataReceivedEventArgs outLine)
        {
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                Match mProgressLine = _regexlProgress.Match(outLine.Data);
                if (mProgressLine.Success)
                {
                    Match mProgressVal = _regexProgress.Match(mProgressLine.Value);
                    if (mProgressVal.Success)
                    {
                        TimeSpan progressSeconds;
                        long duration = _duration.Ticks;
                        if (duration>0 
                            && TimeSpan.TryParse(mProgressVal.Value.Trim(), CultureInfo.InvariantCulture, out progressSeconds))
                            Progress = (int)((progressSeconds.Ticks * 100) / duration);
                    }
                }
            }
        }

    }

}
 