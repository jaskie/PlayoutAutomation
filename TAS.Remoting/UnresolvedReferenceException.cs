using System;

namespace TAS.Remoting
{
    public class UnresolvedReferenceException: Exception
    {
        public Guid Guid { get; }

        public UnresolvedReferenceException(Guid guid)
        {
            Guid = guid;
        }
    }
}
