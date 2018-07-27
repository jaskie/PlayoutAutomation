using System;

namespace TAS.Common.Interfaces
{
    public interface IIngestOperation: IFileOperation
    {
        TAspectConversion AspectConversion { get; set; }
        TAudioChannelMappingConversion AudioChannelMappingConversion { get; set; }
        TFieldOrder SourceFieldOrderEnforceConversion { get; set; }
        double AudioVolume { get; set; }
        bool Trim { get; set; }
        bool LoudnessCheck { get; set; }
        TimeSpan StartTC { get; set; }
        TimeSpan Duration { get; set; }
        TMovieContainerFormat MovieContainerFormat { get; set; }
    }
}
