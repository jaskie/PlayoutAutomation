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
using TAS.Server.Common;

namespace TAS.Server
{
    public class FileOperation : DtoBase, IFileOperation
    {
        [JsonProperty]
        public TFileOperationKind Kind { get; set; }

        private object _destMediaLock = new object();
        protected Media _destMedia;
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

        protected Media _sourceMedia;
        public IMedia SourceMedia { get { return _sourceMedia; } set { SetField(ref _sourceMedia, value as Media); } }
        protected IMediaProperties _destMediaProperties;
        [JsonProperty]
        public IMediaProperties DestMediaProperties { get { return _destMediaProperties; } set { SetField(ref _destMediaProperties, value, nameof(Title)); } }
        [JsonProperty]
        public IMediaDirectory DestDirectory { get; set; }
        public IMedia DestMedia { get { return _destMedia; }  protected set { SetField(ref _destMedia, value as Media); } }

        private int _tryCount = 15;
        [JsonProperty]
        public int TryCount
        {
            get { return _tryCount; }
            set { SetField(ref _tryCount, value); }
        }
        
        private int _progress;
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

        private DateTime _scheduledTime;
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
        private DateTime _startTime;
        [JsonProperty]
        public DateTime StartTime
        {
            get { return _startTime; }
            protected set { SetField(ref _startTime, value); }
        }
        private DateTime _finishedTime;
        [JsonProperty]
        public DateTime FinishedTime 
        {
            get { return _finishedTime; }
            protected set { SetField(ref _finishedTime, value); }
        }

        private FileOperationStatus _operationStatus;
        [JsonProperty]
        public FileOperationStatus OperationStatus
        {
            get { return _operationStatus; }
            set
            {
                if (SetField(ref _operationStatus, value))
                {
                    IServerIngestStatusMedia m = _sourceMedia as IServerIngestStatusMedia;
                    if (m != null)
                        switch (value)
                        {
                            case FileOperationStatus.Finished:
                                m.IngestStatus = TIngestStatus.Ready;
                                break;
                            case FileOperationStatus.Waiting:
                            case FileOperationStatus.InProgress:
                                m.IngestStatus = TIngestStatus.InProgress;
                                break;
                            default:
                                m.IngestStatus = TIngestStatus.Unknown;
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
            set { SetField(ref _isIndeterminate, value); }
        }


        protected bool _aborted;
        [JsonProperty]
        public bool Aborted
        {
            get { return _aborted; }
            private set
            {
                if (SetField(ref _aborted, value))
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
                    if (_sourceMedia != null && File.Exists(_sourceMedia.FullPath) && Directory.Exists(DestDirectory.Folder))
                        try
                        {
                            CreateDestMediaIfNotExists();
                            if (!(_destMedia.FileExists()
                                && File.GetLastWriteTimeUtc(_sourceMedia.FullPath).Equals(File.GetLastWriteTimeUtc(_destMedia.FullPath))
                                && File.GetCreationTimeUtc(_sourceMedia.FullPath).Equals(File.GetCreationTimeUtc(_destMedia.FullPath))
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
                    if (File.Exists(_sourceMedia.FullPath) && Directory.Exists(DestDirectory.Folder))
                        try
                        {
                            CreateDestMediaIfNotExists();
                            if (_destMedia.FileExists())
                            {
                                if (File.GetLastWriteTimeUtc(_sourceMedia.FullPath).Equals(File.GetLastWriteTimeUtc(_destMedia.FullPath))
                                && File.GetCreationTimeUtc(_sourceMedia.FullPath).Equals(File.GetCreationTimeUtc(_destMedia.FullPath))
                                && _sourceMedia.FileSize.Equals(_destMedia.FileSize))
                                {
                                    _sourceMedia.Delete();
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
                            FileUtils.CreateDirectoryIfNotExists(Path.GetDirectoryName(_destMedia.FullPath));
                            File.Move(_sourceMedia.FullPath, _destMedia.FullPath);
                            File.SetCreationTimeUtc(_destMedia.FullPath, File.GetCreationTimeUtc(_sourceMedia.FullPath));
                            File.SetLastWriteTimeUtc(_destMedia.FullPath, File.GetLastWriteTimeUtc(_sourceMedia.FullPath));
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
