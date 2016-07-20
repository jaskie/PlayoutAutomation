using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using TAS.Remoting.Server;
using TAS.Server.Interfaces;

namespace TAS.Server.Common
{
    [Newtonsoft.Json.JsonObject(IsReference = false)]
    public class CommandScriptItemBase : DtoBase, ICommandScriptItem
    {
        private TimeSpan? _executeTime;
        [DataMember]
        public TimeSpan? ExecuteTime
        {
            get { return _executeTime; }
            set { SetField(ref _executeTime, value, "ExecuteTime"); }
        }

        private string _command;
        [DataMember]
        public string Command
        {
            get { return _command; }
            set { SetField(ref _command, value, "Command"); }
        }
    }
}
