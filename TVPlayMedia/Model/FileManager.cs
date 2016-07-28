using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Remoting;
using TAS.Remoting.Client;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.Model
{
    public class FileManager : ProxyBase, IFileManager
    {
        public IEnumerable<IFileOperation> GetOperationQueue() { return Query<List<IFileOperation>>(); }
        event EventHandler<FileOperationEventArgs> _operationAdded;
        public event EventHandler<FileOperationEventArgs> OperationAdded
        {
            add
            {
                EventAdd(_operationAdded);
                _operationAdded += value;
            }
            remove
            {
                _operationAdded -= value;
                EventRemove(_operationAdded);
            }
        }

        event EventHandler<FileOperationEventArgs> _operationCompleted;
        public event EventHandler<FileOperationEventArgs> OperationCompleted
        {
            add
            {
                EventAdd(_operationCompleted);
                _operationCompleted += value;
            }
            remove
            {
                _operationCompleted -= value;
                EventRemove(_operationCompleted);
            }
        }

        public IConvertOperation CreateConvertOperation()
        {
            return Query<ConvertOperation>();
        }

        public IFileOperation CreateSimpleOperation()
        {
            return Query<FileOperation>();
        }

        public ILoudnessOperation CreateLoudnessOperation()
        {
            return Query<LoudnessOperation>();
        }

        protected override void OnEventNotification(WebSocketMessageEventArgs e)
        {
            if (e.Message.MemberName == nameof(OperationAdded))
            {
                var h = _operationAdded;
                if (h != null)
                    h(this, ConvertEventArgs<FileOperationEventArgs>(e));
            }
            if (e.Message.MemberName == nameof(OperationCompleted))
            {
                var h = _operationCompleted;
                if (h != null)
                    h(this, ConvertEventArgs<FileOperationEventArgs>(e));
            }
        }

        public void Queue(IFileOperation operation, bool toTop)
        {
            Invoke(parameters: new object[] { operation, toTop });
        }
      

        public void QueueList(IEnumerable<IFileOperation> operationList, bool toTop)
        {
            Invoke(parameters: new object[] { operationList, toTop });
        }

        public void CancelPending()
        {
            Invoke();
        }
    }
}
