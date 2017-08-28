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
        public bool IsAborted { get { return Get<bool>(); } set { Set(value); } }

        [JsonProperty(IsReference = false, TypeNameHandling = TypeNameHandling.Auto)]
        public IMediaProperties DestProperties { get { return Get<IMediaProperties>(); } set { Set(value); } }

        public IMediaDirectory DestDirectory { get { return Get<IMediaDirectory>(); } set { Set(value); } }

        public DateTime FinishedTime { get { return Get<DateTime>(); } set { SetLocalValue(value); } }

        public bool IsIndeterminate { get { return Get<bool>(); } set { SetLocalValue(value); } }

        public TFileOperationKind Kind { get { return Get<TFileOperationKind>(); } set { SetLocalValue(value); } }

        public List<string> OperationOutput { get { return Get<List<string>>(); } set { SetLocalValue(value); } }

        public FileOperationStatus OperationStatus { get { return Get<FileOperationStatus>(); } set { SetLocalValue(value); } }

        public List<string> OperationWarning { get { return Get<List<string>>(); } set { SetLocalValue(value); } }

        public int Progress { get { return Get<int>(); } set { SetLocalValue(value); } }

        public DateTime ScheduledTime { get { return Get<DateTime>(); } set { SetLocalValue(value); } }

        public IMedia Source { get { return Get<Media>(); } set { Set(value); } }

        public DateTime StartTime { get { return Get<DateTime>(); } set { SetLocalValue(value); } }

        public int TryCount { get { return Get<int>(); } set { SetLocalValue(value); } }

        public string Title { get { return Get<string>(); } set { SetLocalValue(value); } }

        private event EventHandler _finished;
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

        protected override void OnEventNotification(WebSocketMessage e)
        {
            if (e.MemberName == nameof(Finished))
            {
                _finished?.Invoke(this, ConvertEventArgs<EventArgs>(e));
            }
        }

        public void Abort()
        {
            Invoke();
        }

    }
}
