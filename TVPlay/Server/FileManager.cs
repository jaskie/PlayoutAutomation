using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Messaging;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading;
using TAS.Common;
using TAS.Server.Interfaces;
using TAS.Server.Common;
using Newtonsoft.Json;

namespace TAS.Server
{
    [JsonObject(MemberSerialization.OptIn)]
    public class FileManager: IFileManager
    {
        private SynchronizedCollection<IFileOperation> _queueSimpleOperation = new SynchronizedCollection<IFileOperation>();
        private SynchronizedCollection<IFileOperation> _queueConvertOperation = new SynchronizedCollection<IFileOperation>();
        private SynchronizedCollection<IFileOperation> _queueExportOperation = new SynchronizedCollection<IFileOperation>();
        private bool _isRunningSimpleOperation = false;
        private bool _isRunningConvertOperation = false;
        private bool _isRunningExportOperation = false;

        public event EventHandler<FileOperationEventArgs> OperationAdded;
        public event EventHandler<FileOperationEventArgs> OperationCompleted;

        public IConvertOperation CreateConvertOperation() { return new ConvertOperation(); }
        public ILoudnessOperation CreateLoudnessOperation() { return new LoudnessOperation(); }
        public IFileOperation CreateFileOperation() { return new FileOperation(); }

        private readonly Guid _guidDto = Guid.NewGuid();
        [JsonProperty]
        public Guid DtoGuid { get { return _guidDto; } }
        
        public TempDirectory TempDirectory;
        public IEnumerable<IFileOperation> OperationQueue
        {
            get
            {
                IEnumerable<IFileOperation> retList;
                lock (_queueSimpleOperation.SyncRoot)
                    retList = new List<IFileOperation>(_queueSimpleOperation);
                lock (_queueConvertOperation.SyncRoot)
                    retList = retList.Concat(_queueConvertOperation);
                lock (_queueExportOperation.SyncRoot)
                    retList = retList.Concat(_queueExportOperation);
                return retList;
            }
        }

        public void Queue(FileOperation operation, bool toTop = false)
        {
            operation.Owner = this;
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
                            ThreadPool.QueueUserWorkItem(o => _runOperation(_queueConvertOperation, ref _isRunningConvertOperation));
                        }
                    }
            }
            if (operation.Kind == TFileOperationKind.Export)
            {
                lock (_queueExportOperation.SyncRoot)
                    if (!_queueExportOperation.Any(fe => fe.Equals(operation)))
                    {
                        if (toTop)
                            _queueExportOperation.Insert(0, operation);
                        else
                            _queueExportOperation.Add(operation);
                        if (!_isRunningExportOperation)
                        {
                            _isRunningExportOperation = true;
                            ThreadPool.QueueUserWorkItem(o => _runOperation(_queueExportOperation, ref _isRunningExportOperation));
                        }
                    }
            }
            if (operation.Kind == TFileOperationKind.Copy
                || operation.Kind == TFileOperationKind.Delete
                || operation.Kind == TFileOperationKind.Loudness
                || operation.Kind == TFileOperationKind.Move)
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
                            ThreadPool.QueueUserWorkItem(o => _runOperation(_queueSimpleOperation, ref _isRunningSimpleOperation));
                        }
                    }
            }
            NotifyOperation(OperationAdded, operation);
        }

        private void _runOperation(SynchronizedCollection<IFileOperation> queue, ref bool queueRunningIndicator)
        {
            FileOperation op;
            lock (queue.SyncRoot)
                op = queue.FirstOrDefault() as FileOperation;
            while (op != null)
            {
                queue.Remove(op);
                if (!op.Aborted)
                {
                    if (op.Do())
                    {
                        NotifyOperation(OperationCompleted, op);
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
                            NotifyOperation(OperationCompleted, op);
                            if (op.DestMedia != null)
                                op.DestMedia.Delete();
                        }
                    }
                }
                lock (queue.SyncRoot)
                    op = queue.FirstOrDefault() as FileOperation;
            }
            lock (queue.SyncRoot)
                queueRunningIndicator = false;
        }

        private void NotifyOperation(EventHandler<FileOperationEventArgs> handler, IFileOperation operation)
        {
            if (handler != null)
                handler(this, new FileOperationEventArgs(operation));
        }
    }


}
