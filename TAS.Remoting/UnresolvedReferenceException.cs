using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TAS.Remoting
{
    internal class UnresolvedReferenceException: Exception
    {
        public Guid Guid { get; }

        public UnresolvedReferenceException(string message, Guid guid): base(message)
        {
            Guid = guid;
        }
    }
}
