using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TAS.Server.Interfaces
{
    public interface ICommandScript
    {
        IList<ICommandScriptItem> Commands { get; set; }
        void AddCommand(ICommandScriptItem command);
        ICommandScriptItem AddCommand(TimeSpan? executeT, string command);
        bool DeleteCommand(ICommandScriptItem command);
    }

}
