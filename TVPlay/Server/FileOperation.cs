using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Text.RegularExpressions;
using TAS.Common;

namespace TAS.Server
{
    public enum TFileOperationKind { None, Copy, Move, Convert, Delete, Loudness};
    public enum FileOperationStatus {
        [Description("Czeka")]
        Waiting,
        [Description("W trakcie")]
        InProgress,
        [Description("Zakończona")]
        Finished,
        [Description("Nieudana")]
        Failed,
        [Description("Przerawna")]
        Aborted
    };
    public class FileOperation : IComparable, INotifyPropertyChanged
    {
        public TFileOperationKind Kind = TFileOperationKind.None;
        public Media SourceMedia;
        public Media DestMedia;
        public Action SuccessCallback;
        public Action FailureCallback;
        public FileOperation()
        {
            ScheduledTime = DateTime.UtcNow;
        }

        private int _tryCount = 15;
        public int TryCount
        {
            get { return _tryCount; }
            set { SetField(ref _tryCount, value, "TryCount"); }
        }
        
        private int _progress;
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

        public DateTime ScheduledTime { get; private set; }
        private DateTime _startTime;
        public DateTime StartTime
        {
            get { return _startTime; }
            protected set { SetField(ref _startTime, value, "StartTime"); }
        }
        private DateTime _finishedTime;
        public DateTime FinishedTime 
        {
            get { return _finishedTime; }
            protected set { SetField(ref _finishedTime, value, "FinishedTime"); }
        }

        private FileOperationStatus _operationStatus;
        public FileOperationStatus OperationStatus
        {
            get { return _operationStatus; }
            set
            {
                if (SetField(ref _operationStatus, value, "OperationStatus"))
                {
                    if (value == FileOperationStatus.Finished)
                    {
                        Progress = 100;
                        FinishedTime = DateTime.UtcNow;
                    }
                    if (value == FileOperationStatus.Failed)
                    {
                        Progress = 0;
                    }
                    if (value == FileOperationStatus.Aborted)
                    {
                        IsIndeterminate = false;
                    }
                }
            }
        }

        private bool _isIndeterminate;
        public bool IsIndeterminate
        {
            get { return _isIndeterminate; }
            set { SetField(ref _isIndeterminate, value, "IsIndeterminate"); }
        }


        protected bool _aborted;
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
        
        internal virtual bool Do()
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
            StartTime = DateTime.UtcNow;
            OperationStatus = FileOperationStatus.InProgress;
            switch (Kind)
            {
                case TFileOperationKind.None:
                    return true;
                case TFileOperationKind.Convert:
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
                                if (!SourceMedia.CopyMediaTo(DestMedia, ref _aborted))
                                    return false;
                            }
                            DestMedia.MediaStatus = TMediaStatus.Copied;
                            DestMedia.InvokeVerify();

                            Debug.WriteLine(this, "File operation succeed");
                            return true;
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("File operation failed {0} with {1}", this, e.Message);
                        }
                    return false;
                case TFileOperationKind.Delete:
                    try
                    {
                        if (SourceMedia.Delete())
                        {
                            Debug.WriteLine(this, "File operation succeed");
                            return true;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("File operation failed {0} with {1}", this, e.Message);
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
                                    return false;
                                }
                            IsIndeterminate = true;
                            DestMedia.MediaStatus = TMediaStatus.Copying;
                            File.Move(SourceMedia.FullPath, DestMedia.FullPath);
                            File.SetCreationTimeUtc(DestMedia.FullPath, File.GetCreationTimeUtc(SourceMedia.FullPath));
                            File.SetLastWriteTimeUtc(DestMedia.FullPath, File.GetLastWriteTimeUtc(SourceMedia.FullPath));
                            DestMedia.MediaStatus = TMediaStatus.Copied;
                            DestMedia.InvokeVerify();
                            Debug.WriteLine(this, "File operation succeed");
                            return true;
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("File operation failed {0} with {1}", this, e.Message);
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
        
        public override string ToString()
        {
            return string.Concat(Kind, " ", SourceMedia.FullPath, " ", DestMedia == null ? null : DestMedia.FullPath);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        internal void Fail()
        {
            OperationStatus = FileOperationStatus.Failed;
            if (DestMedia != null)
                DestMedia.Delete();
            if (FailureCallback != null)
                FailureCallback();
            Debug.WriteLine(this, "File simple operation failed - TryCount is zero");
        }

        

    }
}
