using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Common.Interfaces;

namespace TAS.Server.Common
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
