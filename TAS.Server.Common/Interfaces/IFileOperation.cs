using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TAS.Server.Common.Interfaces
{
    public interface IFileOperation: INotifyPropertyChanged
    {
        TFileOperationKind Kind { get; set; }
        IMedia Source { get; set; }
        IMediaProperties DestProperties { get; set; }
        IMediaDirectory DestDirectory { get; set; }
        DateTime ScheduledTime { get; }
        DateTime StartTime { get; }
        DateTime FinishedTime { get; }
        string Title { get; }
        FileOperationStatus OperationStatus { get; }
        bool IsIndeterminate { get; }
        int TryCount { get; }
        int Progress { get; }
        bool Aborted { get; }
        List<string> OperationOutput { get; }
        List<string> OperationWarning { get; }
        void Abort();
        event EventHandler Finished;
    }
}
