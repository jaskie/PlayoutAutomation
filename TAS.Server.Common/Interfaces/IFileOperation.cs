using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TAS.Common;

namespace TAS.Server.Interfaces
{
    public interface IFileOperation: INotifyPropertyChanged, IDto
    {
        TFileOperationKind Kind { get; set; }
        IMedia SourceMedia { get; set; }
        IMedia DestMedia { get; set; }
        DateTime ScheduledTime { get; }
        DateTime StartTime { get; }
        DateTime FinishedTime { get; }
        FileOperationStatus OperationStatus { get; }
        bool IsIndeterminate { get; }
        int TryCount { get; }
        int Progress { get; }
        bool Aborted { get; set; }
        List<string> OperationOutput { get; }
        List<string> OperationWarning { get; }
        void Fail();
    }
}
