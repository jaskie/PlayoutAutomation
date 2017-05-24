using System;

namespace TAS.Server.Common.Interfaces
{
    public interface IGpi
    {
        bool IsWideScreen { get; set; }
        event EventHandler Started;
    }
}
