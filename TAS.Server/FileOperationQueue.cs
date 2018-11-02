using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NLog;
using TAS.Common;

namespace TAS.Server
{
    public class FileOperationQueue
    {
        private readonly Queue<FileOperation> _queue = new Queue<FileOperation>();

        private readonly object _queueLock = new object();

        private FileOperation _currentOperation;

        public void Enqueue(FileOperation operation)
        {
            lock (_queueLock)
            {
                _queue.Enqueue(operation);
            }
            RunOperation();
        }

        public List<FileOperation> GetQueue()
        {
            lock (_queueLock)
            {
                var result = _queue.ToList();
                if (_currentOperation != null)
                    result.Add(_currentOperation);
                return result;
            }
        }

        public void CancelPending()
        {
            lock (_queueLock)
                _queue.Where(o => o.OperationStatus == FileOperationStatus.Waiting).ToList().ForEach(o => o.Abort());
        }

        public event EventHandler<FileOperationEventArgs> OperationCompleted;

        private async void RunOperation()
        {
            while (true)
            {
                FileOperation operation;
                lock (_queueLock)
                {
                    if (_currentOperation != null)
                        return;
                    if (_queue.Count == 0)
                        return;
                    _currentOperation = _queue.Dequeue();
                    operation = _currentOperation;
                }
                if (operation.IsAborted)
                    continue;
                var success = await operation.Execute();
                lock (_queueLock)
                {
                    _currentOperation = null;
                    if (!operation.IsAborted)
                        if (!success)
                        {
                            if (operation.TryCount > 0)
                                _queue.Enqueue(operation);
                            else
                                operation.OperationStatus = FileOperationStatus.Failed;
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
