using System;

namespace TAS.Common.Interfaces
{
    public interface IGpi
    {
        bool IsWideScreen { get; set; }
        event EventHandler Started;
    }
}
