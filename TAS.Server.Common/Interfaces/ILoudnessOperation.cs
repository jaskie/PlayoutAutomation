using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Common;

namespace TAS.Server.Interfaces
{
    public interface ILoudnessOperation: IFileOperation
    {
        TimeSpan MeasureStart { get; set; }
        TimeSpan MeasureDuration { get; set; }
        event EventHandler<AudioVolumeEventArgs> AudioVolumeMeasured;
    }
}
