using System;

namespace TAS.Common.Interfaces
{
    public interface ILoudnessOperation: IFileOperation
    {
        TimeSpan MeasureStart { get; set; }
        TimeSpan MeasureDuration { get; set; }
        event EventHandler<AudioVolumeEventArgs> AudioVolumeMeasured;
    }
}
