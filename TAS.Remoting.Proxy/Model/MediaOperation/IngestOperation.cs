using jNet.RPC;
using System;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Remoting.Model.Media;

namespace TAS.Remoting.Model.MediaOperation
{
    public class IngestOperation : FileOperationBase, IIngestOperation
    {

#pragma warning disable CS0649

        [DtoField(nameof(IIngestOperation.DestProperties))]
        private IMediaProperties _destProperties;

        [DtoField(nameof(IIngestOperation.DestDirectory))]
        private MediaDirectoryBase _destDirectory;

        [DtoField(nameof(IIngestOperation.Source))]
        private MediaBase _source;

        [DtoField(nameof(IIngestOperation.AspectConversion))]
        private TAspectConversion _aspectConversion;

        [DtoField(nameof(IIngestOperation.AudioChannelMappingConversion))]
        private TAudioChannelMappingConversion _audioChannelMappingConversion;

        [DtoField(nameof(IIngestOperation.AudioVolume))]
        private double _audioVolume;

        [DtoField(nameof(IIngestOperation.SourceFieldOrderEnforceConversion))]
        private TFieldOrder _sourceFieldOrderEnforceConversion;

        [DtoField(nameof(IIngestOperation.StartTC))]
        private TimeSpan _startTc;

        [DtoField(nameof(IIngestOperation.Duration))]
        private TimeSpan _duration;

        [DtoField(nameof(IIngestOperation.Trim))]
        private bool _trim;

        [DtoField(nameof(IIngestOperation.LoudnessCheck))]
        private bool _loudnessCheck;
        

#pragma warning restore

        public IMediaProperties DestProperties { get => _destProperties; set => Set(value); }

        public IMediaDirectory DestDirectory { get => _destDirectory; set => Set(value); }

        public IMedia Source { get => _source; set => Set(value); }

        public TAspectConversion AspectConversion { get => _aspectConversion; set => Set(value); }

        public TAudioChannelMappingConversion AudioChannelMappingConversion { get => _audioChannelMappingConversion; set => Set(value); }

        public double AudioVolume { get => _audioVolume; set => Set(value); }

        public TFieldOrder SourceFieldOrderEnforceConversion { get => _sourceFieldOrderEnforceConversion; set => Set(value); }

        public TimeSpan StartTC { get => _startTc; set => Set(value); }

        public TimeSpan Duration { get => _duration; set => Set(value); }

        public bool Trim { get => _trim; set => Set(value); }

        public bool LoudnessCheck { get => _loudnessCheck; set => Set(value); }
    }
}
