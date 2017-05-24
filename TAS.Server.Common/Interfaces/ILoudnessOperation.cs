using System;

namespace TAS.Server.Common.Interfaces
{
    public interface ILoudnessOperation: IFileOperation
    {
        TimeSpan MeasureStart { get; set; }
        TimeSpan MeasureDuration { get; set; }
        event EventHandler<AudioVolumeEventArgs> AudioVolumeMeasured;
    }
}
