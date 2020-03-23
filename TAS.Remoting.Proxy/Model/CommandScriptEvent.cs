using jNet.RPC;
using TAS.Common.Interfaces;

namespace TAS.Remoting.Model
{
    public class CommandScriptEvent : Event, ICommandScript
    {
        #pragma warning disable CS0649

        [DtoMember(nameof(ICommandScript.Command))]
        private string _command;

        #pragma warning restore

        public string Command
        {
            get => _command;
            set => Set(value);
        }
    }
}
