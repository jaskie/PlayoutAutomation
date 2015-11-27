using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TAS.Common;
using TAS.Server.Interfaces;
using TAS.Server.Remoting;

namespace TAS.Client.Model
{
    class FileOperation : ProxyBase, IFileOperation
    {
        public bool Aborted { get { return Get<bool>(); } set { Set(value); } }

        public IMedia DestMedia { get { return Get<Media>(); } set { Set(value); } }

        public DateTime FinishedTime { get { return Get<DateTime>(); } set { Set(value); } }

        public bool IsIndeterminate { get { return Get<bool>(); } set { Set(value); } }

        public TFileOperationKind Kind { get { return Get<TFileOperationKind>(); } set { Set(value); } }

        public List<string> OperationOutput { get { return Get<List<string>>(); } set { Set(value); } }

        public FileOperationStatus OperationStatus { get { return Get<FileOperationStatus>(); } set { Set(value); } }

        public List<string> OperationWarning { get { return Get<List<string>>(); } set { Set(value); } }

        public int Progress { get { return Get<int>(); } set { Set(value); } }

        public DateTime ScheduledTime { get { return Get<DateTime>(); } set { Set(value); } }

        public IMedia SourceMedia { get { return Get<Media>(); } set { Set(value); } }

        public DateTime StartTime { get { return Get<DateTime>(); } set { Set(value); } }

        public int TryCount { get { return Get<int>(); } set { Set(value); } }

        public string Title { get { return Get<string>(); } set { Set(value); } }

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
            if (e.Message.MemberName == "Finished")
            {
                var h = _finished;
                if (h != null)
                    h(this, ConvertEventArgs<EventArgs>(e));
            }
        }

        public void Fail()
        {
            throw new NotImplementedException();
        }

    }
}
