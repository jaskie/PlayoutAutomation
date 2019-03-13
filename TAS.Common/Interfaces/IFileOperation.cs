using System;
using System.Collections.Generic;
using System.ComponentModel;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Common.Interfaces
{
    public interface IFileOperationBase: INotifyPropertyChanged
    {
        DateTime ScheduledTime { get; }
        DateTime StartTime { get; }
        DateTime FinishedTime { get; }
        FileOperationStatus OperationStatus { get; }
        bool IsIndeterminate { get; }
        int TryCount { get; }
        int Progress { get; }
        bool IsAborted { get; }
        List<string> OperationOutput { get; }
        List<string> OperationWarning { get; }
        void Abort();
        event EventHandler Finished;
    }

    public interface IMoveOperation : IFileOperationBase
    {
        IMedia Source { get; set; }
        IMediaDirectory DestDirectory { get; set; }
    }

    public interface ICopyOperation : IFileOperationBase
    {
        IMedia Source { get; set; }
        IMediaDirectory DestDirectory { get; set; }
    }

    public interface IDeleteOperation : IFileOperationBase
    {
        IMedia Source { get; set; }
    }


    public interface IExportOperation : IFileOperationBase
    {
        IEnumerable<MediaExportDescription> Sources { get; }
        IMediaDirectory DestDirectory { get; set; }
        IMediaProperties DestProperties { get; set; }
    }

    public interface IIngestOperation : IFileOperationBase
    {
        IMedia Source { get; set; }
        IMediaDirectory DestDirectory { get; set; }
        IMediaProperties DestProperties { get; set; }
        TAspectConversion AspectConversion { get; set; }
        TAudioChannelMappingConversion AudioChannelMappingConversion { get; set; }
        TFieldOrder SourceFieldOrderEnforceConversion { get; set; }
        double AudioVolume { get; set; }
        bool Trim { get; set; }
        bool LoudnessCheck { get; set; }
        TimeSpan StartTC { get; set; }
        TimeSpan Duration { get; set; }
    }

    public interface ILoudnessOperation : IFileOperationBase
    {
        IMedia Source { get; set; }
        TimeSpan MeasureStart { get; set; }
        TimeSpan MeasureDuration { get; set; }

        event EventHandler<AudioVolumeEventArgs> AudioVolumeMeasured;
    }

}
