using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS.Server.Interfaces;

namespace TAS.Remoting.Model
{
    public class CommandScriptEvent : Event, ICommandScript
    {
        public IEnumerable<ICommandScriptItem> Commands
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public void AddCommand(ICommandScriptItem command)
        {
            throw new NotImplementedException();
        }

        public ICommandScriptItem AddCommand(TimeSpan? executeT, string command)
        {
            throw new NotImplementedException();
        }

        public bool DeleteCommand(ICommandScriptItem command)
        {
            throw new NotImplementedException();
        }
    }
}
