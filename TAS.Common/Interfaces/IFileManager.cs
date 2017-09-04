using System;
using System.Collections.Generic;

namespace TAS.Common.Interfaces
{
    public interface IFileManager
    {
        IEnumerable<IFileOperation> GetOperationQueue();
        IIngestOperation CreateConvertOperation(IIngestMedia sourceMedia, IMediaDirectory destDirectory);
        ILoudnessOperation CreateLoudnessOperation();
        IFileOperation CreateSimpleOperation();
        event EventHandler<FileOperationEventArgs> OperationAdded;
        event EventHandler<FileOperationEventArgs> OperationCompleted;
        void QueueList(IEnumerable<IFileOperation> operationList, bool toTop);
        void Queue(IFileOperation operation, bool toTop);
        void CancelPending();
    }
}
