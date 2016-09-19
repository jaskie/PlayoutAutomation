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

        public IMedia SourceMedia { get; set; }
        protected IMedia _destMedia;

        public IMedia DestMedia { get { return _destMedia; } set { SetField(ref _destMedia, value, nameof(Title)); } }
        public event EventHandler Success;
        public event EventHandler Failure;
        public event EventHandler Finished;
        internal FileManager Owner;
        public FileOperation()
        {
            Logger = NLog.LogManager.GetLogger(this.GetType().Name);
        }

        protected NLog.Logger Logger;

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
                    if (DestMedia != null && DestMedia.FileExists())
                        DestMedia.Delete();
                    IsIndeterminate = false;
                    OperationStatus = FileOperationStatus.Aborted;
                }
            }
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
                    if (File.Exists(SourceMedia.FullPath) && Directory.Exists(Path.GetDirectoryName(DestMedia.FullPath)))
                        try
                        {
                            if (!(File.Exists(DestMedia.FullPath)
                                && File.GetLastWriteTimeUtc(SourceMedia.FullPath).Equals(File.GetLastWriteTimeUtc(DestMedia.FullPath))
                                && File.GetCreationTimeUtc(SourceMedia.FullPath).Equals(File.GetCreationTimeUtc(DestMedia.FullPath))
                                && SourceMedia.FileSize.Equals(DestMedia.FileSize)))
                            {
                                DestMedia.MediaStatus = TMediaStatus.Copying;
                                IsIndeterminate = true;
                                if (!((Media)SourceMedia).CopyMediaTo((Media)DestMedia, ref _aborted))
                                    return false;
                            }
                            DestMedia.MediaStatus = TMediaStatus.Copied;
                            ThreadPool.QueueUserWorkItem(o => ((Media)DestMedia).Verify());
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
                    if (File.Exists(SourceMedia.FullPath) && Directory.Exists(Path.GetDirectoryName(DestMedia.FullPath)))
                        try
                        {
                            if (File.Exists(DestMedia.FullPath))
                                if (!DestMedia.Delete())
                                {
                                    AddOutputMessage("Move operation failed - destination media not deleted");
                                    return false;
                                }
                            IsIndeterminate = true;
                            DestMedia.MediaStatus = TMediaStatus.Copying;
                            File.Move(SourceMedia.FullPath, DestMedia.FullPath);
                            File.SetCreationTimeUtc(DestMedia.FullPath, File.GetCreationTimeUtc(SourceMedia.FullPath));
                            File.SetLastWriteTimeUtc(DestMedia.FullPath, File.GetLastWriteTimeUtc(SourceMedia.FullPath));
                            DestMedia.MediaStatus = TMediaStatus.Copied;
                            ThreadPool.QueueUserWorkItem(o => ((Media)DestMedia).Verify());
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
                return DestMedia == null ?
                    string.Format("{0} {1}", Kind, SourceMedia)
                    :
                    string.Format("{0} {1} -> {2}", Kind, SourceMedia, DestMedia);
            }
        }

        internal void Fail()
        {
            OperationStatus = FileOperationStatus.Failed;
            if (DestMedia != null && DestMedia.FileExists())
                DestMedia.Delete();
            Logger.Info($"Operation failed: {Title}");
        }

        public override string ToString()
        {
            return Title;
        }

    }
}
