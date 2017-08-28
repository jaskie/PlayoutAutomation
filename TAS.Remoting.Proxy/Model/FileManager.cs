using System;
using System.Collections.Generic;
using TAS.Remoting.Client;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class FileManager : ProxyBase, IFileManager
    {
        public IEnumerable<IFileOperation> GetOperationQueue() { return Query<List<IFileOperation>>(); }
        event EventHandler<FileOperationEventArgs> _operationAdded;
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

        event EventHandler<FileOperationEventArgs> _operationCompleted;
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

        public IConvertOperation CreateConvertOperation()
        {
            return Query<ConvertOperation>();
        }

        public IFileOperation CreateSimpleOperation()
        {
            return Query<FileOperation>();
        }

        public ILoudnessOperation CreateLoudnessOperation()
        {
            return Query<LoudnessOperation>();
        }

        protected override void OnEventNotification(WebSocketMessage e)
        {
            if (e.MemberName == nameof(OperationAdded))
            {
                _operationAdded?.Invoke(this, ConvertEventArgs<FileOperationEventArgs>(e));
            }
            if (e.MemberName == nameof(OperationCompleted))
            {
                _operationCompleted?.Invoke(this, ConvertEventArgs<FileOperationEventArgs>(e));
            }
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
    }
}
