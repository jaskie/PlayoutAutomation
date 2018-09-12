using System;
using System.Collections.Generic;
using TAS.Remoting.Client;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class FileManager : ProxyBase, IFileManager
    {
        private event EventHandler<FileOperationEventArgs> _operationAdded;

        private event EventHandler<FileOperationEventArgs> _operationCompleted;

        public IEnumerable<IFileOperation> GetOperationQueue() { return Query<List<IFileOperation>>(); }
        public IIngestOperation CreateIngestOperation(IIngestMedia sourceMedia, IMediaManager destMediaManager)
        {
            return Query<IngestOperation>(parameters: new object[] {sourceMedia, destMediaManager});
        }

        public IFileOperation CreateSimpleOperation()
        {
            return Query<FileOperation>();
        }

        public ILoudnessOperation CreateLoudnessOperation()
        {
            return Query<LoudnessOperation>();
        }

        public void Queue(IFileOperation operation, bool toTop)
        {
            Invoke(parameters: new object[] { operation, toTop });
        }
      
        public void QueueList(IEnumerable<IFileOperation> operationList, bool toTop)
        {
            Invoke(parameters: new object[] { operationList, toTop });
        }

        public void CancelPending()
        {
            Invoke();
        }

        public event EventHandler<FileOperationEventArgs> OperationAdded
        {
            add
            {
                EventAdd(_operationAdded);
                _operationAdded += value;
            }
            remove
            {
                _operationAdded -= value;
                EventRemove(_operationAdded);
            }
        }

        public event EventHandler<FileOperationEventArgs> OperationCompleted
        {
            add
            {
                EventAdd(_operationCompleted);
                _operationCompleted += value;
            }
            remove
            {
                _operationCompleted -= value;
                EventRemove(_operationCompleted);
            }
        }

        protected override void OnEventNotification(SocketMessage message)
        {
            if (message.MemberName == nameof(OperationAdded))
            {
                _operationAdded?.Invoke(this, Deserialize<FileOperationEventArgs>(message));
            }
            if (message.MemberName == nameof(OperationCompleted))
            {
                _operationCompleted?.Invoke(this, Deserialize<FileOperationEventArgs>(message));
            }
        }

    }
}
