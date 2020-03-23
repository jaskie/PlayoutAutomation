using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using jNet.RPC;
using jNet.RPC.Server;
using NLog;
using TAS.Common;
using TAS.Common.Interfaces;
using TAS.Common.Interfaces.Media;

namespace TAS.Server.MediaOperation
{
    public abstract class FileOperationBase : ServerObjectBase, IFileOperationBase
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private int _tryCount = 15;
        private DateTime _scheduledTime;
        private DateTime _startTime;
        private DateTime _finishedTime;
        private FileOperationStatus _operationStatus;
        private int _progress;
        private bool _isIndeterminate;
        private readonly List<string> _operationOutput = new List<string>();
        private readonly List<string> _operationWarning = new List<string>();
        protected readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        private bool _isAborted;

        [DtoField]
        public int TryCount
        {
            get => _tryCount;
            internal set => SetField(ref _tryCount, value);
        }

        [DtoField]
        public int Progress
        {
            get => _progress;
            internal set
            {
                if (value > 0 && value <= 100)
                    SetField(ref _progress, value);
                IsIndeterminate = false;
            }
        }

        [DtoField]
        public DateTime ScheduledTime
        {
            get => _scheduledTime;
            internal set => SetField(ref _scheduledTime, value);
        }

        [DtoField]
        public DateTime StartTime
        {
            get => _startTime;
            protected set => SetField(ref _startTime, value);
        }

        [DtoField]
        public DateTime FinishedTime 
        {
            get => _finishedTime;
            protected set => SetField(ref _finishedTime, value);
        }

        [DtoField]
        public FileOperationStatus OperationStatus
        {
            get => _operationStatus;
            set
            {
                if (!SetField(ref _operationStatus, value))
                    return;
                OnOperationStatusChanged();
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


        [DtoField]
        public bool IsIndeterminate
        {
            get => _isIndeterminate;
            set => SetField(ref _isIndeterminate, value);
        }

        [DtoField]
        public bool IsAborted
        {
            get => _isAborted;
            private set => SetField(ref _isAborted, value);
        }

        [DtoField]
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

        [DtoField]
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
            IsIndeterminate = false;
            OperationStatus = FileOperationStatus.Aborted;
        }

        public event EventHandler Success;
        public event EventHandler Failure;
        public event EventHandler Finished;

        internal async Task<bool> Execute()
        {
            try
            {
                AddOutputMessage(LogLevel.Info, $"Operation started: {this}");
                OperationStatus = FileOperationStatus.InProgress;
                if (await InternalExecute())
                {
                    OperationStatus = FileOperationStatus.Finished;
                    AddOutputMessage(LogLevel.Info, $"Operation completed successfully: {this}");
                    return true;
                }
            }
            catch (Exception e)
            {
                AddOutputMessage(LogLevel.Error, e.Message);
            }
            TryCount--;
            if (!IsAborted)
                OperationStatus = TryCount > 0 ? FileOperationStatus.Waiting : FileOperationStatus.Failed;
            return false;
        }

        protected abstract void OnOperationStatusChanged();
        
        internal void AddOutputMessage(LogLevel level, string message)
        {
            lock (((IList)_operationOutput).SyncRoot)
                _operationOutput.Add($"{DateTime.UtcNow} {message}");
            NotifyPropertyChanged(nameof(OperationOutput));
            Logger.Log(level, message);
        }

        internal void AddWarningMessage(string message)
        {
            lock (((IList)_operationWarning).SyncRoot)
                _operationWarning.Add(message);
            Logger.Warn(message);
            NotifyPropertyChanged(nameof(OperationWarning));
        }

        protected abstract Task<bool> InternalExecute();

        protected string MediaToString(IMediaProperties media)
        {
            if (media == null)
                return "None";
            if (string.IsNullOrWhiteSpace(media.Folder))
                return media.MediaName ?? media.FileName ?? string.Empty;
            return Path.Combine(media.Folder, media.MediaName ?? media.FileName ?? string.Empty);
        }

    }
}