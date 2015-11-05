using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Server.Common
{
    public class FileOperationEventArgs : EventArgs
    {
        public FileOperationEventArgs(IFileOperation operation)
        {
            Operation = operation;
        }
        public IFileOperation Operation { get; private set; }
    }
}
