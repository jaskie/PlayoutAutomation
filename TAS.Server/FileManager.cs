using System;
using System.Collections.Generic;
using ComponentModelRPC.Server;
using Newtonsoft.Json;
using TAS.Common.Interfaces;
using TAS.Server.Media;
using TAS.Common;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Server.MediaOperation;

namespace TAS.Server
{
    public class FileManager: DtoBase, IFileManager
    {
#pragma warning disable CS0169
        [JsonProperty]
        private readonly string Dummy; // at  least one property should be serialized to resolve references
#pragma warning restore
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly FileOperationQueue _queueSimpleOperation = new FileOperationQueue();
        private readonly FileOperationQueue _queueConvertOperation = new FileOperationQueue();
        private readonly FileOperationQueue _queueExportOperation = new FileOperationQueue();
        internal readonly TempDirectory TempDirectory;
        internal double ReferenceLoudness;

        internal FileManager(TempDirectory tempDirectory)
        {
            TempDirectory = tempDirectory;
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
                    return new CopyOperation(this);
                case TFileOperationKind.Delete:
                    return new DeleteOperation(this);
                case TFileOperationKind.Export:
                    return new ExportOperation(this);
                case TFileOperationKind.Ingest:
                    return new IngestOperation(this);
                case TFileOperationKind.Loudness:
                    return new LoudnessOperation(this);
                case TFileOperationKind.Move:
                    return new MoveOperation(this);
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
