using System;
using Newtonsoft.Json;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class IngestOperation : FileOperation, IIngestOperation
    {

#pragma warning disable CS0649

        [JsonProperty(nameof(IIngestOperation.AspectConversion))]
        private TAspectConversion _aspectConversion;

        [JsonProperty(nameof(IIngestOperation.AudioChannelMappingConversion))]
        private TAudioChannelMappingConversion _audioChannelMappingConversion;

        [JsonProperty(nameof(IIngestOperation.AudioVolume))]
        private double _audioVolume;

        [JsonProperty(nameof(IIngestOperation.SourceFieldOrderEnforceConversion))]
        private TFieldOrder _sourceFieldOrderEnforceConversion;

        [JsonProperty(nameof(IIngestOperation.StartTC))]
        private TimeSpan _startTc;

        [JsonProperty(nameof(IIngestOperation.Duration))]
        private TimeSpan _duration;

        [JsonProperty(nameof(IIngestOperation.Trim))]
        private bool _trim;

        [JsonProperty(nameof(IIngestOperation.LoudnessCheck))]
        private bool _loudnessCheck;

        [JsonProperty(nameof(IIngestOperation.MovieContainerFormat))]
        private TMovieContainerFormat _movieContainerFormat;

#pragma warning restore

        public TAspectConversion AspectConversion { get => _aspectConversion; set => Set(value); }
        public TAudioChannelMappingConversion AudioChannelMappingConversion { get => _audioChannelMappingConversion; set => Set(value); }
        public double AudioVolume { get => _audioVolume; set => Set(value); }
        public TFieldOrder SourceFieldOrderEnforceConversion { get => _sourceFieldOrderEnforceConversion; set => Set(value); }
        public TimeSpan StartTC { get => _startTc; set => Set(value); }
        public TimeSpan Duration { get => _duration; set => Set(value); }
        public TMovieContainerFormat MovieContainerFormat { get => _movieContainerFormat; set => Set(value); }
        public bool Trim { get => _trim; set => Set(value); }
        public bool LoudnessCheck { get => _loudnessCheck; set => Set(value); }

    }
}
