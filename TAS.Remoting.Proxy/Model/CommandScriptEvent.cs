using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Common;
using TAS.Server.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class CommandScriptEvent : Event, ICommandScript
    {
        public string Command
        {
            get { return Get<string>(); }
            set { Set(value); }
        }
    }
}
