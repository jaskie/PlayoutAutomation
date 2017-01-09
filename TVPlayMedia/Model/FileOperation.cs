using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Remoting;
using TAS.Remoting.Client;
using TAS.Server.Interfaces;

namespace TAS.Client.Model
{
    class FileOperation : ProxyBase, IFileOperation
    {
        public bool Aborted { get { return Get<bool>(); } set { Set(value); } }

        [JsonProperty(IsReference = false, TypeNameHandling = TypeNameHandling.Auto)]
        public IMediaProperties DestMediaProperties { get { return Get<IMediaProperties>(); } set { Set(value); } }

        public IMediaDirectory DestDirectory { get { return Get<IMediaDirectory>(); } set { Set(value); } }

        public DateTime FinishedTime { get { return Get<DateTime>(); } set { SetField(value); } }

        public bool IsIndeterminate { get { return Get<bool>(); } set { SetField(value); } }

        public TFileOperationKind Kind { get { return Get<TFileOperationKind>(); } set { SetField(value); } }

        public List<string> OperationOutput { get { return Get<List<string>>(); } set { SetField(value); } }

        public FileOperationStatus OperationStatus { get { return Get<FileOperationStatus>(); } set { SetField(value); } }

        public List<string> OperationWarning { get { return Get<List<string>>(); } set { SetField(value); } }

        public int Progress { get { return Get<int>(); } set { SetField(value); } }

        public DateTime ScheduledTime { get { return Get<DateTime>(); } set { SetField(value); } }

        public IMedia SourceMedia { get { return Get<Media>(); } set { Set(value); } }

        public DateTime StartTime { get { return Get<DateTime>(); } set { SetField(value); } }

        public int TryCount { get { return Get<int>(); } set { SetField(value); } }

        public string Title { get { return Get<string>(); } set { SetField(value); } }

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

        protected override void OnEventNotification(WebSocketMessageEventArgs e)
        {
            if (e.Message.MemberName == nameof(Finished))
            {
                var h = _finished;
                if (h != null)
                    h(this, ConvertEventArgs<EventArgs>(e));
            }
        }

        public void Abort()
        {
            Invoke();
        }

    }
}
