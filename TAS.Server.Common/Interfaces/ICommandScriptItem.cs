using System;

namespace TAS.Server.Common.Interfaces
{
    public interface ICommandScriptItem
    {
        TimeSpan? ExecuteTime { get; set; }
        string Command { get; set; }
    }
}
