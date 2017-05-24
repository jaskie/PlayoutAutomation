using System;

namespace TAS.Server.Common.Interfaces
{
    public interface IConvertOperation: IFileOperation
    {
        TAspectConversion AspectConversion { get; set; }
        TAudioChannelMappingConversion AudioChannelMappingConversion { get; set; }
        TFieldOrder SourceFieldOrderEnforceConversion { get; set; }
        decimal AudioVolume { get; set; }
        bool Trim { get; set; }
        bool LoudnessCheck { get; set; }
        TimeSpan StartTC { get; set; }
        TimeSpan Duration { get; set; }
    }
}
