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
