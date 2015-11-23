using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Common;

namespace TAS.Server.Interfaces
{
    public interface IFileManager: IDto
    {
        IEnumerable<IFileOperation> OperationQueue { get; }
        IConvertOperation CreateConvertOperation();
        ILoudnessOperation CreateLoudnessOperation();
        IFileOperation CreateFileOperation();
        event EventHandler<FileOperationEventArgs> OperationAdded;
        event EventHandler<FileOperationEventArgs> OperationCompleted;
    }
}
