using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TAS.Common;

namespace TAS.Server.Interfaces
{
    public interface IFileOperation: INotifyPropertyChanged
    {
        TFileOperationKind Kind { get; set; }
        IMedia SourceMedia { get; set; }
        IMediaProperties DestMediaProperties { get; set; }
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
