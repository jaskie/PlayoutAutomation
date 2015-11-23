using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.Model
{
    class ConvertOperation : FileOperation, IConvertOperation
    {
        public TAspectConversion AspectConversion { get; set; }
        public TAudioChannelMappingConversion AudioChannelMappingConversion { get; set; }
        public decimal AudioVolume { get; set; }
        public TVideoFormat OutputFormat { get; set; }
        public TFieldOrder SourceFieldOrderEnforceConversion { get; set; }
    }
}
