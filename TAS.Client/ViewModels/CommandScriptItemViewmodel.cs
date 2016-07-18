using System;
using System.ComponentModel;
using TAS.Server.Interfaces;

namespace TAS.Client.ViewModels
{
    public class CommandScriptItemViewmodel : ViewmodelBase, ICommandScriptItem
    {
        readonly ICommandScriptItem _item;
        public CommandScriptItemViewmodel(ICommandScriptItem item)
        {
            _item = item;
        }

        protected override void OnDispose()
        {
        }

        private TimeSpan? _executeTime;
        public TimeSpan? ExecuteTime
        {
            get { return _executeTime; }
            set { SetField(ref _executeTime, value, nameof(ExecuteTime)); }
        }

        private string _command;
        public string Command
        {
            get { return _command; }
            set { SetField(ref _command, value, nameof(Command)); }
        }
    }
}