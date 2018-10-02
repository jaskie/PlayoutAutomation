using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using TAS.Remoting.Client;
using TAS.Common;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class FileOperation : ProxyBase, IFileOperation
    {
        #pragma warning disable CS0649 

        [JsonProperty(nameof(IFileOperation.IsAborted))]
        private bool _isAborted;

        [JsonProperty(nameof(IFileOperation.DestProperties))]
        private MediaProxy _destProperties;

        [JsonProperty(nameof(IFileOperation.DestDirectory))]
        private WatcherDirectory _destDirectory;

        [JsonProperty(nameof(IFileOperation.FinishedTime))]
        private DateTime _finishedTime;

        [JsonProperty(nameof(IFileOperation.IsIndeterminate))]
        private bool _isIndeterminate;

        [JsonProperty(nameof(IFileOperation.Kind))]
        private TFileOperationKind _kind;

        [JsonProperty(nameof(IFileOperation.OperationOutput))]
        private List<string> _operationOutput;

        [JsonProperty(nameof(IFileOperation.OperationStatus))]
        private FileOperationStatus _operationStatus;

        [JsonProperty(nameof(IFileOperation.OperationWarning))]
        private List<string> _operationWarning;

        [JsonProperty(nameof(IFileOperation.Progress))]
        private int _progress;

        [JsonProperty(nameof(IFileOperation.ScheduledTime))]
        private DateTime _scheduledTime;

        [JsonProperty(nameof(IFileOperation.Source))]
        private MediaBase _source;

        [JsonProperty(nameof(IFileOperation.StartTime))]
        private DateTime _startTime;

        [JsonProperty(nameof(IFileOperation.TryCount))]
        private int _tryCount;

        [JsonProperty(nameof(IFileOperation.Title))]
        private string _title;

#pragma warning restore

        private event EventHandler _finished;

        public bool IsAborted => _isAborted;

        public IMediaProperties DestProperties { get => _destProperties; set => Set(value); }

        public IWatcherDirectory DestDirectory { get => _destDirectory; set => Set(value); }

        public DateTime FinishedTime => _finishedTime;

        public bool IsIndeterminate => _isIndeterminate;

        public TFileOperationKind Kind { get => _kind; set => Set(value); }

        public List<string> OperationOutput => _operationOutput;

        public FileOperationStatus OperationStatus => _operationStatus;

        public List<string> OperationWarning => _operationWarning;

        public int Progress => _progress;

        public DateTime ScheduledTime => _scheduledTime;

        public IMedia Source { get => _source; set => Set(value); }

        public DateTime StartTime => _startTime;

        public int TryCount => _tryCount;

        public string Title => _title;

        public event EventHandler Finished
        {
            add
            {
                EventAdd(_finished);
                _finished += value;
            }
            remove
            {
                _finished -= value;
                EventRemove(_finished);
            }
        }

        public void Abort()
        {
            Invoke();
        }

        protected override void OnEventNotification(SocketMessage message)
        {
            if (message.MemberName == nameof(Finished))
            {
                _finished?.Invoke(this, Deserialize<EventArgs>(message));
            }
        }
    }
}
