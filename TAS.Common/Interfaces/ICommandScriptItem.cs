using System;

namespace TAS.Common.Interfaces
{
    public interface ICommandScriptItem
    {
        TimeSpan? ExecuteTime { get; set; }
        string Command { get; set; }
    }
}
