using System;
using System.Collections.Generic;
using jNet.RPC.Server;
using Newtonsoft.Json;
using TAS.Common.Interfaces;
using TAS.Common;
using TAS.Server.MediaOperation;

namespace TAS.Server
{
    public class FileManager: DtoBase, IFileManager
    {
#pragma warning disable CS0169
        [JsonProperty]
        public readonly string Dummy = string.Empty; // at  least one property must be serialized to resolve references
#pragma warning restore
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly FileOperationQueue _queueSimpleOperation = new FileOperationQueue();
        private readonly FileOperationQueue _queueConvertOperation = new FileOperationQueue();
        private readonly FileOperationQueue _queueExportOperation = new FileOperationQueue();

        public static FileManager Current { get; } = new FileManager();

        private FileManager()
        {
            _queueSimpleOperation.OperationCompleted += _queue_OperationCompleted;
            _queueConvertOperation.OperationCompleted += _queue_OperationCompleted;
            _queueExportOperation.OperationCompleted += _queue_OperationCompleted;
        }

        protected override void DoDispose()
        {
            _queueSimpleOperation.OperationCompleted -= _queue_OperationCompleted;
            _queueConvertOperation.OperationCompleted -= _queue_OperationCompleted;
            _queueExportOperation.OperationCompleted -= _queue_OperationCompleted;
            base.DoDispose();
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
            return retList;
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

    }


}
