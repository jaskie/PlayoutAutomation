using System;
using System.Collections.Generic;

namespace TAS.Common.Interfaces
{
    public interface IFileManager
    {
        IEnumerable<IFileOperationBase> GetOperationQueue();

        IFileOperationBase CreateFileOperation(TFileOperationKind kind);
        
        event EventHandler<FileOperationEventArgs> OperationAdded;

        event EventHandler<FileOperationEventArgs> OperationCompleted;

        void QueueList(IEnumerable<IFileOperationBase> operationList);

        void Queue(IFileOperationBase operation);

        void CancelPending();
    }
}
