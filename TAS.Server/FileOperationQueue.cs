using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TAS.Common;
using TAS.Server.MediaOperation;

namespace TAS.Server
{
    public class FileOperationQueue
    {
        private readonly Queue<FileOperationBase> _queue = new Queue<FileOperationBase>();
        
        private FileOperationBase _currentOperation;

        public void Enqueue(FileOperationBase operation)
        {
            lock (((ICollection)_queue).SyncRoot)
            {
                _queue.Enqueue(operation);
            }
            RunOperation();
        }

        public List<FileOperationBase> GetQueue()
        {
            lock (((ICollection)_queue).SyncRoot)
            {
                var result = _queue.ToList();
                if (_currentOperation != null)
                    result.Add(_currentOperation);
                return result;
            }
        }

        public void CancelPending()
        {
            lock (((ICollection)_queue).SyncRoot)
                _queue.Where(o => o.OperationStatus == FileOperationStatus.Waiting).ToList().ForEach(o => o.Abort());
        }

        public event EventHandler<FileOperationEventArgs> OperationCompleted;

        private async void RunOperation()
        {
            while (true)
            {
                FileOperationBase operation;
                lock (((ICollection)_queue).SyncRoot)
                {
                    if (_currentOperation != null)
                        return;
                    if (_queue.Count == 0)
                        return;
                    operation = _queue.Dequeue();
                    if (operation.IsAborted)
                        continue;
                    _currentOperation = operation;
                }
                var success = await operation.Execute();
                lock (((ICollection)_queue).SyncRoot)
                {
                    _currentOperation = null;
                    if (!operation.IsAborted)
                        if (!success)
                        {
                            if (operation.TryCount > 0)
                                _queue.Enqueue(operation);
                        }
                }
                if (!success)
                {
                    Thread.Sleep(500);
                    continue;
                }
                OperationCompleted?.Invoke(this, new FileOperationEventArgs(operation));
                operation.Dispose();
            }
        }

    }
}
