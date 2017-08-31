using Newtonsoft.Json;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class CommandScriptEvent : Event, ICommandScript
    {
        #pragma warning disable CS0649

        [JsonProperty(nameof(ICommandScript.Command))]
        private string _command;

        #pragma warning restore

        public string Command
        {
            get { return _command; }
            set { Set(value); }
        }
    }
}
