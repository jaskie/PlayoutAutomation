using System;

namespace TAS.Client.Common
{
    public interface IWindow
    {
        event EventHandler Closing;
    }
}
