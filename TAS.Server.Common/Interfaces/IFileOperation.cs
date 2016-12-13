using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Remoting;

namespace TAS.Server.Interfaces
{
    public interface IFileOperation: INotifyPropertyChanged, IDto
    {
        TFileOperationKind Kind { get; set; }
        IMedia SourceMedia { get; set; }
        IMediaProperties DestMediaProperties { get; set; }
        IMedia DestMedia { get; }
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
