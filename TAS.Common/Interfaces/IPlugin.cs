using System;

namespace TAS.Common.Interfaces
{
    public interface IPlugin : IDisposable
    {
        bool IsEnabled { get; set; }
    }
}
