using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Text.RegularExpressions;
using TAS.Common;
using System.Threading;
using TAS.Server.Interfaces;
using Newtonsoft.Json;
using TAS.Remoting.Server;

namespace TAS.Server
{
    public class FileOperation : DtoBase, IFileOperation
    {
        [JsonProperty]
        public TFileOperationKind Kind { get; set; }

        private object _destMediaLock = new object();
        private IMedia _destMedia;
        public event EventHandler Success;
        public event EventHandler Failure;
        public event EventHandler Finished;
        internal FileManager Owner;
        public FileOperation()
        {
            Logger = NLog.LogManager.GetLogger(this.GetType().Name);
        }

#if DEBUG
        ~FileOperation()
        {
            Debug.WriteLine("{0} finalized: {1}", GetType(), this);
        }
#endif

        private NLog.Logger Logger;


        public IMedia SourceMedia { get; set; }
        protected IMediaProperties _destMediaProperties;
        public IMediaProperties DestMediaProperties { get { return _destMediaProperties; } set { SetField(ref _destMediaProperties, value, nameof(Title)); } }
        public IMediaDirectory DestDirectory { get; set; }
        public IMedia DestMedia { get { return _destMedia; }  protected set { SetField(ref _destMedia, value, nameof(DestMedia)); } }

        private int _tryCount = 15;
        [JsonProperty]
        public int TryCount
        {
            get { return _tryCount; }
            set { SetField(ref _tryCount, value, nameof(TryCount)); }
        }
        
        private int _progress;
        [JsonProperty]
        public int Progress
        {
            get { return _progress; }
            set
            {
                if (value > 0 && value <= 100)
                    SetField(ref _progress, value, nameof(Progress));
                IsIndeterminate = false;
            }
        }

        private DateTime _scheduledTime;
        [JsonProperty]
        public DateTime ScheduledTime
        {
            get { return _scheduledTime; }
            internal set
            {
                if (SetField(ref _scheduledTime, value, nameof(ScheduledTime)))
                    AddOutputMessage("Operation scheduled");
            }
        }
        private DateTime _startTime;
        [JsonProperty]
        public DateTime StartTime
        {
            get { return _startTime; }
            protected set { SetField(ref _startTime, value, nameof(StartTime)); }
        }
        private DateTime _finishedTime;
        [JsonProperty]
        public DateTime FinishedTime 
        {
            get { return _finishedTime; }
            protected set { SetField(ref _finishedTime, value, nameof(FinishedTime)); }
        }

        private FileOperationStatus _operationStatus;
        [JsonProperty]
        public FileOperationStatus OperationStatus
        {
            get { return _operationStatus; }
            set
            {
                if (SetField(ref _operationStatus, value, nameof(OperationStatus)))
                {
                    IngestMedia im = SourceMedia as IngestMedia;
                    if (im != null)
                        switch (value)
                        {
                            case FileOperationStatus.Finished:
                                im.IngestStatus = TIngestStatus.Ready;
                                break;
                            case FileOperationStatus.Waiting:
                            case FileOperationStatus.InProgress:
                                im.IngestStatus = TIngestStatus.InProgress;
                                break;
                            default:
                                im.IngestStatus = TIngestStatus.Unknown;
                                break;
                        }
                    ArchiveMedia am = SourceMedia as ArchiveMedia;
                    if (am != null)
                        switch (value)
                        {
                            case FileOperationStatus.Finished:
                                am.IngestStatus = TIngestStatus.Ready;
                                break;
                            case FileOperationStatus.Waiting:
                            case FileOperationStatus.InProgress:
                                am.IngestStatus = TIngestStatus.InProgress;
                                break;
                            default:
                                am.IngestStatus = TIngestStatus.Unknown;
                                break;
                        }

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

        private bool _isIndeterminate;
        [JsonProperty]
        public bool IsIndeterminate
        {
            get { return _isIndeterminate; }
            set { SetField(ref _isIndeterminate, value, nameof(IsIndeterminate)); }
        }


        protected bool _aborted;
        [JsonProperty]
        public bool Aborted
        {
            get { return _aborted; }
            private set
            {
                if (SetField(ref _aborted, value, nameof(Aborted)))
                {
                    lock (_destMediaLock)
                    {
                        if (_destMedia != null && _destMedia.FileExists())
                            _destMedia.Delete();
                    }
                    IsIndeterminate = false;
                    OperationStatus = FileOperationStatus.Aborted;
                }
            }
        }

        internal void Fail()
        {
            OperationStatus = FileOperationStatus.Failed;
            lock (_destMediaLock)
            {
                if (_destMedia != null && _destMedia.FileExists())
                    _destMedia.Delete();
            }
            Logger.Info($"Operation failed: {Title}");
        }

        public virtual void Abort()
        {
            Aborted = true;
        }

        private SynchronizedCollection<string> _operationOutput = new SynchronizedCollection<string>();
        [JsonProperty]
        public List<string> OperationOutput { get { lock (_operationOutput.SyncRoot) return _operationOutput.ToList(); } }
        protected void AddOutputMessage(string message)
        {
            _operationOutput.Add(string.Format("{0} {1}", DateTime.Now, message));
            NotifyPropertyChanged(nameof(OperationOutput));
            Logger.Info("{0}: {1}", Title, message);
        }

        private SynchronizedCollection<string> _operationWarning = new SynchronizedCollection<string>();
        [JsonProperty]
        public List<string> OperationWarning { get { lock (_operationWarning.SyncRoot) return _operationWarning.ToList(); } }
        protected void _addWarningMessage(string message)
        {
            _operationWarning.Add(message);
            NotifyPropertyChanged(nameof(OperationWarning));
        }

        protected void CreateDestMediaIfNotExists()
        {
            lock (_destMediaLock)
            {
                if (_destMedia == null)
                    DestMedia = DestDirectory.CreateMedia(DestMediaProperties != null? DestMediaProperties: SourceMedia);
            }
        }
        
        public virtual bool Do()
        {
            if (_do())
            {
                OperationStatus = FileOperationStatus.Finished;
            }
            else
                TryCount--;
            return OperationStatus == FileOperationStatus.Finished;
        }

        private bool _do()
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
                    if (File.Exists(SourceMedia.FullPath) && Directory.Exists(DestDirectory.Folder))
                        try
                        {
                            CreateDestMediaIfNotExists();
                            if (!(_destMedia.FileExists()
                                && File.GetLastWriteTimeUtc(SourceMedia.FullPath).Equals(File.GetLastWriteTimeUtc(_destMedia.FullPath))
                                && File.GetCreationTimeUtc(SourceMedia.FullPath).Equals(File.GetCreationTimeUtc(_destMedia.FullPath))
                                && SourceMedia.FileSize.Equals(_destMedia.FileSize)))
                            {
                                _destMedia.MediaStatus = TMediaStatus.Copying;
                                IsIndeterminate = true;
                                if (!((Media)SourceMedia).CopyMediaTo((Media)_destMedia, ref _aborted))
                                    return false;
                            }
                            _destMedia.MediaStatus = TMediaStatus.Copied;
                            ThreadPool.QueueUserWorkItem(o => ((Media)_destMedia).Verify());
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
                        if (SourceMedia.Delete())
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
                            if (_destMedia.FileExists())
                            {
                                if (File.GetLastWriteTimeUtc(SourceMedia.FullPath).Equals(File.GetLastWriteTimeUtc(_destMedia.FullPath))
                                && File.GetCreationTimeUtc(SourceMedia.FullPath).Equals(File.GetCreationTimeUtc(_destMedia.FullPath))
                                && SourceMedia.FileSize.Equals(_destMedia.FileSize))
                                {
                                    SourceMedia.Delete();
                                    return true;
                                }
                                else
                                if (!_destMedia.Delete())
                                {
                                    AddOutputMessage("Move operation failed - destination media not deleted");
                                    return false;
                                }
                            }
                            IsIndeterminate = true;
                            _destMedia.MediaStatus = TMediaStatus.Copying;
                            File.Move(SourceMedia.FullPath, _destMedia.FullPath);
                            File.SetCreationTimeUtc(_destMedia.FullPath, File.GetCreationTimeUtc(SourceMedia.FullPath));
                            File.SetLastWriteTimeUtc(_destMedia.FullPath, File.GetLastWriteTimeUtc(SourceMedia.FullPath));
                            _destMedia.MediaStatus = TMediaStatus.Copied;
                            ThreadPool.QueueUserWorkItem(o => ((Media)_destMedia).Verify());
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

        [JsonProperty]
        public virtual string Title
        {
            get
            {
                return DestDirectory == null ?
                    string.Format("{0} {1}", Kind, SourceMedia)
                    :
                    string.Format("{0} {1} -> {2}", Kind, SourceMedia, DestDirectory.DirectoryName);
            }
        }


        public override string ToString()
        {
            return Title;
        }


    }
}
