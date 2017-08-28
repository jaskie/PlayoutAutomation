using System;
using TAS.Common.Interfaces;

namespace TAS.Common
{
    public class FileOperationEventArgs : EventArgs
    {
        public FileOperationEventArgs(IFileOperation operation)
        {
            Operation = operation;
        }
        [Newtonsoft.Json.JsonProperty(ItemIsReference = true)]
        public IFileOperation Operation { get; private set; }
    }
}
