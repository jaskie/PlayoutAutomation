using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Common;

namespace TAS.Server.Interfaces
{
    public interface IFileManager
    {
        IEnumerable<IFileOperation> OperationQueue { get; }
        event EventHandler<FileOperationEventArgs> OperationAdded;
        event EventHandler<FileOperationEventArgs> OperationCompleted;
    }
}
