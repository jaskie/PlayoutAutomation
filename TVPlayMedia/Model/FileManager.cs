using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Client.Model
{
    public class FileManager : ProxyBase, IFileManager
    {
        public IEnumerable<IFileOperation> OperationQueue { get { return Get<List<FileOperation>>().Cast<IFileOperation>(); } set { Set(value); } }

        public event EventHandler<FileOperationEventArgs> OperationAdded;
        public event EventHandler<FileOperationEventArgs> OperationCompleted;

        public IConvertOperation CreateConvertOperation()
        {
            return Query<ConvertOperation>();
        }

        public IFileOperation CreateFileOperation()
        {
            return Query<FileOperation>();
        }

        public ILoudnessOperation CreateLoudnessOperation()
        {
            return Query<LoudnessOperation>();
        }
    }
}
