using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Server
{
    internal class MediaPropertyChangedEventArgs : MediaEventArgs
    {
        public MediaPropertyChangedEventArgs(IMedia media, string propertyName) : base(media)
        {
            PropertyName = propertyName;
        }
        public string PropertyName { get; }
    }
}
