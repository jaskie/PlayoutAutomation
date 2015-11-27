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

namespace TAS.Server
{

    [JsonObject(MemberSerialization.OptIn)]
    public class FileOperation : IFileOperation
    {
        [JsonProperty]
        public TFileOperationKind Kind { get; set; }
        public IMedia SourceMedia { get; set; }
        public IMedia DestMedia { get; set; }
        public event EventHandler Success;
        public event EventHandler Failure;
        public event EventHandler Finished;
        internal FileManager Owner;
        public FileOperation()
        {
            ScheduledTime = DateTime.UtcNow;
            _addOutputMessage("Operation scheduled");
        }

        private readonly Guid _dtoGuid = Guid.NewGuid();

        [JsonProperty]
        public Guid DtoGuid { get { return _dtoGuid; } }

        private int _tryCount = 15;
        [JsonProperty]
        public int TryCount
        {
            get { return _tryCount; }
            set { SetField(ref _tryCount, value, "TryCount"); }
        }
        
        private int _progress;
        [JsonProperty]
        public int Progress
        {
            get { return _progress; }
            set
            {
                if (value > 0 && value <= 100)
                    SetField(ref _progress, value, "Progress");
                IsIndeterminate = false;
            }
        }

        [JsonProperty]
        public DateTime ScheduledTime { get; private set; }
        private DateTime _startTime;
        [JsonProperty]
        public DateTime StartTime
        {
            get { return _startTime; }
            protected set { SetField(ref _startTime, value, "StartTime"); }
        }
        private DateTime _finishedTime;
        [JsonProperty]
        public DateTime FinishedTime 
        {
            get { return _finishedTime; }
            protected set { SetField(ref _finishedTime, value, "FinishedTime"); }
        }

        private FileOperationStatus _operationStatus;
        [JsonProperty]
        public FileOperationStatus OperationStatus
        {
            get { return _operationStatus; }
            set
            {
                if (SetField(ref _operationStatus, value, "OperationStatus"))
                {
                    EventHandler h;
                    if (value == FileOperationStatus.Finished)
                    {
                        Progress = 100;
                        FinishedTime = DateTime.UtcNow;
                        h = Success;
                        if (h != null)
                            h(this, EventArgs.Empty);
                        h = Finished;
                        if (h != null)
                            h(this, EventArgs.Empty);
                    }
                    if (value == FileOperationStatus.Failed)
                    {
                        Progress = 0;
                        h = Failure;
                        if (h != null)
                            h(this, EventArgs.Empty);
                        h = Finished;
                        if (h != null)
                            h(this, EventArgs.Empty);
                    }
                    if (value == FileOperationStatus.Aborted)
                    {
                        IsIndeterminate = false;
                        h = Failure;
                        if (h != null)
                            h(this, EventArgs.Empty);
                        h = Finished;
                        if (h != null)
                            h(this, EventArgs.Empty);
                    }
                }
            }
        }

        private bool _isIndeterminate;
        [JsonProperty]
        public bool IsIndeterminate
        {
            get { return _isIndeterminate; }
            set { SetField(ref _isIndeterminate, value, "IsIndeterminate"); }
        }


        protected bool _aborted;
        [JsonProperty]
        public bool Aborted
        {
            get { return _aborted; }
            set
            {
                if (SetField(ref _aborted, value, "Aborted"))
                {
                    Progress = 0;
                    IsIndeterminate = false;
                    OperationStatus = FileOperationStatus.Aborted;
                }
            }
        }

        private SynchronizedCollection<string> _operationOutput = new SynchronizedCollection<string>();
        [JsonProperty]
        public List<string> OperationOutput { get { lock (_operationOutput.SyncRoot) return _operationOutput.ToList(); } }
        protected void _addOutputMessage(string message)
        {
            _operationOutput.Add(string.Format("{0} {1}", DateTime.Now, message));
            NotifyPropertyChanged("OperationOutput");
        }

        private SynchronizedCollection<string> _operationWarning = new SynchronizedCollection<string>();
        [JsonProperty]
        public List<string> OperationWarning { get { lock (_operationWarning.SyncRoot) return _operationWarning.ToList(); } }
        protected void _addWarningMessage(string message)
        {
            _operationWarning.Add(message);
            NotifyPropertyChanged("OperationWarning");
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
            Debug.WriteLine(this, "File operation started");
            _addOutputMessage("Operation started");
            StartTime = DateTime.UtcNow;
            OperationStatus = FileOperationStatus.InProgress;
            switch (Kind)
            {
                case TFileOperationKind.None:
                    return true;
                case TFileOperationKind.Convert:
                case TFileOperationKind.Export:
                    throw new InvalidOperationException("File operation can't convert");
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
                            ThreadPool.QueueUserWorkItem(o => DestMedia.Verify());

                            Debug.WriteLine(this, "File operation succeed");
                            _addOutputMessage("Copy operation finished");
                            return true;
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("File operation {0} failed with {1}", this, e.Message);
                            _addOutputMessage(string.Format("Copy operation failed with {0}", e.Message));
                        }
                    return false;
                case TFileOperationKind.Delete:
                    try
                    {
                        if (SourceMedia.Delete())
                        {
                            _addOutputMessage("Delete operation finished"); 
                            Debug.WriteLine(this, "File operation succeed");
                            return true;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("File operation failed {0} with {1}", this, e.Message);
                        _addOutputMessage(string.Format("Delete operation failed with {0}", e.Message));
                    }
                    return false;
                case TFileOperationKind.Move:
                    if (File.Exists(SourceMedia.FullPath) && Directory.Exists(Path.GetDirectoryName(DestMedia.FullPath)))
                        try
                        {
                            if (File.Exists(DestMedia.FullPath))
                                if (!DestMedia.Delete())
                                {
                                    Debug.WriteLine(this, "File operation failed - dest not deleted");
                                    _addOutputMessage("Move operation failed - destination media not deleted");
                                    return false;
                                }
                            IsIndeterminate = true;
                            DestMedia.MediaStatus = TMediaStatus.Copying;
                            File.Move(SourceMedia.FullPath, DestMedia.FullPath);
                            File.SetCreationTimeUtc(DestMedia.FullPath, File.GetCreationTimeUtc(SourceMedia.FullPath));
                            File.SetLastWriteTimeUtc(DestMedia.FullPath, File.GetLastWriteTimeUtc(SourceMedia.FullPath));
                            DestMedia.MediaStatus = TMediaStatus.Copied;
                            ThreadPool.QueueUserWorkItem(o => DestMedia.Verify());
                            _addOutputMessage("Move operation finished");
                            Debug.WriteLine(this, "File operation succeed");
                            return true;
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("File operation failed {0} with {1}", this, e.Message);
                            _addOutputMessage(string.Format("Move operation failed with {0}", e.Message));
                        }
                    return false;
                default:
                    return false;
            }
        }

        public virtual int CompareTo(object obj)
        {
            if (obj == null || !(obj is FileOperation)) return 1;
            FileOperation co = obj as FileOperation;
            int ret = Kind.CompareTo(co.Kind);
            if (ret != 0)
                return ret;
            if (SourceMedia != null && co.SourceMedia != null)
            {
                ret = SourceMedia.FullPath.CompareTo(co.SourceMedia.FullPath);
                if (ret != 0)
                    return ret;
                if (DestMedia != null && co.DestMedia != null)
                    return DestMedia.FullPath.CompareTo(co.DestMedia.FullPath);
            }
            return -1;
        }

        public override bool Equals(object obj)
        {
            return CompareTo(obj) == 0;
        }

        protected bool SetField<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            NotifyPropertyChanged(propertyName);
            return true;
        }
        
        [JsonProperty]
        public string Title
        {
            get
            {
                return DestMedia == null ?
                    string.Format("{0} {1}", Kind, SourceMedia)
                    :
                    string.Format("{0} {1} -> {2}", Kind, SourceMedia, DestMedia);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Fail()
        {
            OperationStatus = FileOperationStatus.Failed;
            if (DestMedia != null)
                DestMedia.Delete();
            Debug.WriteLine(this, "File simple operation failed - TryCount is zero");
        }       

    }
}
