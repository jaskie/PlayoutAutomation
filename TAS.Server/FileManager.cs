using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TAS.Remoting.Server;
using TAS.Common.Interfaces;
using TAS.Server.Media;
using TAS.Common;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;

namespace TAS.Server
{
    public class FileManager: DtoBase, IFileManager
    {
#pragma warning disable CS0169
        [JsonProperty]
        private readonly string Dummy; // at  least one property should be serialized to resolve references
#pragma warning restore
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(FileManager));
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

        public IIngestOperation CreateIngestOperation(IIngestMedia sourceMedia, IMediaManager destMediaManager)
        {
            if (!(sourceMedia.Directory is IIngestDirectory sourceDirectory))
                return null;
            var pri = destMediaManager.MediaDirectoryPRI;
            var sec = destMediaManager.MediaDirectorySEC;
            if (!((pri != null && pri.DirectoryExists() ? pri : sec != null && sec.DirectoryExists() ? sec : null) is ServerDirectory dir))
                return null;
            
            return new IngestOperation(this)
            {
                Source = sourceMedia,
                DestDirectory = dir,
                AudioVolume = sourceDirectory.AudioVolume,
                SourceFieldOrderEnforceConversion = sourceDirectory.SourceFieldOrder,
                AspectConversion = sourceDirectory.AspectConversion,
                LoudnessCheck = sourceDirectory.MediaLoudnessCheckAfterIngest,
                StartTC = sourceMedia.TcStart,
                Duration = sourceMedia.Duration,
                MovieContainerFormat = dir.Server.MovieContainerFormat
            };
        }

        public ILoudnessOperation CreateLoudnessOperation(IMedia media, TimeSpan startTc, TimeSpan duration)
        {
            return new LoudnessOperation(this) {Source = media, MeasureStart = startTc, MeasureDuration = duration};
        }

        public IFileOperation CreateSimpleOperation() { return new FileOperation(this); }
        
        public IEnumerable<IFileOperation> GetOperationQueue()
        {
            var retList = _queueSimpleOperation.GetQueue();
            retList.AddRange(_queueConvertOperation.GetQueue());
            retList.AddRange(_queueExportOperation.GetQueue());
            return retList;
        }

        public void QueueList(IEnumerable<IFileOperation> operationList)
        {
            foreach (var operation in operationList)
                Queue(operation);
        }

        public void Queue(IFileOperation operation)
        {
            if (!(operation is FileOperation op))
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

        private void _queue(FileOperation operation)
        {
            operation.ScheduledTime = DateTime.UtcNow;
            operation.OperationStatus = FileOperationStatus.Waiting;
            Logger.Info("Operation scheduled: {0}", operation);
            OperationAdded?.Invoke(this, new FileOperationEventArgs(operation));
            if (operation.Kind == TFileOperationKind.Copy || operation.Kind == TFileOperationKind.Move || operation.Kind == TFileOperationKind.Ingest)
            {
                IMedia destMedia = operation.Dest;
                if (destMedia != null)
                    destMedia.MediaStatus = TMediaStatus.CopyPending;
            }
            switch (operation.Kind)
            {
                case TFileOperationKind.Ingest:
                    _queueConvertOperation.Enqueue(operation);
                    break;
                case TFileOperationKind.Export:
                    _queueExportOperation.Enqueue(operation);
                    break;
                case TFileOperationKind.Copy:
                case TFileOperationKind.Delete:
                case TFileOperationKind.Loudness:
                case TFileOperationKind.Move:
                    _queueSimpleOperation.Enqueue(operation);
                    break;
            }
        }

    }


}
