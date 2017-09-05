using System;

namespace TAS.Client.Common
{
    public interface ICloseable
    {
        event EventHandler ClosedOk;
    }
}
