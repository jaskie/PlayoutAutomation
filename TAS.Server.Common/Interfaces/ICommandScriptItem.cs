using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server.Interfaces
{
    public interface ICommandScriptItem
    {
        TimeSpan? ExecuteTime { get; set; }
        string Command { get; set; }
        bool ValidateCommandText(string commandText);
    }
}
