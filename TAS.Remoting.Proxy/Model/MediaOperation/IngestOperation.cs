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

        [DtoMember(nameof(IIngestOperation.DestProperties))]
        private IMediaProperties _destProperties;

        [DtoMember(nameof(IIngestOperation.DestDirectory))]
        private MediaDirectoryBase _destDirectory;

        [DtoMember(nameof(IIngestOperation.Source))]
        private MediaBase _source;

        [DtoMember(nameof(IIngestOperation.AspectConversion))]
        private TAspectConversion _aspectConversion;

        [DtoMember(nameof(IIngestOperation.AudioChannelMappingConversion))]
        private TAudioChannelMappingConversion _audioChannelMappingConversion;

        [DtoMember(nameof(IIngestOperation.AudiodescriptionChannelMappingConversion))]
        private TAudioChannelMappingConversion _audiodescriptionChannelMappingConversion;

        [DtoMember(nameof(IIngestOperation.AudioVolume))]
        private double _audioVolume;

        [DtoMember(nameof(IIngestOperation.SourceFieldOrderEnforceConversion))]
        private TFieldOrder _sourceFieldOrderEnforceConversion;

        [DtoMember(nameof(IIngestOperation.StartTC))]
        private TimeSpan _startTc;

        [DtoMember(nameof(IIngestOperation.Duration))]
        private TimeSpan _duration;

        [DtoMember(nameof(IIngestOperation.Trim))]
        private bool _trim;

        [DtoMember(nameof(IIngestOperation.LoudnessCheck))]
        private bool _loudnessCheck;


#pragma warning restore

        public IMediaProperties DestProperties { get => _destProperties; set => Set(value); }

        public IMediaDirectory DestDirectory { get => _destDirectory; set => Set(value); }

        public IMedia Source { get => _source; set => Set(value); }

        public TAspectConversion AspectConversion { get => _aspectConversion; set => Set(value); }

        public TAudioChannelMappingConversion AudioChannelMappingConversion { get => _audioChannelMappingConversion; set => Set(value); }

        public TAudioChannelMappingConversion AudiodescriptionChannelMappingConversion { get => _audiodescriptionChannelMappingConversion; set => Set(value); }

        public double AudioVolume { get => _audioVolume; set => Set(value); }

        public TFieldOrder SourceFieldOrderEnforceConversion { get => _sourceFieldOrderEnforceConversion; set => Set(value); }

        public TimeSpan StartTC { get => _startTc; set => Set(value); }

        public TimeSpan Duration { get => _duration; set => Set(value); }

        public bool Trim { get => _trim; set => Set(value); }

        public bool LoudnessCheck { get => _loudnessCheck; set => Set(value); }
    }
}
