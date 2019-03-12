using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Server.Media;

namespace TAS.Server.MediaOperation
{
    public class LoudnessOperation : FileOperationBase, ILoudnessOperation
    {

        private static readonly string lLufsPattern = @"    I:\s*-?\d*\.?\d* LUFS";
        private static readonly string lPeakPattern = @"    Peak:\s*-?\d*\.?\d* dBFS";
        private static readonly string LufsPattern = @"-?\d+\.\d";
        private static readonly string lProgressPattern = @" t: \d*\.?\d*";
        private static readonly string ProgressPattern = @"\d+\.?\d*";
        private static readonly Regex RegexlLufs = new Regex(lLufsPattern, RegexOptions.None);
        private static readonly Regex RegexlPeak = new Regex(lPeakPattern, RegexOptions.None);
        private static readonly Regex RegexLoudnesslProgress = new Regex(lProgressPattern, RegexOptions.None);
        private static readonly Regex RegexLoudnessProgress = new Regex(ProgressPattern, RegexOptions.None);

        private double _loudness;
        private double _samplePeak = double.MinValue;
        private bool _loudnessMeasured;
        private bool _samplePeakMeasured;
        private IMedia _source;

        public LoudnessOperation(FileManager ownerFileManager) : base(ownerFileManager)
        {
            TryCount = 1;
        }

        [JsonProperty]
        public IMedia Source { get => _source; set => SetField(ref _source, value); }

        public event EventHandler<AudioVolumeEventArgs> AudioVolumeMeasured; // will not save to Media object if not null

        [JsonProperty]
        public TimeSpan MeasureStart { get; set; }

        [JsonProperty]
        public TimeSpan MeasureDuration { get; set; }

        protected override void OnOperationStatusChanged()
        {
            
        }

        protected override async Task<bool> InternalExecute()
        {
            StartTime = DateTime.UtcNow;
            if (!(Source is MediaBase source))
                throw new ArgumentException("LoudnessOperation: Source is not of type MediaBase");
            if (source.Directory is IngestDirectory directory &&
                directory.AccessType != TDirectoryAccessType.Direct)
                using (var localSourceMedia = (TempMedia) OwnerFileManager.TempDirectory.CreateMedia(source))
                {
                    if (!await source.CopyMediaTo(localSourceMedia, CancellationTokenSource.Token))
                        return false;
                    return await DoExecute(localSourceMedia);
                }

            return await DoExecute(source);
        }

        private async Task<bool> DoExecute(MediaBase inputMedia)
        {
            string Params = $"-nostats -i \"{inputMedia.FullPath}\" -ss {MeasureStart} -t {(MeasureDuration == TimeSpan.Zero ? inputMedia.DurationPlay : MeasureDuration)} -filter_complex ebur128=peak=sample -f null -";
            var helper = new FFMpegHelper(this, MeasureDuration);
            helper.DataReceived += DataReceived;
            return await helper.RunProcess(Params);
        }

        protected void DataReceived(object sender, DataReceivedEventArgs outLine)
        {
            // Collect the process command output. 
            if (string.IsNullOrEmpty(outLine.Data))
                return;
            var lineMatch = RegexLoudnesslProgress.Match(outLine.Data);
            if (lineMatch.Success)
            {
                var valueMatch = RegexLoudnessProgress.Match(lineMatch.Value);
                if (!valueMatch.Success)
                    return;
                var totalSeconds = Source.Duration.TotalSeconds;
                if (Math.Abs(totalSeconds) > double.Epsilon
                    && double.TryParse(valueMatch.Value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var currentPos))
                    Progress = (int)((currentPos * 100) / totalSeconds);
            }
            else
            {
                var luFsLineMatch = RegexlLufs.Match(outLine.Data);
                if (luFsLineMatch.Success)
                {
                    var regexLufs = new Regex(LufsPattern, RegexOptions.None);
                    var valueMatch = regexLufs.Match(luFsLineMatch.Value);
                    if (valueMatch.Success)
                        _loudnessMeasured = (double.TryParse(valueMatch.Value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out _loudness));
                }
                if (!_samplePeakMeasured)
                {
                    var truePeakLineMatch = RegexlPeak.Match(outLine.Data);
                    if (truePeakLineMatch.Success)
                    {
                        var regexLufs = new Regex(LufsPattern, RegexOptions.None);
                        var valueMatch = regexLufs.Match(truePeakLineMatch.Value);
                        if (valueMatch.Success)
                            _samplePeakMeasured = double.TryParse(valueMatch.Value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out _samplePeak);
                        else
                            _loudnessMeasured = false;
                    }
                }
                if (_samplePeakMeasured && _loudnessMeasured)
                {
                    var volume = -Math.Max(_loudness - OwnerFileManager.ReferenceLoudness, _samplePeak); // prevents automatic amplification over 0dBFS
                    var h = AudioVolumeMeasured;
                    if (h == null)
                    {
                        Source.AudioLevelIntegrated = _loudness;
                        Source.AudioLevelPeak = _samplePeak;
                        Source.AudioVolume = volume;
                        (Source as PersistentMedia)?.Save();
                    }
                    else
                        h(this, new AudioVolumeEventArgs(volume));
                }
                AddOutputMessage(outLine.Data);
            }
        }
    }

}
