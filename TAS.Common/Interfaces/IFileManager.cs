using System;
using System.Collections.Generic;
using TAS.Common.Interfaces.Media;

namespace TAS.Common.Interfaces
{
    public interface IFileManager
    {
        IEnumerable<IFileOperation> GetOperationQueue();
        IIngestOperation CreateIngestOperation(IIngestMedia sourceMedia, IMediaManager destMediaManager);
        ILoudnessOperation CreateLoudnessOperation();
        IFileOperation CreateSimpleOperation();
        event EventHandler<FileOperationEventArgs> OperationAdded;
        event EventHandler<FileOperationEventArgs> OperationCompleted;
        void QueueList(IEnumerable<IFileOperation> operationList, bool toTop);
        void Queue(IFileOperation operation, bool toTop);
        void CancelPending();
    }
}
