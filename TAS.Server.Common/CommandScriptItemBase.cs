using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Remoting.Server;
using TAS.Server.Interfaces;

namespace TAS.Server.Common
{
    public class CommandScriptItemBase : DtoBase, ICommandScriptItem
    {
        private TimeSpan? _executeTime;
        public TimeSpan? ExecuteTime
        {
            get { return _executeTime; }
            set { SetField(ref _executeTime, value, "ExecuteTime"); }
        }

        private string _command;
        public string Command
        {
            get { return _command; }
            set { SetField(ref _command, value, "Command"); }
        }
    }
}
