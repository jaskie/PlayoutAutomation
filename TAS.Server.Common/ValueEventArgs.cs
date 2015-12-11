using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server.Common
{
    public class ValueEventArgs<T>: EventArgs
    {
        public ValueEventArgs(T value)
        {
            this.Value = value;
        }
        public T Value { get; private set; }
    }
}
