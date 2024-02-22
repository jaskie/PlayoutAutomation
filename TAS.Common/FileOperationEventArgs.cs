using System;
using TAS.Common.Interfaces;

namespace TAS.Common
{
    public class FileOperationEventArgs : EventArgs
    {
        public FileOperationEventArgs(IFileOperationBase operation)
        {
            Operation = operation;
        }

        public IFileOperationBase Operation { get; }
    }
}
