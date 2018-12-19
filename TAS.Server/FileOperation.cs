using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TAS.Remoting.Server;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;
using TAS.Common.Interfaces.MediaDirectory;
using TAS.Server.Media;

namespace TAS.Server
{
    public class FileOperation : DtoBase, IFileOperation
    {
        [JsonProperty]
        public TFileOperationKind Kind { get; set; }

        private readonly object _destMediaLock = new object();

        private IMedia _sourceMedia;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(FileOperation));
        private IMediaProperties _destMediaProperties;
        private int _tryCount = 15;
        private DateTime _scheduledTime;
        private DateTime _startTime;
        private DateTime _finishedTime;
        private FileOperationStatus _operationStatus;
        private int _progress;
        private bool _isIndeterminate;
        private readonly List<string> _operationOutput = new List<string>();
        private readonly List<string> _operationWarning = new List<string>();

        protected readonly FileManager OwnerFileManager;
        protected readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        private bool _isAborted;

        internal FileOperation(FileManager ownerFileManager)
        {
            OwnerFileManager = ownerFileManager;
        }
       
        [JsonProperty]
        public IMediaProperties DestProperties { get => _destMediaProperties; set => SetField(ref _destMediaProperties, value, nameof(Title)); }

        [JsonProperty]
        public IMediaDirectory DestDirectory { get; set; }

        [JsonProperty]
        public IMedia Source { get => _sourceMedia; set => SetField(ref _sourceMedia, value); }

        internal MediaBase Dest { get; set; }

        [JsonProperty]
        public int TryCount
        {
            get => _tryCount;
            set => SetField(ref _tryCount, value);
        }
        
        [JsonProperty]
        public int Progress
        {
            get => _progress;
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
            get => _scheduledTime;
            internal set
            {
                if (SetField(ref _scheduledTime, value))
                    AddOutputMessage("Operation scheduled");
            }
        }

        [JsonProperty]
        public DateTime StartTime
        {
            get => _startTime;
            protected set => SetField(ref _startTime, value);
        }

        [JsonProperty]
        public DateTime FinishedTime 
        {
            get => _finishedTime;
            protected set => SetField(ref _finishedTime, value);
        }

        [JsonProperty]
        public FileOperationStatus OperationStatus
        {
            get => _operationStatus;
            set
            {
                if (!SetField(ref _operationStatus, value))
                    return;
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
                if (_sourceMedia is IngestMedia im)
                    im.IngestStatus = newIngestStatus;
                if (_sourceMedia is ArchiveMedia am)
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

        [JsonProperty]
        public bool IsIndeterminate
        {
            get => _isIndeterminate;
            set => SetField(ref _isIndeterminate, value);
        }


        [JsonProperty]
        public bool IsAborted
        {
            get => _isAborted;
            private set => SetField(ref _isAborted, value);
        }

        [JsonProperty]
        public virtual string Title => DestDirectory == null 
            ? $"{Kind} {Source}"
            : $"{Kind} {Source} -> {DestDirectory.DirectoryName}";

        [JsonProperty]
        public List<string> OperationWarning
        {
            get
            {
                lock (((IList) _operationWarning).SyncRoot)
                {
                    return _operationWarning.ToList();
                }
            }
        }

        [JsonProperty]
        public List<string> OperationOutput
        {
            get
            {
                lock (((IList)_operationOutput).SyncRoot)
                {
                    return _operationOutput.ToList();
                }
            }
        }

        public virtual void Abort()
        {
            if (IsAborted)
                return;
            IsAborted = true;
            CancellationTokenSource.Cancel();
            lock (_destMediaLock)
            {
                if (Dest != null && Dest.FileExists())
                    Dest.Delete();
            }
            IsIndeterminate = false;
            OperationStatus = FileOperationStatus.Aborted;
        }

        public event EventHandler Success;
        public event EventHandler Failure;
        public event EventHandler Finished;


        // utility methods
        internal async Task<bool> Execute()
        {
            try
            {
                AddOutputMessage("Operation started");
                if (await InternalExecute())
                {
                    OperationStatus = FileOperationStatus.Finished;
                    AddOutputMessage("Operation completed successfully.");
                    return true;
                }
            }
            catch (Exception e)
            {
                AddOutputMessage(e.Message);
            }
            TryCount--;
            return false;
        }

        internal void Fail()
        {
            OperationStatus = FileOperationStatus.Failed;
            lock (_destMediaLock)
            {
                if (Dest != null && Dest.FileExists())
                    Dest.Delete();
            }
            Logger.Info($"Operation failed: {Title}");
        }

        protected void AddOutputMessage(string message)
        {
            lock (((IList)_operationOutput).SyncRoot)
                _operationOutput.Add($"{DateTime.UtcNow} {message}");
            NotifyPropertyChanged(nameof(OperationOutput));
            Logger.Info("{0}: {1}", Title, message);
        }

        protected void AddWarningMessage(string message)
        {
            lock (((IList)_operationWarning).SyncRoot)
                _operationWarning.Add(message);
            NotifyPropertyChanged(nameof(OperationWarning));
        }

        protected virtual void CreateDestMediaIfNotExists()
        {
            lock (_destMediaLock)
            {
                if (Dest != null)
                    return;
                if (!(DestDirectory is MediaDirectoryBase mediaDirectory))
                    throw new ApplicationException($"Cannot create destination media on {DestDirectory}");
                Dest = (MediaBase) mediaDirectory.CreateMedia(DestProperties ?? Source);
            }
        }
        
        protected virtual async Task<bool> InternalExecute()
        {
            StartTime = DateTime.UtcNow;
            OperationStatus = FileOperationStatus.InProgress;
            if (!(Source is MediaBase source))
                return false;
            switch (Kind)
            {
                case TFileOperationKind.Copy:
                    if (!File.Exists(source.FullPath) || !Directory.Exists(DestDirectory.Folder))
                        return false;
                    CreateDestMediaIfNotExists();
                    if (!(Dest.FileExists()
                          && File.GetLastWriteTimeUtc(source.FullPath)
                              .Equals(File.GetLastWriteTimeUtc(Dest.FullPath))
                          && File.GetCreationTimeUtc(source.FullPath).Equals(File.GetCreationTimeUtc(Dest.FullPath))
                          && Source.FileSize.Equals(Dest.FileSize)))
                    {
                        Dest.MediaStatus = TMediaStatus.Copying;
                        IsIndeterminate = true;
                        if (!await source.CopyMediaTo(Dest, CancellationTokenSource.Token))
                            return false;
                    }
                    Dest.MediaStatus = TMediaStatus.Copied;
                    await Task.Run(() => Dest.Verify());
                    ((MediaDirectoryBase) DestDirectory).RefreshVolumeInfo();
                    return true;
                case TFileOperationKind.Delete:
                    if (!Source.Delete()) return false;
                    ((MediaDirectoryBase) Source.Directory).RefreshVolumeInfo();
                    return true;
                case TFileOperationKind.Move:
                    if (!File.Exists(source.FullPath) || !Directory.Exists(DestDirectory.Folder))
                        return false;
                    CreateDestMediaIfNotExists();
                    if (Dest.FileExists())
                        if (File.GetLastWriteTimeUtc(source.FullPath).Equals(File.GetLastWriteTimeUtc(Dest.FullPath))
                            && File.GetCreationTimeUtc(source.FullPath).Equals(File.GetCreationTimeUtc(Dest.FullPath))
                            && source.FileSize.Equals(Dest.FileSize))
                        {
                            source.Delete();
                            ((MediaDirectoryBase) Source.Directory).RefreshVolumeInfo();
                            return true;
                        }
                        else if (!Dest.Delete())
                        {
                            AddOutputMessage("Move operation failed - destination media not deleted");
                            return false;
                        }
                    IsIndeterminate = true;
                    Dest.MediaStatus = TMediaStatus.Copying;
                    FileUtils.CreateDirectoryIfNotExists(Path.GetDirectoryName(Dest.FullPath));
                    File.Move(source.FullPath, Dest.FullPath);
                    File.SetCreationTimeUtc(Dest.FullPath, File.GetCreationTimeUtc(source.FullPath));
                    File.SetLastWriteTimeUtc(Dest.FullPath, File.GetLastWriteTimeUtc(source.FullPath));
                    Dest.MediaStatus = TMediaStatus.Copied;
                    await Task.Run(() => Dest.Verify());
                    ((MediaDirectoryBase) Source.Directory).RefreshVolumeInfo();
                    ((MediaDirectoryBase) DestDirectory).RefreshVolumeInfo();
                    return true;
                default:
                    throw new InvalidOperationException("Invalid operation kind");
            }
        }
        
    }
}
