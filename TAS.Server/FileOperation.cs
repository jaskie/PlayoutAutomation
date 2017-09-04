using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Threading;
using Newtonsoft.Json;
using TAS.Remoting.Server;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Server.Media;

namespace TAS.Server
{
    public class FileOperation : DtoBase, IFileOperation
    {
        [JsonProperty]
        public TFileOperationKind Kind { get; set; }

        private readonly object _destMediaLock = new object();

        protected MediaBase DestMedia;
        protected MediaBase SourceMedia;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(FileOperation));
        private IMediaProperties _destMediaProperties;
        private int _tryCount = 15;
        private DateTime _scheduledTime;
        private DateTime _startTime;
        private DateTime _finishedTime;
        private FileOperationStatus _operationStatus;
        private int _progress;
        private bool _isIndeterminate;
        private readonly SynchronizedCollection<string> _operationOutput = new SynchronizedCollection<string>();
        private readonly SynchronizedCollection<string> _operationWarning = new SynchronizedCollection<string>();

        protected readonly FileManager OwnerFileManager;
        protected bool Aborted;

        internal FileOperation(FileManager ownerFileManager)
        {
            OwnerFileManager = ownerFileManager;
        }

#if DEBUG
        ~FileOperation()
        {
            Debug.WriteLine("{0} finalized: {1}", GetType(), this);
        }
#endif
        
        [JsonProperty]
        public IMediaProperties DestProperties { get { return _destMediaProperties; } set { SetField(ref _destMediaProperties, value, nameof(Title)); } }

        [JsonProperty]
        public IMediaDirectory DestDirectory { get; set; }

        [JsonProperty]
        public IMedia Source { get { return SourceMedia; } set { SetField(ref SourceMedia, value as MediaBase); } }

        public IMedia Dest { get { return DestMedia; }  protected set { SetField(ref DestMedia, value as MediaBase); } }

        [JsonProperty]
        public int TryCount
        {
            get { return _tryCount; }
            set { SetField(ref _tryCount, value); }
        }
        
        [JsonProperty]
        public int Progress
        {
            get { return _progress; }
            set
            {
                if (value > 0 && value <= 100)
                    SetField(ref _progress, value);
                IsIndeterminate = false;
            }
        }

        [JsonProperty]
        public DateTime ScheduledTime
        {
            get { return _scheduledTime; }
            internal set
            {
                if (SetField(ref _scheduledTime, value))
                    AddOutputMessage("Operation scheduled");
            }
        }

        [JsonProperty]
        public DateTime StartTime
        {
            get { return _startTime; }
            protected set { SetField(ref _startTime, value); }
        }

        [JsonProperty]
        public DateTime FinishedTime 
        {
            get { return _finishedTime; }
            protected set { SetField(ref _finishedTime, value); }
        }

        [JsonProperty]
        public FileOperationStatus OperationStatus
        {
            get { return _operationStatus; }
            set
            {
                if (SetField(ref _operationStatus, value))
                {
                    TIngestStatus newIngestStatus;
                        switch (value)
                        {
                            case FileOperationStatus.Finished:
                                newIngestStatus = TIngestStatus.Ready;
                                break;
                            case FileOperationStatus.Waiting:
                            case FileOperationStatus.InProgress:
                                newIngestStatus = TIngestStatus.InProgress;
                                break;
                            default:
                                newIngestStatus = TIngestStatus.Unknown;
                                break;
                        }
                    var im = SourceMedia as IngestMedia;
                    if (im != null)
                        im.IngestStatus = newIngestStatus;
                    var am = SourceMedia as ArchiveMedia;
                    if (am != null)
                        am.IngestStatus = newIngestStatus;

                    EventHandler h;
                    if (value == FileOperationStatus.Finished)
                    {
                        Progress = 100;
                        FinishedTime = DateTime.UtcNow;
                        h = Success;
                        h?.Invoke(this, EventArgs.Empty);
                        h = Finished;
                        h?.Invoke(this, EventArgs.Empty);
                    }
                    if (value == FileOperationStatus.Failed)
                    {
                        Progress = 0;
                        h = Failure;
                        h?.Invoke(this, EventArgs.Empty);
                        h = Finished;
                        h?.Invoke(this, EventArgs.Empty);
                    }
                    if (value == FileOperationStatus.Aborted)
                    {
                        IsIndeterminate = false;
                        h = Failure;
                        h?.Invoke(this, EventArgs.Empty);
                        h = Finished;
                        h?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        [JsonProperty]
        public bool IsIndeterminate
        {
            get { return _isIndeterminate; }
            set { SetField(ref _isIndeterminate, value); }
        }


        [JsonProperty]
        public bool IsAborted
        {
            get { return Aborted; }
            private set
            {
                if (SetField(ref Aborted, value))
                {
                    lock (_destMediaLock)
                    {
                        if (DestMedia != null && DestMedia.FileExists())
                            DestMedia.Delete();
                    }
                    IsIndeterminate = false;
                    OperationStatus = FileOperationStatus.Aborted;
                }
            }
        }

        [JsonProperty]
        public virtual string Title => DestDirectory == null ?
            string.Format("{0} {1}", Kind, Source)
            :
            string.Format("{0} {1} -> {2}", Kind, Source, DestDirectory.DirectoryName);

        [JsonProperty]
        public List<string> OperationWarning { get { lock (_operationWarning.SyncRoot) return _operationWarning.ToList(); } }

        [JsonProperty]
        public List<string> OperationOutput { get { lock (_operationOutput.SyncRoot) return _operationOutput.ToList(); } }

        public virtual void Abort()
        {
            IsAborted = true;
        }

        public event EventHandler Success;
        public event EventHandler Failure;
        public event EventHandler Finished;


        // utility methods
        internal virtual bool Execute()
        {
            if (InternalExecute())
            {
                OperationStatus = FileOperationStatus.Finished;
            }
            else
                TryCount--;
            return OperationStatus == FileOperationStatus.Finished;
        }

        internal void Fail()
        {
            OperationStatus = FileOperationStatus.Failed;
            lock (_destMediaLock)
            {
                if (DestMedia != null && DestMedia.FileExists())
                    DestMedia.Delete();
            }
            Logger.Info($"Operation failed: {Title}");
        }

        protected void AddOutputMessage(string message)
        {
            _operationOutput.Add(string.Format("{0} {1}", DateTime.Now, message));
            NotifyPropertyChanged(nameof(OperationOutput));
            Logger.Info("{0}: {1}", Title, message);
        }

        protected void AddWarningMessage(string message)
        {
            _operationWarning.Add(message);
            NotifyPropertyChanged(nameof(OperationWarning));
        }

        protected void CreateDestMediaIfNotExists()
        {
            lock (_destMediaLock)
            {
                if (DestMedia == null)
                    Dest = DestDirectory.CreateMedia(DestProperties != null? DestProperties: Source);
            }
        }
        
        private bool InternalExecute()
        {
            AddOutputMessage($"Operation {Title} started");
            StartTime = DateTime.UtcNow;
            OperationStatus = FileOperationStatus.InProgress;
            switch (Kind)
            {
                case TFileOperationKind.None:
                    return true;
                case TFileOperationKind.Convert:
                case TFileOperationKind.Export:
                    throw new InvalidOperationException("Invalid operation kind");
                case TFileOperationKind.Copy:
                    if (SourceMedia != null && File.Exists(SourceMedia.FullPath) && Directory.Exists(DestDirectory.Folder))
                        try
                        {
                            CreateDestMediaIfNotExists();
                            if (!(DestMedia.FileExists()
                                && File.GetLastWriteTimeUtc(SourceMedia.FullPath).Equals(File.GetLastWriteTimeUtc(DestMedia.FullPath))
                                && File.GetCreationTimeUtc(SourceMedia.FullPath).Equals(File.GetCreationTimeUtc(DestMedia.FullPath))
                                && Source.FileSize.Equals(DestMedia.FileSize)))
                            {
                                DestMedia.MediaStatus = TMediaStatus.Copying;
                                IsIndeterminate = true;
                                if (!SourceMedia.CopyMediaTo(DestMedia, ref Aborted))
                                    return false;
                            }
                            DestMedia.MediaStatus = TMediaStatus.Copied;
                            ThreadPool.QueueUserWorkItem(o => DestMedia.Verify());
                            AddOutputMessage($"Copy operation {Title} finished");
                            return true;
                        }
                        catch (Exception e)
                        {
                            AddOutputMessage($"Copy operation {Title} failed with {e.Message}");
                        }
                    return false;
                case TFileOperationKind.Delete:
                    try
                    {
                        if (Source.Delete())
                        {
                            AddOutputMessage($"Delete operation {Title} finished"); 
                            return true;
                        }
                    }
                    catch (Exception e)
                    {
                        AddOutputMessage($"Delete operation {Title} failed with {e.Message}");
                    }
                    return false;
                case TFileOperationKind.Move:
                    if (File.Exists(SourceMedia.FullPath) && Directory.Exists(DestDirectory.Folder))
                        try
                        {
                            CreateDestMediaIfNotExists();
                            if (DestMedia.FileExists())
                            {
                                if (File.GetLastWriteTimeUtc(SourceMedia.FullPath).Equals(File.GetLastWriteTimeUtc(DestMedia.FullPath))
                                && File.GetCreationTimeUtc(SourceMedia.FullPath).Equals(File.GetCreationTimeUtc(DestMedia.FullPath))
                                && SourceMedia.FileSize.Equals(DestMedia.FileSize))
                                {
                                    SourceMedia.Delete();
                                    return true;
                                }
                                else
                                if (!DestMedia.Delete())
                                {
                                    AddOutputMessage("Move operation failed - destination media not deleted");
                                    return false;
                                }
                            }
                            IsIndeterminate = true;
                            DestMedia.MediaStatus = TMediaStatus.Copying;
                            FileUtils.CreateDirectoryIfNotExists(Path.GetDirectoryName(DestMedia.FullPath));
                            File.Move(SourceMedia.FullPath, DestMedia.FullPath);
                            File.SetCreationTimeUtc(DestMedia.FullPath, File.GetCreationTimeUtc(SourceMedia.FullPath));
                            File.SetLastWriteTimeUtc(DestMedia.FullPath, File.GetLastWriteTimeUtc(SourceMedia.FullPath));
                            DestMedia.MediaStatus = TMediaStatus.Copied;
                            ThreadPool.QueueUserWorkItem(o => DestMedia.Verify());
                            AddOutputMessage("Move operation finished");
                            Debug.WriteLine(this, "File operation succeed");
                            return true;
                        }
                        catch (Exception e)
                        {
                            AddOutputMessage($"Move operation {Title} failed with {e.Message}");
                        }
                    return false;
                default:
                    return false;
            }
        }
        
    }
}
