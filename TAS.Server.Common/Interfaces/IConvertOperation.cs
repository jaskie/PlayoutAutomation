using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Common;

namespace TAS.Server.Interfaces
{
    public interface IConvertOperation: IFileOperation
    {
        TAspectConversion AspectConversion { get; set; }
        TAudioChannelMappingConversion AudioChannelMappingConversion { get; set; }
        TFieldOrder SourceFieldOrderEnforceConversion { get; set; }
        TVideoFormat OutputFormat { get; set; }
        decimal AudioVolume { get; set; }
    }
}
