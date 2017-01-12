using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Common;
using TAS.Server.Interfaces;

namespace TAS.Remoting.Model
{
    public class CommandScriptEvent : Event, ICommandScript
    {
        [JsonProperty(nameof(ICommandScript.Commands))]
        private List<CommandScriptItemProxy> _commands { get { return Get<List<CommandScriptItemProxy>>(); } set { Set(value); } }
        [JsonIgnore]
        public IEnumerable<ICommandScriptItem> Commands { get { return _commands; } set { _commands = value.Cast<CommandScriptItemProxy>().ToList(); } }

        public void AddCommand(ICommandScriptItem command)
        {
            Invoke(parameters: new[] { command });
        }

        public ICommandScriptItem AddCommand(TimeSpan? executeT, string command)
        {
            return Query<CommandScriptItemProxy> (parameters: new object[] { executeT, command });
        }

        public bool DeleteCommand(ICommandScriptItem command)
        {
            return Query<bool>(parameters: new[] { command });
        }
    }
}
