using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Messaging;
using System.Diagnostics;
using System.ComponentModel;

namespace TAS.Server
{
    public delegate void FileOperationEventHandler(FileOperation operation);

    public static class FileManager
    {
        private static SynchronizedCollection<FileOperation> _queueSimpleOperation = new SynchronizedCollection<FileOperation>();
        private static SynchronizedCollection<FileOperation> _queueConvertOperation = new SynchronizedCollection<FileOperation>();
        private static bool _isRunningSimpleOperation = false;
        private static bool _isRunningConvertOperation = false;

        public static event FileOperationEventHandler OperationAdded;
        public static event FileOperationEventHandler OperationCompleted;

        public static TempDirectory TempDirectory = new TempDirectory();
        public static IEnumerable<FileOperation> OperationQueue
        {
            get
            {
                IEnumerable<FileOperation> retList;
                lock (_queueSimpleOperation.SyncRoot)
                    retList = new List<FileOperation>(_queueSimpleOperation);
                lock (_queueConvertOperation.SyncRoot)
                    retList = retList.Concat(_queueConvertOperation);
                return retList;
            }
        }

        public static void Queue(FileOperation operation, bool toTop = false)
        {
            if ((operation.Kind == TFileOperationKind.Copy || operation.Kind == TFileOperationKind.Move || operation.Kind == TFileOperationKind.Convert)
                && operation.DestMedia != null)
                operation.DestMedia.MediaStatus = TMediaStatus.CopyPending;
            if (operation.Kind == TFileOperationKind.Convert)
            {
                lock (_queueConvertOperation.SyncRoot)
                    if (!_queueConvertOperation.Any(fe => fe.Equals(operation)))
                    {
                        if (toTop)
                            _queueConvertOperation.Insert(0, operation);
                        else
                            _queueConvertOperation.Add(operation);
                        if (!_isRunningConvertOperation)
                        {
                            _isRunningConvertOperation = true;
                            (new Action(() => _runOperation(_queueConvertOperation, ref _isRunningConvertOperation))).BeginInvoke(_queueProcessFinishedCallback, null);
                        }
                    }
            }
            else
            {
                lock (_queueSimpleOperation.SyncRoot)
                    if (!_queueSimpleOperation.Any(fe => fe.Equals(operation)))
                    {
                        if (toTop)
                            _queueSimpleOperation.Insert(0, operation);
                        else
                            _queueSimpleOperation.Add(operation);
                        if (!_isRunningSimpleOperation)
                        {
                            _isRunningSimpleOperation = true;
                            (new Action(() => _runOperation(_queueSimpleOperation, ref _isRunningSimpleOperation))).BeginInvoke(_queueProcessFinishedCallback, null);
                        }
                    }
            }
            if (OperationAdded != null)
                OperationAdded(operation);
        }

        private static void _queueProcessFinishedCallback(IAsyncResult ar)
        {
            ((Action)((AsyncResult)ar).AsyncDelegate).EndInvoke(ar);
        }

        private static void _runOperation(SynchronizedCollection<FileOperation> queue, ref bool queueRunningIndicator)
        {
            FileOperation op;
            lock (queue.SyncRoot)
                op = queue.FirstOrDefault();
            while (op != null)
            {
                queue.Remove(op);
                if (!op.Aborted)
                {
                    if (op.Do())
                    {
                        if (OperationCompleted != null)
                            OperationCompleted(op);
                        if (op.SuccessCallback != null)
                            op.SuccessCallback();
                    }
                    else
                    {
                        if (op.TryCount > 0)
                        {
                            System.Threading.Thread.Sleep(500);
                            queue.Add(op);
                        }
                        else
                        {
                            op.Fail();
                            if (OperationCompleted != null)
                                OperationCompleted(op);
                        }
                    }
                }
                lock (queue.SyncRoot)
                    op = queue.FirstOrDefault();
            }
            lock (queue.SyncRoot)
                queueRunningIndicator = false;
        }
    }
}
