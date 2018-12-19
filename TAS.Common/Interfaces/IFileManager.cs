using System;
using System.Collections.Generic;
using TAS.Common.Interfaces.Media;

namespace TAS.Common.Interfaces
{
    public interface IFileManager
    {
        IEnumerable<IFileOperation> GetOperationQueue();
        IIngestOperation CreateIngestOperation(IIngestMedia sourceMedia, IMediaManager destMediaManager);
        ILoudnessOperation CreateLoudnessOperation(IMedia media, TimeSpan startTc, TimeSpan duration);
        IFileOperation CreateSimpleOperation();
        event EventHandler<FileOperationEventArgs> OperationAdded;
        event EventHandler<FileOperationEventArgs> OperationCompleted;
        void QueueList(IEnumerable<IFileOperation> operationList);
        void Queue(IFileOperation operation);
        void CancelPending();
    }
}
