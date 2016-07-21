using System;
using System.ComponentModel;
using TAS.Client.Common;
using TAS.Common;
using TAS.Server.Interfaces;
using resources = TAS.Client.Common.Properties.Resources;

namespace TAS.Client.ViewModels
{
    public class CommandScriptItemViewmodel : OkCancelViewmodelBase<ICommandScriptItem>, ICommandScriptItem
    {
        public CommandScriptItemViewmodel(ICommandScriptItem item, RationalNumber frameRate):base(item, new Views.CommandScriptItemEditView(frameRate), resources._window_CommandScriptItemEditWindowTitle)
        {
            IsFinalizationCommand = item.ExecuteTime == null;
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

        public bool IsFinalizationCommand { get; set; }
    }
}