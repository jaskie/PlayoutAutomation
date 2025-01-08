using System;
using System.Collections.Generic;
using jNet.RPC.Client;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Remoting.Model.MediaOperation;

namespace TAS.Remoting.Model
{
    public class FileManager : ProxyObjectBase, IFileManager
    {

        private event EventHandler<FileOperationEventArgs> _operationAdded;

        private event EventHandler<FileOperationEventArgs> _operationCompleted;

        public IEnumerable<IFileOperationBase> GetOperationQueue() { return Query<FileOperationBase[]>(); }

        public IFileOperationBase CreateFileOperation(TFileOperationKind kind)
        {
            return Query<IFileOperationBase>(parameters: new object[] { kind });
        }
        
        public void Queue(IFileOperationBase operation)
        {
            Invoke(parameters: operation);
        }
      
        public void QueueList(IEnumerable<IFileOperationBase> operationList)
        {
            Invoke(parameters: operationList);
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

        protected override void OnEventNotification(string eventName, EventArgs eventArgs) 
        {
            switch (eventName)
            {
                case nameof(OperationAdded):
                    _operationAdded?.Invoke(this, (FileOperationEventArgs)eventArgs);
                    return;
                case nameof(OperationCompleted):
                    _operationCompleted?.Invoke(this, (FileOperationEventArgs)eventArgs);
                    return;
            }
            base.OnEventNotification(eventName, eventArgs);
        }

    }
}
