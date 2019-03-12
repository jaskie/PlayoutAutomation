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
        [Newtonsoft.Json.JsonProperty(ItemIsReference = true)]
        public IFileOperationBase Operation { get; private set; }
    }
}
