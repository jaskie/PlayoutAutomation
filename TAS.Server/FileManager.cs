using System;
using System.Collections.Generic;
using jNet.RPC.Server;
using TAS.Common.Interfaces;
using TAS.Common;
using TAS.Server.MediaOperation;
using jNet.RPC;

namespace TAS.Server
{
    public class FileManager: ServerObjectBase, IFileManager, IDisposable
    {
#pragma warning disable CS0169
        [DtoMember]
        public readonly string Dummy = string.Empty; // at  least one property must be serialized to resolve references
#pragma warning restore
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly FileOperationQueue _queueSimpleOperation = new FileOperationQueue();
        private readonly FileOperationQueue _queueConvertOperation = new FileOperationQueue();
        private readonly FileOperationQueue _queueExportOperation = new FileOperationQueue();
        private bool _isDisposed = false;

        public static FileManager Current { get; } = new FileManager();

        private FileManager()
        {
            _queueSimpleOperation.OperationCompleted += _queue_OperationCompleted;
            _queueConvertOperation.OperationCompleted += _queue_OperationCompleted;
            _queueExportOperation.OperationCompleted += _queue_OperationCompleted;
        }

        private void _queue_OperationCompleted(object sender, FileOperationEventArgs e)
        {
            OperationCompleted?.Invoke(this, e);
        }

        public event EventHandler<FileOperationEventArgs> OperationAdded;
        public event EventHandler<FileOperationEventArgs> OperationCompleted;


        public IFileOperationBase CreateFileOperation(TFileOperationKind kind)
        {
            switch (kind)
            {
                case TFileOperationKind.Copy:
                    return new CopyOperation();
                case TFileOperationKind.Delete:
                    return new DeleteOperation();
                case TFileOperationKind.Export:
                    return new ExportOperation();
                case TFileOperationKind.Ingest:
                    return new IngestOperation();
                case TFileOperationKind.Loudness:
                    return new LoudnessOperation();
                case TFileOperationKind.Move:
                    return new MoveOperation();
                default:
                    throw new ArgumentException(nameof(kind));
            }
        }

        public IEnumerable<IFileOperationBase> GetOperationQueue()
        {
            var retList = _queueSimpleOperation.GetQueue();
            retList.AddRange(_queueConvertOperation.GetQueue());
            retList.AddRange(_queueExportOperation.GetQueue());
            return retList.ToArray();
        }

        public void QueueList(IEnumerable<IFileOperationBase> operationList)
        {
            foreach (var operation in operationList)
                Queue(operation);
        }

        public void Queue(IFileOperationBase operation)
        {
            if (!(operation is FileOperationBase op))
                return;
            _queue(op);
        }

        public void CancelPending()
        {
            _queueSimpleOperation.CancelPending();
            _queueConvertOperation.CancelPending();
            _queueExportOperation.CancelPending();
            Logger.Trace("Cancelled pending operations");
        }

        private void _queue(FileOperationBase operation)
        {
            operation.ScheduledTime = DateTime.UtcNow;
            operation.OperationStatus = FileOperationStatus.Waiting;
            Logger.Info("Operation scheduled: {0}", operation);
            switch (operation)
            {
                case IngestOperation _:
                    _queueConvertOperation.Enqueue(operation);
                    break;
                case ExportOperation _:
                    _queueExportOperation.Enqueue(operation);
                    break;
                case CopyOperation _:
                case DeleteOperation _:
                case LoudnessOperation _:
                case MoveOperation _:
                    _queueSimpleOperation.Enqueue(operation);
                    break;
            }
            OperationAdded?.Invoke(this, new FileOperationEventArgs(operation));
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;
            _queueSimpleOperation.OperationCompleted -= _queue_OperationCompleted;
            _queueConvertOperation.OperationCompleted -= _queue_OperationCompleted;
            _queueExportOperation.OperationCompleted -= _queue_OperationCompleted;
            Logger.Debug("FileManager disposed");
        }
    }


}
